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
