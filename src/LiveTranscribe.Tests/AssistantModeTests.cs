using LiveTranscribe.App.Services;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using LiveTranscribe.Core.Prompting;

namespace LiveTranscribe.Tests;

/// <summary>
/// Covers the v1.3.0 behaviour: the instruction-aware Assistant prompt, and the visible
/// outcome of an OpenAI rework when no key is configured (fallback to raw + a user-facing reason).
/// </summary>
public sealed class AssistantModeTests
{
    private sealed class NoKeyCredentials : ISecureCredentialService
    {
        public bool HasApiKey => false;
        public string? GetApiKey() => null;
        public void StoreApiKey(string apiKey) { }
        public void DeleteApiKey() { }
    }

    private sealed class FixedSettings : ISettingsService
    {
        public FixedSettings(AppSettings s) => Current = s;
        public AppSettings Current { get; }
        public Task SaveAsync() => Task.CompletedTask;
        public void Save() { }
        public event EventHandler? Changed { add { } remove { } }
    }

    [Fact]
    public void Assistant_prompt_tells_the_model_to_execute_instructions_and_return_only_the_result()
    {
        var prompt = PromptBuilder.BuildSystemPrompt(ProcessingMode.Assistant, Tone.AutoDetect);
        Assert.Contains("Anweisung", prompt);
        Assert.Contains("AUSSCHLIESSLICH", prompt);
    }

    [Fact]
    public void Background_info_is_woven_into_the_system_prompt_when_present()
    {
        var prompt = PromptBuilder.BuildSystemPrompt(
            ProcessingMode.Assistant, Tone.AutoDetect, customInstruction: null,
            backgroundInfo: "Jakob Kaiser, Einkauf bei Jeremias GmbH");

        Assert.Contains("Hintergrundinformationen", prompt);
        Assert.Contains("Jeremias GmbH", prompt);
    }

    [Fact]
    public void Background_info_is_omitted_when_empty()
    {
        var prompt = PromptBuilder.BuildSystemPrompt(ProcessingMode.Assistant, Tone.AutoDetect);
        Assert.DoesNotContain("Hintergrundinformationen", prompt);
    }

    [Fact]
    public async Task Optimize_without_key_falls_back_to_raw_with_a_visible_reason()
    {
        var svc = new OpenAiTextOptimizationService(new NoKeyCredentials(), new FixedSettings(new AppSettings()));

        var result = await svc.OptimizeAsync("schreibe eine mail an herrn büttner", ProcessingMode.Assistant, Tone.AutoDetect, null);

        Assert.False(result.UsedOpenAi);
        Assert.Equal("schreibe eine mail an herrn büttner", result.Text);
        Assert.Equal("Kein OpenAI-Key hinterlegt", result.Notice);
    }

    [Fact]
    public async Task TranscribeOnly_never_calls_openai()
    {
        var svc = new OpenAiTextOptimizationService(new NoKeyCredentials(), new FixedSettings(new AppSettings()));

        var result = await svc.OptimizeAsync("roher text", ProcessingMode.TranscribeOnly, Tone.AutoDetect, null);

        Assert.False(result.UsedOpenAi);
        Assert.Null(result.Notice);
        Assert.Equal("roher text", result.Text);
    }
}
