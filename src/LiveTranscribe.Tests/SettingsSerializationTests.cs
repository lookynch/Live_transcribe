using System.Text.Json;
using System.Text.Json.Serialization;
using LiveTranscribe.Core.Models;

namespace LiveTranscribe.Tests;

public class SettingsSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void RoundTrips_PreservingValues()
    {
        var original = new AppSettings
        {
            WhisperModel = WhisperModelType.Small,
            Language = "en",
            DefaultProcessingMode = ProcessingMode.ComposeEmail,
            DefaultTone = Tone.Friendly,
            PostRecordBehavior = PostRecordBehavior.ShowPreview,
            OpenAiModel = "gpt-4o",
            FallbackToRawOnOpenAiError = false,
            CustomInstruction = "Test",
            Autostart = true
        };
        original.Overlay.Left = 123;
        original.Update.AllowPrerelease = true;
        original.Uninstall.DeleteModels = true;

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<AppSettings>(json, Options)!;

        Assert.Equal(original.WhisperModel, restored.WhisperModel);
        Assert.Equal(original.Language, restored.Language);
        Assert.Equal(original.DefaultProcessingMode, restored.DefaultProcessingMode);
        Assert.Equal(original.DefaultTone, restored.DefaultTone);
        Assert.Equal(original.PostRecordBehavior, restored.PostRecordBehavior);
        Assert.Equal(original.OpenAiModel, restored.OpenAiModel);
        Assert.False(restored.FallbackToRawOnOpenAiError);
        Assert.Equal("Test", restored.CustomInstruction);
        Assert.True(restored.Autostart);
        Assert.Equal(123, restored.Overlay.Left);
        Assert.True(restored.Update.AllowPrerelease);
        Assert.True(restored.Uninstall.DeleteModels);
    }

    [Fact]
    public void Enums_SerializeAsStrings()
    {
        var json = JsonSerializer.Serialize(new AppSettings(), Options);
        Assert.Contains("\"BaseQ5_0\"", json);
        Assert.Contains("\"Assistant\"", json);
    }

    [Fact]
    public void Defaults_AreSensible()
    {
        var s = new AppSettings();
        Assert.Equal("de", s.Language);
        Assert.Equal(WhisperModelType.BaseQ5_0, s.WhisperModel);
        Assert.Equal(ProcessingMode.Assistant, s.DefaultProcessingMode);
        Assert.True(s.FallbackToRawOnOpenAiError);
        Assert.True(s.Update.CheckOnStartup);
        Assert.True(s.Uninstall.DeleteApiKey);
        Assert.Equal(0x77, s.PushToTalk.VirtualKey);
    }
}
