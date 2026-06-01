using System.Text;
using LiveTranscribe.Core.Models;

namespace LiveTranscribe.Core.Prompting;

/// <summary>
/// Builds the OpenAI system prompt from the selected processing mode and tone.
/// Used only for text rework — transcription is always local.
/// </summary>
public static class PromptBuilder
{
    public static string BuildSystemPrompt(ProcessingMode mode, Tone tone, string? customInstruction = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Du bist ein Assistent, der diktierten, lokal transkribierten Text überarbeitet. " +
            "Gib ausschließlich den überarbeiteten Text zurück — ohne Anführungszeichen, " +
            "ohne Vorbemerkung, ohne Erklärung. Behalte die Sprache des Eingabetextes bei. " +
            "Übernimm und nutze sinnvolle Formatierung: Absätze und Zeilenumbrüche, sowie " +
            "Aufzählungen (mit „- “) oder nummerierte Listen, wo sie den Text klarer gliedern. " +
            "Erfinde keine Formatierung, wo Fließtext angemessen ist.");
        sb.AppendLine();
        sb.AppendLine("Aufgabe: " + DescribeMode(mode, customInstruction));

        if (mode != ProcessingMode.BetterAiPrompt)
        {
            var toneText = DescribeTone(tone, mode);
            if (!string.IsNullOrEmpty(toneText))
            {
                sb.AppendLine();
                sb.AppendLine("Tonalität: " + toneText);
            }
        }

        return sb.ToString().TrimEnd();
    }

    public static string DescribeMode(ProcessingMode mode, string? customInstruction = null) => mode switch
    {
        ProcessingMode.TranscribeOnly =>
            "Gib den Text unverändert zurück.",
        ProcessingMode.FixSpellingGrammar =>
            "Korrigiere Rechtschreibung, Grammatik und Zeichensetzung. Inhalt und Bedeutung nicht verändern.",
        ProcessingMode.ComposeEmail =>
            "Formuliere aus dem Diktat eine vollständige, gut strukturierte E-Mail mit passender Anrede und Grußformel.",
        ProcessingMode.ReplyEmail =>
            "Formuliere eine höfliche, klare E-Mail-Antwort auf Basis des Diktats.",
        ProcessingMode.ChatMessage =>
            "Formuliere eine kurze, direkte und verständliche Chat-/Teams-Nachricht.",
        ProcessingMode.MoreProfessional =>
            "Formuliere den Text professioneller und geschäftstauglicher, ohne den Inhalt zu ändern.",
        ProcessingMode.ShorterConcise =>
            "Kürze den Text und mach ihn präziser, ohne wichtige Informationen zu verlieren.",
        ProcessingMode.Technical =>
            "Formuliere den Text sachlich, eindeutig und technisch präzise, ohne Floskeln.",
        ProcessingMode.Friendlier =>
            "Formuliere den Text freundlicher und zugänglicher, ohne unprofessionell zu wirken.",
        ProcessingMode.BetterAiPrompt =>
            "Wandle das Diktat in einen präzisen, strukturierten und vollständigen KI-Prompt um. " +
            "Ergänze sinnvolle Details und Anforderungen, behalte aber die Absicht bei.",
        ProcessingMode.Custom =>
            string.IsNullOrWhiteSpace(customInstruction)
                ? "Verbessere den Text sinnvoll."
                : customInstruction!.Trim(),
        _ => "Verbessere den Text sinnvoll."
    };

    private static string DescribeTone(Tone tone, ProcessingMode mode) => tone switch
    {
        Tone.AutoDetect =>
            "Erkenne anhand von Text und Aufgabe selbst, welcher Ton angemessen ist.",
        Tone.Professional => "professionell",
        Tone.Friendly => "freundlich",
        Tone.Direct => "direkt",
        Tone.Formal => "förmlich",
        Tone.Casual => "locker",
        Tone.Technical => "technisch und sachlich",
        Tone.Neutral => "neutral",
        Tone.Confident => "selbstbewusst",
        Tone.Apologetic => "entschuldigend",
        _ => string.Empty
    };
}
