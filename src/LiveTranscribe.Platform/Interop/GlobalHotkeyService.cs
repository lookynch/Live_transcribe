using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using static LiveTranscribe.Platform.Interop.NativeMethods;

namespace LiveTranscribe.Platform.Interop;

/// <summary>
/// Global hotkeys via a WH_KEYBOARD_LL hook on a dedicated thread. RegisterHotKey is
/// unusable here because it provides no key-up event, which push-to-talk requires.
/// The hook never swallows keys (always calls CallNextHookEx) so normal typing is unaffected.
/// </summary>
public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12, VK_LWIN = 0x5B, VK_RWIN = 0x5C;

    private readonly object _gate = new();
    private LowLevelKeyboardProc? _proc;     // kept alive to avoid GC of the callback
    private IntPtr _hookHandle;
    private Thread? _thread;
    private uint _threadId;

    private HotkeyConfig _pushToTalk = new();
    private HotkeyConfig _toggleOverlay = new();
    private HotkeyConfig _startStop = new();

    private bool _pttDown;                    // KeyDown-once guard for push-to-talk

    public event EventHandler? PushToTalkDown;
    public event EventHandler? PushToTalkUp;
    public event EventHandler? ToggleOverlayPressed;
    public event EventHandler? StartStopPressed;

    public void Reload(AppSettings settings)
    {
        lock (_gate)
        {
            _pushToTalk = settings.PushToTalk;
            _toggleOverlay = settings.ToggleOverlay;
            _startStop = settings.StartStopRecord;
        }
    }

    public void Start()
    {
        if (_thread is not null) return;

        _thread = new Thread(HookThread)
        {
            IsBackground = true,
            Name = "LiveTranscribe.Hotkeys"
        };
        _thread.Start();
    }

    private void HookThread()
    {
        _threadId = GetCurrentThreadId();
        _proc = HookCallback;
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);

        while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            // The hook delivers via the message queue; nothing else to dispatch.
        }

        if (_hookHandle != IntPtr.Zero)
            UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = System.Runtime.InteropServices.Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var vk = (int)data.vkCode;
            var msg = (int)wParam;
            var isDown = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
            var isUp = msg is WM_KEYUP or WM_SYSKEYUP;

            HotkeyConfig ptt, toggle, startStop;
            lock (_gate) { ptt = _pushToTalk; toggle = _toggleOverlay; startStop = _startStop; }

            // Push-to-talk: fire down exactly once, up on release. Ignore OS auto-repeat.
            if (ptt.IsSet && vk == ptt.VirtualKey)
            {
                if (isDown && ModifiersHeld(ptt.Modifiers) && !_pttDown)
                {
                    _pttDown = true;
                    Raise(PushToTalkDown);
                }
                else if (isUp && _pttDown)
                {
                    _pttDown = false;
                    Raise(PushToTalkUp);
                }
            }

            if (isDown)
            {
                if (toggle.IsSet && vk == toggle.VirtualKey && ModifiersHeld(toggle.Modifiers))
                    Raise(ToggleOverlayPressed);
                if (startStop.IsSet && vk == startStop.VirtualKey && ModifiersHeld(startStop.Modifiers))
                    Raise(StartStopPressed);
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool ModifiersHeld(HotkeyModifiers mods)
    {
        bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

        if (mods.HasFlag(HotkeyModifiers.Control) != Down(VK_CONTROL)) return false;
        if (mods.HasFlag(HotkeyModifiers.Shift) != Down(VK_SHIFT)) return false;
        if (mods.HasFlag(HotkeyModifiers.Alt) != Down(VK_MENU)) return false;
        if (mods.HasFlag(HotkeyModifiers.Win) != (Down(VK_LWIN) || Down(VK_RWIN))) return false;
        return true;
    }

    private static void Raise(EventHandler? handler) => handler?.Invoke(null, EventArgs.Empty);

    public void Dispose()
    {
        if (_thread is null) return;
        if (_threadId != 0)
            PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _proc = null;
    }
}
