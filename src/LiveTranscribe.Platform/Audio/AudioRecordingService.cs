using LiveTranscribe.Core;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using NAudio.Wave;

namespace LiveTranscribe.Platform.Audio;

/// <summary>
/// Captures the default microphone directly into a temporary 16 kHz / 16-bit / mono
/// WAV file — exactly the format Whisper expects, so no resampling is needed.
/// </summary>
public sealed class AudioRecordingService : IAudioRecordingService
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _currentPath;
    private TaskCompletionSource<string>? _stopTcs;

    public bool IsRecording { get; private set; }

    public event EventHandler<float>? LevelChanged;
    public event EventHandler<AudioFrame>? SamplesAvailable;

    public void Start()
    {
        if (IsRecording) return;

        if (WaveInEvent.DeviceCount == 0)
            throw new InvalidOperationException("Kein Mikrofon gefunden. Bitte ein Aufnahmegerät anschließen und prüfen, ob der Zugriff erlaubt ist.");

        _currentPath = Path.Combine(AppPaths.TempDir, $"rec_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.wav");
        _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 16, 1) };
        _writer = new WaveFileWriter(_currentPath, _waveIn.WaveFormat);

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        IsRecording = true;
        _waveIn.StartRecording();
    }

    public Task<string> StopAsync()
    {
        if (!IsRecording || _waveIn is null)
            return Task.FromResult(_currentPath ?? string.Empty);

        _stopTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        IsRecording = false;
        _waveIn.StopRecording(); // completes asynchronously via RecordingStopped
        return _stopTcs.Task;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        _writer?.Write(e.Buffer, 0, e.BytesRecorded);

        float peak = 0f;
        for (var i = 0; i + 1 < e.BytesRecorded; i += 2)
        {
            var sample = Math.Abs(BitConverter.ToInt16(e.Buffer, i) / 32768f);
            if (sample > peak) peak = sample;
        }
        LevelChanged?.Invoke(this, peak);

        // Hand a copy of this buffer to live consumers (NAudio reuses e.Buffer).
        var samples = SamplesAvailable;
        if (samples is not null && _waveIn is not null)
        {
            var copy = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, copy, e.BytesRecorded);
            var fmt = _waveIn.WaveFormat;
            samples.Invoke(this, new AudioFrame(copy, fmt.SampleRate, fmt.Channels));
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _writer?.Dispose();
        _writer = null;

        if (_waveIn is not null)
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.RecordingStopped -= OnRecordingStopped;
            _waveIn.Dispose();
            _waveIn = null;
        }

        _stopTcs?.TrySetResult(_currentPath ?? string.Empty);
    }
}
