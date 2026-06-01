using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;

namespace LiveTranscribe.Platform.Interop;

/// <summary>
/// Inserts text into the window that was focused before recording. Re-focuses the
/// target, pastes via the clipboard (restoring the previous clipboard content), and
/// falls back to simulated Unicode keystrokes if pasting fails.
/// </summary>
public sealed class TextInsertionService : ITextInsertionService
{
    private readonly IActiveWindowService _activeWindow;
    private readonly IClipboardService _clipboard;
    private readonly IInputSimulator _input;

    public TextInsertionService(IActiveWindowService activeWindow, IClipboardService clipboard, IInputSimulator input)
    {
        _activeWindow = activeWindow;
        _clipboard = clipboard;
        _input = input;
    }

    public async Task InsertAsync(string text, InsertMethod method, IntPtr targetWindow)
    {
        if (string.IsNullOrEmpty(text)) return;

        var focused = await TryRestoreFocusAsync(targetWindow);

        // If we couldn't confirm the original window is foreground, a clipboard paste would
        // land nowhere useful. Typing goes to whatever control currently has focus, so it's
        // the safer fallback. Exactly one insertion method ever runs (never both).
        if (method == InsertMethod.Keystrokes || !focused)
        {
            _input.TypeText(text);
            return;
        }

        var snapshot = _clipboard.Capture();
        try
        {
            _clipboard.SetText(text);
            await Task.Delay(40);
            _input.SendPaste();
            await Task.Delay(220); // let the target consume the paste before restoring
        }
        catch
        {
            _input.TypeText(text); // clipboard path failed — type it instead
            return;
        }
        finally
        {
            _clipboard.Restore(snapshot);
        }
    }

    /// <summary>
    /// Re-focuses the target window and verifies it actually became foreground, retrying a
    /// few times because Windows often delays/blocks SetForegroundWindow from a background app.
    /// Returns true once the target is confirmed foreground.
    /// </summary>
    private async Task<bool> TryRestoreFocusAsync(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero) return false;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            _activeWindow.RestoreForeground(targetWindow);
            await Task.Delay(60);
            if (_activeWindow.CaptureForeground() == targetWindow)
                return true;
        }
        return false;
    }
}
