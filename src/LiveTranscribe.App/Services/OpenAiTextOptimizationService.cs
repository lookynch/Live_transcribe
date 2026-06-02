using System.ClientModel;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using LiveTranscribe.Core.Prompting;
using OpenAI.Chat;
using Serilog;

namespace LiveTranscribe.App.Services;

/// <summary>
/// Optional OpenAI rework of the already-transcribed TEXT. Never sends audio.
/// On any failure (missing/invalid key, network, rate limit, empty result) it
/// falls back to the raw text when the user has that enabled; otherwise it throws.
/// </summary>
public sealed class OpenAiTextOptimizationService(
    ISecureCredentialService credentials,
    ISettingsService settings) : IOpenAiTextOptimizationService
{
    public async Task<OptimizationResult> OptimizeAsync(
        string text, ProcessingMode mode, Tone tone, string? customInstruction, CancellationToken ct = default)
    {
        if (mode == ProcessingMode.TranscribeOnly || string.IsNullOrWhiteSpace(text))
            return new OptimizationResult(text, UsedOpenAi: false, Notice: null);

        try
        {
            var apiKey = credentials.GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return Fallback(text, "Kein OpenAI-Key hinterlegt");

            var systemPrompt = PromptBuilder.BuildSystemPrompt(mode, tone, customInstruction);
            var client = new ChatClient(settings.Current.OpenAiModel, apiKey);

            var completion = await client.CompleteChatAsync(
                [new SystemChatMessage(systemPrompt), new UserChatMessage(text)],
                new ChatCompletionOptions { Temperature = 0.3f },
                ct);

            var result = completion.Value.Content.Count > 0
                ? completion.Value.Content[0].Text?.Trim()
                : null;

            if (string.IsNullOrWhiteSpace(result))
                return Fallback(text, "OpenAI lieferte leeren Text zurück");

            return OptimizationResult.FromOpenAi(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ClientResultException ex)
        {
            var reason = ex.Status switch
            {
                401 => "OpenAI-Key ungültig",
                429 => "OpenAI-Kontingent erschöpft",
                _ => $"OpenAI-Fehler (HTTP {ex.Status})"
            };
            return Fallback(text, reason, ex);
        }
        catch (Exception ex)
        {
            return Fallback(text, "OpenAI-Anfrage fehlgeschlagen", ex);
        }
    }

    private OptimizationResult Fallback(string raw, string reason, Exception? ex = null)
    {
        if (settings.Current.FallbackToRawOnOpenAiError)
        {
            Log.Warning(ex, "OpenAI-Optimierung übersprungen ({Reason}); Rohtext wird verwendet", reason);
            return OptimizationResult.Fallback(raw, reason);
        }

        Log.Error(ex, "OpenAI-Optimierung fehlgeschlagen ({Reason}); Fallback deaktiviert", reason);
        throw new InvalidOperationException(reason, ex);
    }
}
