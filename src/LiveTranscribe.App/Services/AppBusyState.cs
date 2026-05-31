using LiveTranscribe.Core.Abstractions;

namespace LiveTranscribe.App.Services;

/// <summary>
/// Tracks in-flight dictation work via a reentrant counter. Used to gate update
/// installs so they never interrupt recording/transcription/insertion.
/// </summary>
public sealed class AppBusyState : IAppBusyState
{
    private readonly object _gate = new();
    private int _count;

    public bool IsBusy
    {
        get { lock (_gate) return _count > 0; }
    }

    public event EventHandler? Changed;

    public IDisposable Enter()
    {
        bool became;
        lock (_gate)
        {
            became = _count == 0;
            _count++;
        }
        if (became) Changed?.Invoke(this, EventArgs.Empty);
        return new Scope(this);
    }

    private void Exit()
    {
        bool cleared;
        lock (_gate)
        {
            if (_count > 0) _count--;
            cleared = _count == 0;
        }
        if (cleared) Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class Scope(AppBusyState owner) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            owner.Exit();
        }
    }
}
