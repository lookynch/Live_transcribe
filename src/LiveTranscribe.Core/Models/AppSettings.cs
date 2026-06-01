namespace LiveTranscribe.Core.Models;

/// <summary>
/// User settings persisted as JSON in %AppData%\LiveTranscribe\settings.json.
/// The OpenAI API key is intentionally NOT stored here — it lives in the OS secure
/// store (DPAPI) via ISecureCredentialService.
/// </summary>
public sealed class AppSettings
{
    public HotkeyConfig PushToTalk { get; set; } = new(0x77); // F8
    public HotkeyConfig ToggleOverlay { get; set; } = new(0x78); // F9
    public HotkeyConfig StartStopRecord { get; set; } = new(0x79); // F10

    public WhisperModelType WhisperModel { get; set; } = WhisperModelType.BaseQ5_0;

    /// <summary>"de" (default), "en" or "auto".</summary>
    public string Language { get; set; } = "de";

    public PostRecordBehavior PostRecordBehavior { get; set; } = PostRecordBehavior.InsertRaw;
    public ProcessingMode DefaultProcessingMode { get; set; } = ProcessingMode.TranscribeOnly;
    public Tone DefaultTone { get; set; } = Tone.AutoDetect;
    public string CustomInstruction { get; set; } = string.Empty;

    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    public bool FallbackToRawOnOpenAiError { get; set; } = true;

    public OverlaySettings Overlay { get; set; } = new();
    public LiveTranscriptionSettings LiveTranscription { get; set; } = new();
    public UpdatePreferences Update { get; set; } = new();
    public UninstallPreferences Uninstall { get; set; } = new();

    public bool Autostart { get; set; }
    public bool HistoryEnabled { get; set; }
}

public sealed class OverlaySettings
{
    public double Left { get; set; } = 40;
    public double Top { get; set; } = 40;
    public bool AlwaysOnTop { get; set; } = true;
    public bool Minimized { get; set; }

    /// <summary>Whether the mode/tone options are expanded under the compact pill.</summary>
    public bool Expanded { get; set; }
}

/// <summary>
/// Settings for the live, in-progress transcript shown while recording. The live preview uses
/// a small fast model (default Tiny); the final inserted text always uses the main model.
/// </summary>
public sealed class LiveTranscriptionSettings
{
    public bool Enabled { get; set; } = true;
    public WhisperModelType PreviewModel { get; set; } = WhisperModelType.Tiny;
    public double RefreshSeconds { get; set; } = 1.75;
    public double WindowSeconds { get; set; } = 15;
}

public sealed class UpdatePreferences
{
    public bool CheckOnStartup { get; set; } = true;
    public bool AllowPrerelease { get; set; }
    public DateTimeOffset? LastCheckUtc { get; set; }
}

/// <summary>
/// Choices read by the Velopack uninstall callback. Velopack cannot show an
/// interactive prompt during uninstall, so the user configures this beforehand.
/// Defaults preserve user data.
/// </summary>
public sealed class UninstallPreferences
{
    public bool DeleteSettings { get; set; }
    public bool DeleteModels { get; set; }
    public bool DeleteApiKey { get; set; } = true;
}
