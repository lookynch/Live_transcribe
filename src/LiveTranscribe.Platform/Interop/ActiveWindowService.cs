using LiveTranscribe.Core.Abstractions;

namespace LiveTranscribe.Platform.Interop;

public sealed class ActiveWindowService : IActiveWindowService
{
    public IntPtr CaptureForeground() => NativeMethods.GetForegroundWindow();

    public void RestoreForeground(IntPtr handle)
    {
        if (handle == IntPtr.Zero || !NativeMethods.IsWindow(handle))
            return;

        if (NativeMethods.SetForegroundWindow(handle))
            return;

        // Windows often blocks SetForegroundWindow from a background app.
        // Attaching to the target's input thread lets the call succeed.
        var targetThread = NativeMethods.GetWindowThreadProcessId(handle, out _);
        var thisThread = NativeMethods.GetCurrentThreadId();
        if (targetThread == 0 || targetThread == thisThread)
            return;

        if (NativeMethods.AttachThreadInput(thisThread, targetThread, true))
        {
            try { NativeMethods.SetForegroundWindow(handle); }
            finally { NativeMethods.AttachThreadInput(thisThread, targetThread, false); }
        }
    }
}
