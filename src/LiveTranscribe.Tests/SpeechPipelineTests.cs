using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using LiveTranscribe.Platform.Audio;
using LiveTranscribe.Platform.Speech;
using NAudio.Wave;

namespace LiveTranscribe.Tests;

/// <summary>
/// End-to-end verification of the offline speech pipeline WITHOUT a human: Windows TTS
/// synthesizes a known sentence to a 16 kHz mono WAV, the real production transcription
/// service decodes it, and we assert the spoken words come back. This exercises the
/// Whisper.net native runtime + ggml model + decode path exactly as the app uses it.
/// </summary>
public sealed class SpeechPipelineTests
{
    private sealed class FakeSettings : ISettingsService
    {
        public FakeSettings(AppSettings s) => Current = s;
        public AppSettings Current { get; }
        public Task SaveAsync() => Task.CompletedTask;
        public void Save() { }
        public event EventHandler? Changed { add { } remove { } }
    }

    private static string Synthesize(string text)
    {
        using var synth = new SpeechSynthesizer();
        if (synth.GetInstalledVoices().Count(v => v.Enabled) == 0)
            return string.Empty; // no TTS voice on this machine — caller skips

        var path = Path.Combine(Path.GetTempPath(), $"lt_tts_{Guid.NewGuid():N}.wav");
        var fmt = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
        synth.SetOutputToWaveFile(path, fmt);
        synth.Speak(text);
        synth.SetOutputToNull();
        return path;
    }

    [Fact]
    public async Task Transcribes_synthesized_speech_to_expected_words()
    {
        var wav = Synthesize("The quick brown fox jumps over the lazy dog.");
        if (wav.Length == 0) return; // environment has no speech voice installed

        try
        {
            var models = new WhisperModelService();
            // Only run for real where the model is already present (e.g. a dev box). On a clean
            // CI runner we skip rather than download ~55 MB from Hugging Face mid-pipeline.
            if (!models.IsInstalled(WhisperModelType.BaseQ5_0)) return;

            var settings = new FakeSettings(new AppSettings { WhisperModel = WhisperModelType.BaseQ5_0, Language = "en" });
            using var stt = new LocalSpeechToTextService(models, settings);

            var text = (await stt.TranscribeAsync(wav, "en")).ToLowerInvariant();

            Assert.False(string.IsNullOrWhiteSpace(text), "Transcription returned empty text.");
            string[] expected = { "fox", "brown", "quick", "dog", "lazy" };
            Assert.True(expected.Any(text.Contains),
                $"Transcript did not contain any expected word. Got: '{text}'");
        }
        finally
        {
            try { File.Delete(wav); } catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task Recorder_writes_a_valid_wav_when_a_device_exists()
    {
        if (WaveInEvent.DeviceCount == 0) return; // no capture device in this environment

        var recorder = new AudioRecordingService();
        recorder.Start();
        await Task.Delay(600);
        var path = await recorder.StopAsync();

        try
        {
            Assert.True(File.Exists(path), "No WAV file produced.");
            using var reader = new WaveFileReader(path);
            Assert.Equal(16000, reader.WaveFormat.SampleRate);
            Assert.Equal(1, reader.WaveFormat.Channels);
        }
        finally
        {
            try { File.Delete(path); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Rework_prompt_instructs_the_model_to_keep_formatting()
    {
        var prompt = LiveTranscribe.Core.Prompting.PromptBuilder.BuildSystemPrompt(ProcessingMode.ComposeEmail, Tone.Professional);
        Assert.Contains("Formatierung", prompt);
    }
}
