using LiveTranscribe.Core;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using Whisper.net.Ggml;

namespace LiveTranscribe.Platform.Speech;

/// <summary>
/// Downloads and tracks ggml Whisper models under %LocalAppData%\LiveTranscribe\models.
/// Quantized variants are preferred to reduce CPU/RAM use.
/// </summary>
public sealed class WhisperModelService : IWhisperModelService
{
    private static readonly IReadOnlyDictionary<WhisperModelType, (GgmlType Ggml, QuantizationType Quant, string File, string Name)> Map =
        new Dictionary<WhisperModelType, (GgmlType, QuantizationType, string, string)>
        {
            [WhisperModelType.Tiny]    = (GgmlType.Tiny, QuantizationType.NoQuantization, "ggml-tiny.bin",        "Tiny (schnellste, geringste Qualität)"),
            [WhisperModelType.Base]    = (GgmlType.Base, QuantizationType.NoQuantization, "ggml-base.bin",        "Base (Standard)"),
            [WhisperModelType.BaseQ5_0]= (GgmlType.Base, QuantizationType.Q5_0,           "ggml-base-q5_0.bin",   "Base Q5_0 (quantisiert, empfohlen)"),
            [WhisperModelType.Small]   = (GgmlType.Small, QuantizationType.NoQuantization,"ggml-small.bin",       "Small (beste Qualität, langsamer)"),
        };

    public IReadOnlyList<WhisperModelInfo> Available { get; } =
        Map.Select(kv => new WhisperModelInfo(kv.Key, kv.Value.Name, kv.Value.File)).ToList();

    public string GetModelPath(WhisperModelType type) =>
        Path.Combine(AppPaths.ModelsDir, Map[type].File);

    public bool IsInstalled(WhisperModelType type) => File.Exists(GetModelPath(type));

    public async Task<string> EnsureModelAsync(
        WhisperModelType type, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var path = GetModelPath(type);
        if (File.Exists(path)) return path;

        var (ggml, quant, _, _) = Map[type];
        var tmp = path + ".download";

        await using (var src = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggml, quant, ct))
        await using (var dst = File.Create(tmp))
        {
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await src.ReadAsync(buffer, ct)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                total += read;
                progress?.Report(total); // length unknown from the HF stream; report bytes
            }
        }

        File.Move(tmp, path, overwrite: true);
        return path;
    }

    public void DeleteAllModels()
    {
        if (!Directory.Exists(AppPaths.ModelsDir)) return;
        foreach (var file in Directory.EnumerateFiles(AppPaths.ModelsDir))
        {
            try { File.Delete(file); } catch (IOException) { /* best effort */ }
        }
    }
}
