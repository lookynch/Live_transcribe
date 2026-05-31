using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using Serilog;

namespace LiveTranscribe.App.Services;

/// <summary>Raised when the pipeline produces text destined for the preview window.</summary>
public sealed record DictationResult(string RawText, string FinalText, ProcessingMode Mode);

/// <summary>
/// Orchestrates one dictation run end to end: capture foreground → record →
/// local transcription → optional OpenAI rework → insert/copy/preview. The temp
/// WAV is always deleted, and <see cref="IAppBusyState"/> is held for the whole
/// run so updates can't interrupt it.
/// </summary>
public sealed class DictationCoordinator(
    IAudioRecordingService recorder,
    IWhisperModelService models,
    ILocalSpeechToTextService speech,
    IOpenAiTextOptimizationService optimizer,
    ITextInsertionService insertion,
    IActiveWindowService activeWindow,
    IClipboardService clipboard,
    ISettingsService settings,
    IAppBusyState busy)
{
    private IntPtr _target;
    private IDisposable? _busyScope;

    public bool IsRecording => recorder.IsRecording;

    /// <summary>The window that was focused when recording started — the insertion target.</summary>
    public IntPtr LastTarget => _target;

    public event Action<AppStatus, string?>? StatusChanged;
    public event Action<DictationResult>? PreviewRequested;
    public event EventHandler<float>? LevelChanged
    {
        add => recorder.LevelChanged += value;
        remove => recorder.LevelChanged -= value;
    }

    public void StartRecording()
    {
        if (recorder.IsRecording) return;

        _busyScope = busy.Enter();
        try
        {
            _target = activeWindow.CaptureForeground();
            recorder.Start();
            Report(AppStatus.Recording);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Aufnahme konnte nicht gestartet werden");
            Report(AppStatus.Error, ex.Message);
            ReleaseBusy();
        }
    }

    public async Task StopAndProcessAsync()
    {
        if (!recorder.IsRecording) return;

        string? wavPath = null;
        try
        {
            wavPath = await recorder.StopAsync().ConfigureAwait(false);

            var s = settings.Current;
            Report(AppStatus.TranscribingLocal);
            await models.EnsureModelAsync(s.WhisperModel).ConfigureAwait(false);
            var raw = (await speech.TranscribeAsync(wavPath, s.Language).ConfigureAwait(false)).Trim();

            if (string.IsNullOrWhiteSpace(raw))
            {
                Report(AppStatus.Error, "Keine Sprache erkannt");
                return;
            }

            var mode = s.DefaultProcessingMode;
            var final = raw;
            if (mode != ProcessingMode.TranscribeOnly)
            {
                Report(AppStatus.Optimizing);
                final = await optimizer
                    .OptimizeAsync(raw, mode, s.DefaultTone, s.CustomInstruction)
                    .ConfigureAwait(false);
            }

            await DeliverAsync(raw, final, mode, s).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Diktat-Verarbeitung fehlgeschlagen");
            Report(AppStatus.Error, ex.Message);
        }
        finally
        {
            TryDeleteTemp(wavPath);
            ReleaseBusy();
        }
    }

    private async Task DeliverAsync(string raw, string final, ProcessingMode mode, AppSettings s)
    {
        switch (s.PostRecordBehavior)
        {
            case PostRecordBehavior.ShowPreview:
                PreviewRequested?.Invoke(new DictationResult(raw, final, mode));
                Report(AppStatus.Ready);
                break;

            case PostRecordBehavior.CopyOnly:
                clipboard.SetText(final);
                Report(AppStatus.Inserted, "In Zwischenablage kopiert");
                break;

            case PostRecordBehavior.InsertRaw:
                await insertion.InsertAsync(raw, InsertMethod.ClipboardPaste, _target).ConfigureAwait(false);
                Report(AppStatus.Inserted);
                break;

            case PostRecordBehavior.TranscribeOptimizeInsert:
            default:
                await insertion.InsertAsync(final, InsertMethod.ClipboardPaste, _target).ConfigureAwait(false);
                Report(AppStatus.Inserted);
                break;
        }
    }

    private void Report(AppStatus status, string? message = null) => StatusChanged?.Invoke(status, message);

    private void ReleaseBusy()
    {
        _busyScope?.Dispose();
        _busyScope = null;
    }

    private static void TryDeleteTemp(string? path)
    {
        if (path is null) return;
        try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
        catch (Exception ex) { Log.Warning(ex, "Temporäre WAV konnte nicht gelöscht werden: {Path}", path); }
    }
}
