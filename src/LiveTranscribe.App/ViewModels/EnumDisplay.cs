using LiveTranscribe.Core.Models;

namespace LiveTranscribe.App.ViewModels;

/// <summary>A selectable enum value with a German display label, for combo boxes.</summary>
public sealed record EnumOption<T>(T Value, string Label)
{
    public override string ToString() => Label;
}

/// <summary>German display labels for the user-facing enums.</summary>
public static class EnumDisplay
{
    public static string Describe(ProcessingMode mode) => mode switch
    {
        ProcessingMode.TranscribeOnly => "Nur transkribieren",
        ProcessingMode.FixSpellingGrammar => "Rechtschreibung & Grammatik",
        ProcessingMode.ComposeEmail => "E-Mail verfassen",
        ProcessingMode.ReplyEmail => "E-Mail beantworten",
        ProcessingMode.ChatMessage => "Chat-/Teams-Nachricht",
        ProcessingMode.MoreProfessional => "Professioneller",
        ProcessingMode.ShorterConcise => "Kürzer & prägnanter",
        ProcessingMode.Technical => "Technisch & sachlich",
        ProcessingMode.Friendlier => "Freundlicher",
        ProcessingMode.BetterAiPrompt => "Besserer KI-Prompt",
        ProcessingMode.Custom => "Eigene Anweisung",
        _ => mode.ToString()
    };

    public static string Describe(Tone tone) => tone switch
    {
        Tone.AutoDetect => "Automatisch erkennen",
        Tone.Professional => "Professionell",
        Tone.Friendly => "Freundlich",
        Tone.Direct => "Direkt",
        Tone.Formal => "Förmlich",
        Tone.Casual => "Locker",
        Tone.Technical => "Technisch",
        Tone.Neutral => "Neutral",
        Tone.Confident => "Selbstbewusst",
        Tone.Apologetic => "Entschuldigend",
        _ => tone.ToString()
    };

    public static string Describe(WhisperModelType model) => model switch
    {
        WhisperModelType.Tiny => "Tiny (schnell, ungenau)",
        WhisperModelType.Base => "Base",
        WhisperModelType.BaseQ5_0 => "Base Q5_0 (empfohlen)",
        WhisperModelType.Small => "Small (genau, langsamer)",
        _ => model.ToString()
    };

    public static string Describe(PostRecordBehavior behavior) => behavior switch
    {
        PostRecordBehavior.InsertRaw => "Rohtext direkt einfügen",
        PostRecordBehavior.TranscribeOptimizeInsert => "Optimieren & einfügen",
        PostRecordBehavior.ShowPreview => "Vorschau anzeigen",
        PostRecordBehavior.CopyOnly => "Nur in Zwischenablage",
        _ => behavior.ToString()
    };

    public static string Describe(AppStatus status) => status switch
    {
        AppStatus.Ready => "Bereit",
        AppStatus.Recording => "Aufnahme…",
        AppStatus.TranscribingLocal => "Transkribiere lokal…",
        AppStatus.Optimizing => "Optimiere mit OpenAI…",
        AppStatus.Inserted => "Eingefügt",
        AppStatus.Error => "Fehler",
        _ => status.ToString()
    };

    public static IReadOnlyList<EnumOption<T>> Options<T>(Func<T, string> describe) where T : struct, Enum =>
        Enum.GetValues<T>().Select(v => new EnumOption<T>(v, describe(v))).ToList();
}
