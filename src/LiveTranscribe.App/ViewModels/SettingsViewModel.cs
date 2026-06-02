using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;

namespace LiveTranscribe.App.ViewModels;

/// <summary>Backs the settings window: dictation, OpenAI key, autostart, updates, uninstall.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ISecureCredentialService _credentials;
    private readonly IAutostartService _autostart;
    private readonly IUpdateService _updates;
    private readonly IGlobalHotkeyService _hotkeys;

    public SettingsViewModel(
        ISettingsService settings,
        ISecureCredentialService credentials,
        IAutostartService autostart,
        IUpdateService updates,
        IGlobalHotkeyService hotkeys)
    {
        _settings = settings;
        _credentials = credentials;
        _autostart = autostart;
        _updates = updates;
        _hotkeys = hotkeys;

        var s = settings.Current;
        _selectedModel = Models.First(m => m.Value == s.WhisperModel);
        _selectedLanguage = Languages.FirstOrDefault(l => l.Value == s.Language) ?? Languages[0];
        _openAiModel = s.OpenAiModel;
        _fallbackToRaw = s.FallbackToRawOnOpenAiError;
        _customInstruction = s.CustomInstruction;
        _backgroundInfo = s.BackgroundInfo;
        _autostartEnabled = autostart.IsEnabled();
        _checkOnStartup = s.Update.CheckOnStartup;
        _allowPrerelease = s.Update.AllowPrerelease;
        _deleteSettingsOnUninstall = s.Uninstall.DeleteSettings;
        _deleteModelsOnUninstall = s.Uninstall.DeleteModels;
        _deleteApiKeyOnUninstall = s.Uninstall.DeleteApiKey;
        _hasApiKey = credentials.HasApiKey;

        _pushToTalkHotkey = Clone(s.PushToTalk);
        _startStopHotkey = Clone(s.StartStopRecord);
        _toggleOverlayHotkey = Clone(s.ToggleOverlay);
        _liveEnabled = s.LiveTranscription.Enabled;

        CurrentVersion = updates.CurrentVersion;
        UpdateSource = updates.SourceDescription;
        LastCheck = s.Update.LastCheckUtc?.LocalDateTime.ToString("g") ?? "noch nie";
    }

    // ── Dictation ─────────────────────────────────────────────────────────
    public IReadOnlyList<EnumOption<WhisperModelType>> Models { get; } =
        EnumDisplay.Options<WhisperModelType>(EnumDisplay.Describe);
    public IReadOnlyList<EnumOption<string>> Languages { get; } = new[]
    {
        new EnumOption<string>("de", "Deutsch"),
        new EnumOption<string>("en", "Englisch"),
        new EnumOption<string>("auto", "Automatisch")
    };

    [ObservableProperty] private EnumOption<WhisperModelType> _selectedModel;
    [ObservableProperty] private EnumOption<string> _selectedLanguage;
    [ObservableProperty] private string _openAiModel;
    [ObservableProperty] private bool _fallbackToRaw;
    [ObservableProperty] private string _customInstruction;
    [ObservableProperty] private string _backgroundInfo;

    // ── OpenAI key (DPAPI) ────────────────────────────────────────────────
    [ObservableProperty] private bool _hasApiKey;
    [ObservableProperty] private string _apiKeyInput = string.Empty;

    // ── Autostart ─────────────────────────────────────────────────────────
    [ObservableProperty] private bool _autostartEnabled;

    // ── Hotkeys & Live-Vorschau ───────────────────────────────────────────
    [ObservableProperty] private HotkeyConfig _pushToTalkHotkey;
    [ObservableProperty] private HotkeyConfig _startStopHotkey;
    [ObservableProperty] private HotkeyConfig _toggleOverlayHotkey;
    [ObservableProperty] private bool _liveEnabled;

    private static HotkeyConfig Clone(HotkeyConfig c) => new(c.VirtualKey, c.Modifiers);

    // ── Updates ───────────────────────────────────────────────────────────
    public string CurrentVersion { get; }
    public string UpdateSource { get; }
    [ObservableProperty] private string _lastCheck;
    [ObservableProperty] private bool _checkOnStartup;
    [ObservableProperty] private bool _allowPrerelease;
    [ObservableProperty] private string? _updateStatus;
    [ObservableProperty] private bool _updateAvailable;
    [ObservableProperty] private string? _availableVersion;
    [ObservableProperty] private string? _releaseNotes;

    // ── Uninstall preferences ─────────────────────────────────────────────
    [ObservableProperty] private bool _deleteSettingsOnUninstall;
    [ObservableProperty] private bool _deleteModelsOnUninstall;
    [ObservableProperty] private bool _deleteApiKeyOnUninstall;

    [RelayCommand]
    private void SaveApiKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKeyInput)) return;
        _credentials.StoreApiKey(ApiKeyInput.Trim());
        ApiKeyInput = string.Empty;
        HasApiKey = true;
    }

    [RelayCommand]
    private void ClearApiKey()
    {
        _credentials.DeleteApiKey();
        HasApiKey = false;
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        UpdateStatus = "Suche nach Updates…";
        UpdateAvailable = false;
        var result = await _updates.CheckAsync(AllowPrerelease);
        LastCheck = DateTime.Now.ToString("g");

        if (result.Error is not null)
            UpdateStatus = $"Fehler: {result.Error}";
        else if (result.UpdateAvailable)
        {
            UpdateAvailable = true;
            AvailableVersion = result.NewVersion;
            ReleaseNotes = result.ReleaseNotes;
            UpdateStatus = $"Update verfügbar: {result.NewVersion}";
        }
        else
            UpdateStatus = "Sie verwenden die neueste Version.";
    }

    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        UpdateStatus = "Update wird heruntergeladen und im Leerlauf installiert…";
        await _updates.DownloadAndApplyWhenIdleAsync();
    }

    /// <summary>Persists all editable settings. Called when the user closes the window.</summary>
    public void Persist()
    {
        var s = _settings.Current;
        s.WhisperModel = SelectedModel.Value;
        s.Language = SelectedLanguage.Value;
        s.OpenAiModel = string.IsNullOrWhiteSpace(OpenAiModel) ? "gpt-4o-mini" : OpenAiModel.Trim();
        s.FallbackToRawOnOpenAiError = FallbackToRaw;
        s.CustomInstruction = CustomInstruction;
        s.BackgroundInfo = BackgroundInfo;
        s.Update.CheckOnStartup = CheckOnStartup;
        s.Update.AllowPrerelease = AllowPrerelease;
        s.Uninstall.DeleteSettings = DeleteSettingsOnUninstall;
        s.Uninstall.DeleteModels = DeleteModelsOnUninstall;
        s.Uninstall.DeleteApiKey = DeleteApiKeyOnUninstall;
        s.Autostart = AutostartEnabled;
        s.PushToTalk = Clone(PushToTalkHotkey);
        s.StartStopRecord = Clone(StartStopHotkey);
        s.ToggleOverlay = Clone(ToggleOverlayHotkey);
        s.LiveTranscription.Enabled = LiveEnabled;
        _settings.Save();
        _hotkeys.Reload(s);
    }

    partial void OnAutostartEnabledChanged(bool value)
    {
        if (value) _autostart.Enable();
        else _autostart.Disable();
    }
}
