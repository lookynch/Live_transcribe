using System.Text;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using Whisper.net;

namespace LiveTranscribe.Platform.Speech;

/// <summary>
/// Local, offline transcription via Whisper.net. Keeps one warm <see cref="WhisperFactory"/>
/// for the selected model so repeated dictations avoid the model reload cost; the factory is
/// rebuilt only when the user switches models. Fully async — never blocks the UI thread.
/// </summary>
public sealed class LocalSpeechToTextService : ILocalSpeechToTextService, IDisposable
{
    private readonly IWhisperModelService _models;
    private readonly ISettingsService _settings;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private WhisperFactory? _factory;
    private WhisperModelType? _loadedModel;

    public LocalSpeechToTextService(IWhisperModelService models, ISettingsService settings)
    {
        _models = models;
        _settings = settings;
    }

    public async Task<string> TranscribeAsync(string wavPath, string language, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(wavPath) || !File.Exists(wavPath))
            return string.Empty;

        var factory = await GetWarmFactoryAsync(ct);

        var builder = factory.CreateBuilder();
        builder = language == "auto" ? builder.WithLanguageDetection() : builder.WithLanguage(language);

        await using var processor = builder.Build();
        await using var stream = File.OpenRead(wavPath);

        var sb = new StringBuilder();
        await foreach (var segment in processor.ProcessAsync(stream, ct))
            sb.Append(segment.Text);

        return sb.ToString().Trim();
    }

    private async Task<WhisperFactory> GetWarmFactoryAsync(CancellationToken ct)
    {
        var model = _settings.Current.WhisperModel;
        if (_factory is not null && _loadedModel == model)
            return _factory;

        await _gate.WaitAsync(ct);
        try
        {
            if (_factory is not null && _loadedModel == model)
                return _factory;

            var path = await _models.EnsureModelAsync(model, progress: null, ct);

            _factory?.Dispose();
            _factory = WhisperFactory.FromPath(path);
            _loadedModel = model;
            return _factory;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _factory?.Dispose();
        _factory = null;
        _gate.Dispose();
    }
}
