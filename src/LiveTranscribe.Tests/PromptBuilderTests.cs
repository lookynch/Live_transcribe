using LiveTranscribe.Core.Models;
using LiveTranscribe.Core.Prompting;

namespace LiveTranscribe.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void IncludesTone_ForReworkModes()
    {
        var prompt = PromptBuilder.BuildSystemPrompt(ProcessingMode.FixSpellingGrammar, Tone.Professional);
        Assert.Contains("Tonalität", prompt);
        Assert.Contains("professionell", prompt);
    }

    [Fact]
    public void OmitsTone_ForBetterAiPrompt()
    {
        var prompt = PromptBuilder.BuildSystemPrompt(ProcessingMode.BetterAiPrompt, Tone.Formal);
        Assert.DoesNotContain("Tonalität", prompt);
    }

    [Fact]
    public void Custom_UsesCustomInstruction()
    {
        const string instruction = "Schreibe alles in Großbuchstaben.";
        var prompt = PromptBuilder.BuildSystemPrompt(ProcessingMode.Custom, Tone.Neutral, instruction);
        Assert.Contains(instruction, prompt);
    }

    [Fact]
    public void Custom_WithoutInstruction_FallsBack()
    {
        var prompt = PromptBuilder.BuildSystemPrompt(ProcessingMode.Custom, Tone.Neutral, "   ");
        Assert.Contains("Verbessere den Text", prompt);
    }

    [Theory]
    [InlineData(ProcessingMode.FixSpellingGrammar)]
    [InlineData(ProcessingMode.ComposeEmail)]
    [InlineData(ProcessingMode.ReplyEmail)]
    [InlineData(ProcessingMode.ChatMessage)]
    [InlineData(ProcessingMode.MoreProfessional)]
    [InlineData(ProcessingMode.ShorterConcise)]
    [InlineData(ProcessingMode.Technical)]
    [InlineData(ProcessingMode.Friendlier)]
    [InlineData(ProcessingMode.BetterAiPrompt)]
    public void EveryMode_ProducesNonEmptyTask(ProcessingMode mode)
    {
        var description = PromptBuilder.DescribeMode(mode);
        Assert.False(string.IsNullOrWhiteSpace(description));
    }

    [Fact]
    public void AllModeTonePairs_BuildWithoutError()
    {
        foreach (var mode in Enum.GetValues<ProcessingMode>())
        foreach (var tone in Enum.GetValues<Tone>())
        {
            var prompt = PromptBuilder.BuildSystemPrompt(mode, tone, "Beispielanweisung");
            Assert.False(string.IsNullOrWhiteSpace(prompt));
        }
    }
}
