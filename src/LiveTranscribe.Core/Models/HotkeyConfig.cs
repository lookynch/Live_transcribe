namespace LiveTranscribe.Core.Models;

/// <summary>A configurable global hotkey: a virtual-key code plus modifier flags.</summary>
public sealed class HotkeyConfig
{
    /// <summary>Windows virtual-key code (e.g. 0x77 = F8).</summary>
    public int VirtualKey { get; set; }

    public HotkeyModifiers Modifiers { get; set; } = HotkeyModifiers.None;

    public HotkeyConfig() { }

    public HotkeyConfig(int virtualKey, HotkeyModifiers modifiers = HotkeyModifiers.None)
    {
        VirtualKey = virtualKey;
        Modifiers = modifiers;
    }

    public bool IsSet => VirtualKey != 0;
}
