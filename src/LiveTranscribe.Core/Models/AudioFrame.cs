namespace LiveTranscribe.Core.Models;

/// <summary>
/// A copied slice of captured PCM-16 audio handed to live consumers. The buffer is owned
/// by the consumer (NAudio reuses its own buffers), so it is safe to retain.
/// </summary>
public readonly record struct AudioFrame(byte[] Pcm16, int SampleRate, int Channels);
