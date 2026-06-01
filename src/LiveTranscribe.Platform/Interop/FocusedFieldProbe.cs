using System.Text;
using LiveTranscribe.Core.Abstractions;

namespace LiveTranscribe.Platform.Interop;

/// <summary>
/// Decides whether the current input focus is a text field, so dictation can paste there
/// instead of only copying. Two cheap, dependency-free signals:
///   1. A system caret in the focused thread (GetGUIThreadInfo.hwndCaret) — present whenever
///      the user is in a normal text input (Win32 edits, browser inputs, chat boxes, …).
///   2. A focused control whose window class is a known editable type (Edit, RichEdit, …) —
///      catches a few fields that suppress the blinking caret.
/// When neither holds, there is nowhere sensible to type, so the caller uses the clipboard.
/// </summary>
public sealed class FocusedFieldProbe : IFocusedFieldProbe
{
    private static readonly string[] EditableClasses =
        { "edit", "richedit", "richedit20w", "richedit20a", "richedit50w", "richeditd2dpt" };

    public bool IsEditableFieldFocused()
    {
        try
        {
            var foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;

            var thread = NativeMethods.GetWindowThreadProcessId(foreground, out _);
            if (thread == 0) return false;

            var gti = new NativeMethods.GUITHREADINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.GUITHREADINFO>() };
            if (!NativeMethods.GetGUIThreadInfo(thread, ref gti))
                return false;

            // A live caret is the strongest, most universal "you can type here" signal.
            if (gti.hwndCaret != IntPtr.Zero)
                return true;

            return gti.hwndFocus != IntPtr.Zero && IsEditableClass(gti.hwndFocus);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsEditableClass(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        if (NativeMethods.GetClassName(hWnd, sb, sb.Capacity) == 0)
            return false;

        var name = sb.ToString().ToLowerInvariant();
        foreach (var cls in EditableClasses)
            if (name.Contains(cls))
                return true;
        return false;
    }
}
