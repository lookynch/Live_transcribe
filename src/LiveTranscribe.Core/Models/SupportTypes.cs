namespace LiveTranscribe.Core.Models;

/// <summary>Snapshot of clipboard text so it can be restored after a paste-insert.</summary>
public sealed class ClipboardSnapshot
{
    public bool HadText { get; init; }
    public string? Text { get; init; }

    public static readonly ClipboardSnapshot Empty = new() { HadText = false, Text = null };
}

/// <summary>Result of an update check, surfaced to the settings UI.</summary>
public sealed class UpdateCheckResult
{
    public bool UpdateAvailable { get; init; }
    public string? NewVersion { get; init; }
    public string? ReleaseNotes { get; init; }
    public string? Error { get; init; }

    public static UpdateCheckResult UpToDate => new() { UpdateAvailable = false };
    public static UpdateCheckResult Failed(string error) => new() { Error = error };
}

/// <summary>Describes an available local Whisper model for the settings UI.</summary>
public sealed record WhisperModelInfo(WhisperModelType Type, string DisplayName, string FileName);

/// <summary>
/// Outcome of an OpenAI rework attempt. <see cref="UsedOpenAi"/> is true only when OpenAI
/// actually produced the text; on any fallback <see cref="Notice"/> carries a short, user-facing
/// reason (e.g. "OpenAI-Kontingent erschöpft") so the overlay can show what happened.
/// </summary>
public sealed record OptimizationResult(string Text, bool UsedOpenAi, string? Notice)
{
    public static OptimizationResult FromOpenAi(string text) => new(text, true, null);
    public static OptimizationResult Fallback(string raw, string notice) => new(raw, false, notice);
}
