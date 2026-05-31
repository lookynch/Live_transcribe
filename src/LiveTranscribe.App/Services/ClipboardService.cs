using System.Windows;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using Serilog;

namespace LiveTranscribe.App.Services;

/// <summary>
/// WPF clipboard access. The Windows clipboard is a shared, sometimes-locked OS
/// resource, so every operation retries a few times. Runs on the UI thread.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    private const int Retries = 5;
    private const int RetryDelayMs = 40;

    public ClipboardSnapshot Capture()
    {
        return OnUi(() =>
        {
            try
            {
                if (Clipboard.ContainsText())
                    return new ClipboardSnapshot { HadText = true, Text = Clipboard.GetText() };
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Clipboard capture failed");
            }
            return ClipboardSnapshot.Empty;
        });
    }

    public void Restore(ClipboardSnapshot snapshot)
    {
        OnUi(() =>
        {
            try
            {
                if (snapshot.HadText && snapshot.Text is not null)
                    SetWithRetry(snapshot.Text);
                else
                    Clipboard.Clear();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Clipboard restore failed");
            }
            return 0;
        });
    }

    public void SetText(string text) => OnUi(() => { SetWithRetry(text); return 0; });

    private static void SetWithRetry(string text)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return;
            }
            catch when (attempt < Retries)
            {
                Thread.Sleep(RetryDelayMs);
            }
        }
    }

    private static T OnUi<T>(Func<T> action)
    {
        var app = Application.Current;
        if (app is null || app.Dispatcher.CheckAccess())
            return action();
        return app.Dispatcher.Invoke(action);
    }
}
