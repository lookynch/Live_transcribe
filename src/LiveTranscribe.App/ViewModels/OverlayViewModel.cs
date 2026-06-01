using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveTranscribe.App.Services;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;

namespace LiveTranscribe.App.ViewModels;

/// <summary>Drives the always-on-top mic overlay: status, mode/tone pickers, record toggle.</summary>
public sealed partial class OverlayViewModel : ObservableObject
{
    private readonly DictationCoordinator _coordinator;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _statusText = "Bereit";
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private double _level;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private string _livePartialText = string.Empty;

    [ObservableProperty] private EnumOption<ProcessingMode> _selectedMode;
    [ObservableProperty] private EnumOption<Tone> _selectedTone;

    public IReadOnlyList<EnumOption<ProcessingMode>> Modes { get; } =
        EnumDisplay.Options<ProcessingMode>(EnumDisplay.Describe);
    public IReadOnlyList<EnumOption<Tone>> Tones { get; } =
        EnumDisplay.Options<Tone>(EnumDisplay.Describe);

    /// <summary>Tone only applies when an OpenAI rework mode is selected.</summary>
    public bool IsToneEnabled => SelectedMode.Value is not ProcessingMode.TranscribeOnly and not ProcessingMode.BetterAiPrompt;

    public OverlayViewModel(DictationCoordinator coordinator, ISettingsService settings)
    {
        _coordinator = coordinator;
        _settings = settings;

        var s = settings.Current;
        _selectedMode = Modes.First(m => m.Value == s.DefaultProcessingMode);
        _selectedTone = Tones.First(t => t.Value == s.DefaultTone);
        _isExpanded = s.Overlay.Expanded;

        coordinator.StatusChanged += OnStatusChanged;
        coordinator.LevelChanged += (_, lvl) => OnUi(() => Level = lvl);
        coordinator.PartialTranscript += text => OnUi(() => LivePartialText = text);
    }

    partial void OnIsExpandedChanged(bool value)
    {
        _settings.Current.Overlay.Expanded = value;
        _settings.Save();
    }

    /// <summary>Sets a transient status line (e.g. startup model loading) directly on the overlay.</summary>
    public void SetStatus(string text) => OnUi(() => StatusText = text);

    private void OnStatusChanged(AppStatus status, string? message)
    {
        OnUi(() =>
        {
            StatusText = message is null ? EnumDisplay.Describe(status) : $"{EnumDisplay.Describe(status)}: {message}";
            IsRecording = status == AppStatus.Recording;
            IsBusy = status is AppStatus.Recording or AppStatus.TranscribingLocal or AppStatus.Optimizing;
            if (!IsRecording) Level = 0;
            // Clear the live preview once we leave recording (the final text is delivered separately).
            if (status is not (AppStatus.Recording or AppStatus.TranscribingLocal))
                LivePartialText = string.Empty;
        });
    }

    partial void OnSelectedModeChanged(EnumOption<ProcessingMode> value)
    {
        _settings.Current.DefaultProcessingMode = value.Value;
        _settings.Save();
        OnPropertyChanged(nameof(IsToneEnabled));
    }

    partial void OnSelectedToneChanged(EnumOption<Tone> value)
    {
        _settings.Current.DefaultTone = value.Value;
        _settings.Save();
    }

    [RelayCommand]
    private async Task ToggleRecordingAsync()
    {
        if (_coordinator.IsRecording)
            await _coordinator.StopAndProcessAsync();
        else
            _coordinator.StartRecording();
    }

    public async Task StartRecordingAsync()
    {
        if (!_coordinator.IsRecording) _coordinator.StartRecording();
        await Task.CompletedTask;
    }

    public Task StopRecordingAsync() =>
        _coordinator.IsRecording ? _coordinator.StopAndProcessAsync() : Task.CompletedTask;

    private static void OnUi(Action action)
    {
        var app = Application.Current;
        if (app is null || app.Dispatcher.CheckAccess()) action();
        else app.Dispatcher.Invoke(action);
    }
}
