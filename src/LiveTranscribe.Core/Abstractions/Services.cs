using LiveTranscribe.Core.Models;

namespace LiveTranscribe.Core.Abstractions;

/// <summary>Tracks whether a dictation operation is in progress. Updates are gated on idle.</summary>
public interface IAppBusyState
{
    bool IsBusy { get; }
    IDisposable Enter();
    event EventHandler? Changed;
}

/// <summary>Records microphone audio to a temporary 16 kHz mono WAV file.</summary>
public interface IAudioRecordingService
{
    bool IsRecording { get; }
    void Start();
    /// <summary>Stops recording and returns the path to the written WAV file.</summary>
    Task<string> StopAsync();
    event EventHandler<float>? LevelChanged;
}

/// <summary>Local, offline speech-to-text. No audio ever leaves the machine.</summary>
public interface ILocalSpeechToTextService
{
    Task<string> TranscribeAsync(string wavPath, string language, CancellationToken ct = default);
}

/// <summary>Manages download/availability of ggml Whisper models in %LocalAppData%.</summary>
public interface IWhisperModelService
{
    IReadOnlyList<WhisperModelInfo> Available { get; }
    bool IsInstalled(WhisperModelType type);
    Task<string> EnsureModelAsync(WhisperModelType type, IProgress<double>? progress = null, CancellationToken ct = default);
    void DeleteAllModels();
}

/// <summary>Optional OpenAI rework of the transcribed TEXT (never audio).</summary>
public interface IOpenAiTextOptimizationService
{
    Task<string> OptimizeAsync(string text, ProcessingMode mode, Tone tone, string? customInstruction, CancellationToken ct = default);
}

/// <summary>Inserts text into the previously active window.</summary>
public interface ITextInsertionService
{
    Task InsertAsync(string text, InsertMethod method, IntPtr targetWindow);
}

/// <summary>Captures/restores the foreground window so the overlay never becomes the insertion target.</summary>
public interface IActiveWindowService
{
    IntPtr CaptureForeground();
    void RestoreForeground(IntPtr handle);
}

/// <summary>Clipboard get/set with snapshot+restore.</summary>
public interface IClipboardService
{
    ClipboardSnapshot Capture();
    void Restore(ClipboardSnapshot snapshot);
    void SetText(string text);
}

/// <summary>Synthesizes keyboard input via SendInput (Ctrl+V paste, Unicode typing).</summary>
public interface IInputSimulator
{
    void SendPaste();
    void TypeText(string text);
}

/// <summary>Low-level global hotkeys with push-to-talk (KeyDown-once / KeyUp) semantics.</summary>
public interface IGlobalHotkeyService : IDisposable
{
    void Start();
    void Reload(AppSettings settings);
    event EventHandler? PushToTalkDown;
    event EventHandler? PushToTalkUp;
    event EventHandler? ToggleOverlayPressed;
    event EventHandler? StartStopPressed;
}

/// <summary>Loads and persists <see cref="AppSettings"/>.</summary>
public interface ISettingsService
{
    AppSettings Current { get; }
    Task SaveAsync();
    void Save();
    event EventHandler? Changed;
}

/// <summary>Stores the OpenAI API key in the OS secure store (DPAPI).</summary>
public interface ISecureCredentialService
{
    void StoreApiKey(string apiKey);
    string? GetApiKey();
    bool HasApiKey { get; }
    void DeleteApiKey();
}

/// <summary>Checks GitHub Releases and applies updates only when the app is idle.</summary>
public interface IUpdateService
{
    string CurrentVersion { get; }
    string SourceDescription { get; }
    Task<UpdateCheckResult> CheckAsync(bool allowPrerelease, CancellationToken ct = default);
    /// <summary>Downloads the pending update and applies it once the app is idle, then restarts.</summary>
    Task<bool> DownloadAndApplyWhenIdleAsync(CancellationToken ct = default);
    bool UpdatePending { get; }
}

/// <summary>Toggles "start with Windows" via the HKCU Run key.</summary>
public interface IAutostartService
{
    bool IsEnabled();
    void Enable();
    void Disable();
}
