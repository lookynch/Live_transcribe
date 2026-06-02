using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using Serilog;

namespace LiveTranscribe.App.Services;

/// <summary>
/// Orchestrates one dictation run end to end: capture foreground → record →
/// local transcription → optional OpenAI rework → insert into the focused field
/// (or clipboard). The temp WAV is always deleted, and <see cref="IAppBusyState"/>
/// is held for the whole run so updates can't interrupt it.
/// </summary>
public sealed class DictationCoordinator
{
    private readonly IAudioRecordingService recorder;
    private readonly ILocalSpeechToTextService speech;
    private readonly IOpenAiTextOptimizationService optimizer;
    private readonly ITextInsertionService insertion;
    private readonly IActiveWindowService activeWindow;
    private readonly IFocusedFieldProbe fieldProbe;
    private readonly IClipboardService clipboard;
    private readonly ISettingsService settings;
    private readonly IAppBusyState busy;
    private readonly ILiveTranscriptionService live;

    public DictationCoordinator(
        IAudioRecordingService recorder,
        ILocalSpeechToTextService speech,
        IOpenAiTextOptimizationService optimizer,
        ITextInsertionService insertion,
        IActiveWindowService activeWindow,
        IFocusedFieldProbe fieldProbe,
        IClipboardService clipboard,
        ISettingsService settings,
        IAppBusyState busy,
        ILiveTranscriptionService live)
    {
        this.recorder = recorder;
        this.speech = speech;
        this.optimizer = optimizer;
        this.insertion = insertion;
        this.activeWindow = activeWindow;
        this.fieldProbe = fieldProbe;
        this.clipboard = clipboard;
        this.settings = settings;
        this.busy = busy;
        this.live = live;
        live.PartialTranscript += text => PartialTranscript?.Invoke(text);
    }

    private IntPtr _target;
    private IDisposable? _busyScope;

    public bool IsRecording => recorder.IsRecording;

    public event Action<AppStatus, string?>? StatusChanged;

    /// <summary>Approximate partial transcript shown live while recording (from the preview model).</summary>
    public event Action<string>? PartialTranscript;
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
            if (settings.Current.LiveTranscription.Enabled)
                live.Start(settings.Current.Language);
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
            await live.StopAsync().ConfigureAwait(false);
            wavPath = await recorder.StopAsync().ConfigureAwait(false);

            var s = settings.Current;
            Report(AppStatus.TranscribingLocal);
            var raw = (await speech.TranscribeAsync(wavPath, s.Language).ConfigureAwait(false)).Trim();

            if (string.IsNullOrWhiteSpace(raw))
            {
                Report(AppStatus.Error, "Keine Sprache erkannt");
                return;
            }

            var mode = s.DefaultProcessingMode;
            var final = raw;
            string? note = null;
            if (mode != ProcessingMode.TranscribeOnly)
            {
                Report(AppStatus.Optimizing);
                var opt = await optimizer
                    .OptimizeAsync(raw, mode, s.DefaultTone, s.CustomInstruction)
                    .ConfigureAwait(false);
                final = opt.Text;
                // Always tell the user whether OpenAI actually reworked the text, or why it didn't.
                note = opt.UsedOpenAi ? "mit OpenAI" : $"{opt.Notice} – Rohtext";
            }

            await DeliverAsync(final, note).ConfigureAwait(false);
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

    /// <summary>
    /// Like a phone keyboard: if the user's focus is a text field, type the text straight in;
    /// otherwise there is nowhere to type, so leave it on the clipboard to paste manually. No
    /// per-mode setting — the behaviour is automatic.
    /// </summary>
    private async Task DeliverAsync(string final, string? note)
    {
        if (fieldProbe.IsEditableFieldFocused())
        {
            // InsertAsync pastes via the clipboard and, if that fails, types the text instead —
            // so a briefly-locked clipboard can't lose the dictation.
            await insertion.InsertAsync(final, InsertMethod.ClipboardPaste, _target).ConfigureAwait(false);
            Report(AppStatus.Inserted, note);
        }
        else
        {
            var clip = "in Zwischenablage";
            try
            {
                clipboard.SetText(final);
                Report(AppStatus.Inserted, note is null ? $"Kein Textfeld aktiv – {clip}" : $"{note}, {clip}");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Zwischenablage nicht verfügbar");
                Report(AppStatus.Error, "Zwischenablage belegt – bitte erneut versuchen");
            }
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
