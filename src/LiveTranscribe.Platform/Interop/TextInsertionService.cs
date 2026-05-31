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

        _activeWindow.RestoreForeground(targetWindow);
        await Task.Delay(80); // give the target time to become active again

        if (method == InsertMethod.Keystrokes)
        {
            _input.TypeText(text);
            return;
        }

        var snapshot = _clipboard.Capture();
        try
        {
            _clipboard.SetText(text);
            await Task.Delay(30);
            _input.SendPaste();
            await Task.Delay(120); // let the target consume the paste before restoring
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
}
