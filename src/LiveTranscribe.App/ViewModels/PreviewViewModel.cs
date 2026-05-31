using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveTranscribe.App.Services;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;

namespace LiveTranscribe.App.ViewModels;

/// <summary>Backs the preview window shown when PostRecordBehavior = ShowPreview.</summary>
public sealed partial class PreviewViewModel : ObservableObject
{
    private readonly IClipboardService _clipboard;
    private readonly ITextInsertionService _insertion;
    private readonly IntPtr _target;

    [ObservableProperty] private string _rawText;
    [ObservableProperty] private string _finalText;

    public bool HasOptimization { get; }

    /// <summary>Raised to ask the view to close (true = something was inserted/copied).</summary>
    public event Action<bool>? CloseRequested;

    public PreviewViewModel(DictationResult result, IntPtr target, IClipboardService clipboard, ITextInsertionService insertion)
    {
        _clipboard = clipboard;
        _insertion = insertion;
        _target = target;
        _rawText = result.RawText;
        _finalText = result.FinalText;
        HasOptimization = result.Mode != ProcessingMode.TranscribeOnly &&
                          !string.Equals(result.RawText, result.FinalText, StringComparison.Ordinal);
    }

    [RelayCommand]
    private async Task InsertAsync()
    {
        await _insertion.InsertAsync(FinalText, InsertMethod.ClipboardPaste, _target);
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Copy()
    {
        _clipboard.SetText(FinalText);
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Discard() => CloseRequested?.Invoke(false);
}
