using System.Buffers.Binary;
using System.Text;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using Whisper.net;

namespace LiveTranscribe.Platform.Speech;

/// <summary>
/// Approximates live transcription: while recording, it accumulates the last N seconds of PCM
/// in memory and re-runs a small Whisper model (default Tiny) over that rolling window every
/// ~RefreshSeconds, raising the result via <see cref="PartialTranscript"/>. It keeps its OWN warm
/// factory so it never contends with the main model used for the final, accurate transcription.
/// </summary>
public sealed class LiveTranscriptionService : ILiveTranscriptionService, IDisposable
{
    private readonly IAudioRecordingService _recorder;
    private readonly IWhisperModelService _models;
    private readonly ISettingsService _settings;

    private readonly object _bufferGate = new();
    private readonly List<byte> _pcm = new();
    private int _sampleRate = 16000;
    private int _channels = 1;

    private WhisperFactory? _factory;
    private WhisperModelType? _loadedModel;

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private string _language = "de";

    public bool IsRunning { get; private set; }

    public event Action<string>? PartialTranscript;

    public LiveTranscriptionService(IAudioRecordingService recorder, IWhisperModelService models, ISettingsService settings)
    {
        _recorder = recorder;
        _models = models;
        _settings = settings;
    }

    public void Start(string language)
    {
        if (IsRunning) return;
        if (!_settings.Current.LiveTranscription.Enabled) return;

        _language = string.IsNullOrWhiteSpace(language) ? "de" : language;
        lock (_bufferGate) _pcm.Clear();

        _cts = new CancellationTokenSource();
        _recorder.SamplesAvailable += OnSamples;
        IsRunning = true;
        _loop = Task.Run(() => RunLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;
        IsRunning = false;

        _recorder.SamplesAvailable -= OnSamples;
        try { _cts?.Cancel(); } catch { /* ignore */ }

        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected */ }
            catch { /* live preview is best-effort; final transcription is authoritative */ }
        }

        _loop = null;
        _cts?.Dispose();
        _cts = null;
        lock (_bufferGate) _pcm.Clear();
    }

    private void OnSamples(object? sender, AudioFrame frame)
    {
        var max = (int)(_settings.Current.LiveTranscription.WindowSeconds * frame.SampleRate * 2); // 16-bit
        lock (_bufferGate)
        {
            _sampleRate = frame.SampleRate;
            _channels = frame.Channels;
            _pcm.AddRange(frame.Pcm16);
            var overflow = _pcm.Count - max;
            if (overflow > 0) _pcm.RemoveRange(0, overflow);
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        var cfg = _settings.Current.LiveTranscription;

        if (!_models.IsInstalled(cfg.PreviewModel))
        {
            // Don't block recording on a download; fetch in the background so live works next time.
            _ = Task.Run(() => _models.EnsureModelAsync(cfg.PreviewModel, null, CancellationToken.None));
            return;
        }

        WhisperFactory factory;
        try { factory = await GetFactoryAsync(cfg.PreviewModel, ct).ConfigureAwait(false); }
        catch { return; }

        var period = TimeSpan.FromSeconds(Math.Max(0.5, cfg.RefreshSeconds));
        using var timer = new PeriodicTimer(period);

        // ~1s of 16-bit mono is the minimum worth decoding.
        var minBytes = _sampleRate * 2;

        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            byte[] snapshot;
            int rate, channels;
            lock (_bufferGate)
            {
                if (_pcm.Count < minBytes) continue;
                snapshot = _pcm.ToArray();
                rate = _sampleRate;
                channels = _channels;
            }

            try
            {
                var text = await TranscribeWindowAsync(factory, snapshot, rate, channels, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(text))
                    PartialTranscript?.Invoke(text.Trim());
            }
            catch (OperationCanceledException) { break; }
            catch { /* skip this pass; the next tick retries */ }
        }
    }

    private async Task<string> TranscribeWindowAsync(WhisperFactory factory, byte[] pcm, int rate, int channels, CancellationToken ct)
    {
        var builder = factory.CreateBuilder();
        builder = _language == "auto" ? builder.WithLanguageDetection() : builder.WithLanguage(_language);

        await using var processor = builder.Build();
        using var wav = BuildWavStream(pcm, rate, channels);

        var sb = new StringBuilder();
        await foreach (var segment in processor.ProcessAsync(wav, ct).ConfigureAwait(false))
            sb.Append(segment.Text);
        return sb.ToString();
    }

    private async Task<WhisperFactory> GetFactoryAsync(WhisperModelType model, CancellationToken ct)
    {
        if (_factory is not null && _loadedModel == model) return _factory;

        var path = await _models.EnsureModelAsync(model, null, ct).ConfigureAwait(false);
        _factory?.Dispose();
        _factory = WhisperFactory.FromPath(path);
        _loadedModel = model;
        return _factory;
    }

    /// <summary>Wraps raw PCM-16 bytes in a canonical 44-byte WAV header (no temp file needed).</summary>
    private static MemoryStream BuildWavStream(byte[] pcm, int sampleRate, int channels)
    {
        const int bitsPerSample = 16;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;

        var ms = new MemoryStream(44 + pcm.Length);
        var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        w.Write("RIFF"u8.ToArray());
        w.Write(36 + pcm.Length);
        w.Write("WAVE"u8.ToArray());
        w.Write("fmt "u8.ToArray());
        w.Write(16);                       // PCM fmt chunk size
        w.Write((short)1);                 // PCM
        w.Write((short)channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write((short)blockAlign);
        w.Write((short)bitsPerSample);
        w.Write("data"u8.ToArray());
        w.Write(pcm.Length);
        w.Write(pcm);
        w.Flush();

        ms.Position = 0;
        return ms;
    }

    public void Dispose()
    {
        try { _recorder.SamplesAvailable -= OnSamples; } catch { /* ignore */ }
        _cts?.Cancel();
        _cts?.Dispose();
        _factory?.Dispose();
        _factory = null;
    }
}
