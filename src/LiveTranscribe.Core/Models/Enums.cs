namespace LiveTranscribe.Core.Models;

/// <summary>How the recognized text is processed before it is inserted.</summary>
public enum ProcessingMode
{
    /// <summary>
    /// Smart default: treats the dictation as either an instruction to fulfil (returning only the
    /// result, e.g. a finished email) or as text to improve. Uses OpenAI.
    /// </summary>
    Assistant,
    TranscribeOnly,
    FixSpellingGrammar,
    ComposeEmail,
    ReplyEmail,
    ChatMessage,
    MoreProfessional,
    ShorterConcise,
    Technical,
    Friendlier,
    BetterAiPrompt,
    Custom
}

/// <summary>Tone applied only when an OpenAI rework mode is selected.</summary>
public enum Tone
{
    AutoDetect,
    Professional,
    Friendly,
    Direct,
    Formal,
    Casual,
    Technical,
    Neutral,
    Confident,
    Apologetic
}

/// <summary>Local Whisper model. Quantized variants preferred for low CPU/RAM use.</summary>
public enum WhisperModelType
{
    Tiny,
    Base,
    BaseQ5_0,
    Small
}

/// <summary>What happens after transcription/optimization completes.</summary>
public enum PostRecordBehavior
{
    InsertRaw,
    TranscribeOptimizeInsert,
    ShowPreview,
    CopyOnly
}

/// <summary>Insertion strategy into the previously focused window.</summary>
public enum InsertMethod
{
    ClipboardPaste,
    Keystrokes
}

/// <summary>Live status shown on the overlay.</summary>
public enum AppStatus
{
    Ready,
    Recording,
    TranscribingLocal,
    Optimizing,
    Inserted,
    Error
}

/// <summary>Modifier keys for a configurable global hotkey (flags).</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}
