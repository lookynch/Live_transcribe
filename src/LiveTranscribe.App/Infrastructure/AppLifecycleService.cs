using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using LiveTranscribe.App.Services;
using LiveTranscribe.App.ViewModels;
using LiveTranscribe.App.Views;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LiveTranscribe.App.Infrastructure;

/// <summary>
/// Owns the app's runtime presence: tray icon, global hotkeys, the overlay window,
/// preview routing, and the optional startup update check. Created and torn down by
/// the generic host.
/// </summary>
public sealed class AppLifecycleService(
    IServiceProvider services,
    OverlayWindow overlay,
    IGlobalHotkeyService hotkeys,
    ISettingsService settings,
    IUpdateService updates,
    IWhisperModelService models,
    ILocalSpeechToTextService speech) : IHostedService
{
    private TaskbarIcon? _tray;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Dispatch(() =>
        {
            CreateTray();

            hotkeys.Reload(settings.Current);
            hotkeys.PushToTalkDown += (_, _) => Dispatch(() => _ = StartRecordingSafe());
            hotkeys.PushToTalkUp += (_, _) => Dispatch(() => _ = StopRecordingSafe());
            hotkeys.StartStopPressed += (_, _) => Dispatch(ToggleRecording);
            hotkeys.ToggleOverlayPressed += (_, _) => Dispatch(ToggleOverlay);
            hotkeys.Start();

            if (!settings.Current.Overlay.Minimized) overlay.Show();
        });

        _ = PrewarmModelAsync();

        if (settings.Current.Update.CheckOnStartup)
            _ = CheckForUpdatesAtStartupAsync();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Downloads (if needed) and loads the Whisper model into memory at startup so the first
    /// dictation is fast and doesn't silently stall behind a multi-second download. Progress is
    /// shown on the overlay so the user always sees what's happening.
    /// </summary>
    private async Task PrewarmModelAsync()
    {
        try
        {
            var model = settings.Current.WhisperModel;
            if (!models.IsInstalled(model))
            {
                OverlayVm.SetStatus("Sprachmodell wird geladen…");
                var progress = new Progress<double>(bytes =>
                    OverlayVm.SetStatus($"Sprachmodell wird geladen… {bytes / 1_048_576.0:0} MB"));
                await models.EnsureModelAsync(model, progress);
            }

            OverlayVm.SetStatus("Sprachmodell wird vorbereitet…");
            await speech.PrewarmAsync();
            OverlayVm.SetStatus("Bereit");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Vorwärmen des Sprachmodells fehlgeschlagen");
            OverlayVm.SetStatus("Modell-Ladefehler – bitte Einstellungen prüfen");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispatch(() =>
        {
            try { hotkeys.Dispose(); } catch (Exception ex) { Log.Warning(ex, "Hotkey-Dispose fehlgeschlagen"); }
            _tray?.Dispose();
            overlay.Close();
        });
        return Task.CompletedTask;
    }

    private OverlayViewModel OverlayVm => (OverlayViewModel)overlay.DataContext;

    private Task StartRecordingSafe() => OverlayVm.StartRecordingAsync();
    private Task StopRecordingSafe() => OverlayVm.StopRecordingAsync();

    private void ToggleRecording() => _ = OverlayVm.ToggleRecordingCommand.ExecuteAsync(null);

    private void ToggleOverlay()
    {
        if (overlay.IsVisible) overlay.Hide();
        else overlay.Show();
        settings.Current.Overlay.Minimized = !overlay.IsVisible;
        settings.Save();
    }

    private void CreateTray()
    {
        _tray = new TaskbarIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            ToolTipText = "Live Transcribe"
        };

        var menu = new ContextMenu();

        var openSettings = new MenuItem { Header = "Einstellungen" };
        openSettings.Click += (_, _) => ShowSettings();
        menu.Items.Add(openSettings);

        var toggle = new MenuItem { Header = "Overlay ein-/ausblenden" };
        toggle.Click += (_, _) => ToggleOverlay();
        menu.Items.Add(toggle);

        menu.Items.Add(new Separator());

        var quit = new MenuItem { Header = "Beenden" };
        quit.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(quit);

        _tray.ContextMenu = menu;
        _tray.TrayMouseDoubleClick += (_, _) => ShowSettings();
    }

    private void ShowSettings()
    {
        var existing = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existing is not null) { existing.ShowAndFocus(); return; }

        var window = services.GetRequiredService<SettingsWindow>();
        window.ShowAndFocus();
    }

    private async Task CheckForUpdatesAtStartupAsync()
    {
        try
        {
            var result = await updates.CheckAsync(settings.Current.Update.AllowPrerelease);
            if (!result.UpdateAvailable) return;

            Dispatch(() => _tray?.ShowBalloonTip(
                "Update wird installiert",
                $"Version {result.NewVersion} wird im Hintergrund geladen und im Leerlauf installiert.",
                BalloonIcon.Info));

            // Lädt das Update und wendet es an, sobald die App idle ist (kein laufendes
            // Diktat). Startet die App danach automatisch neu — kein Nutzerklick nötig.
            await updates.DownloadAndApplyWhenIdleAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Automatisches Update beim Start fehlgeschlagen");
        }
    }

    private static void Dispatch(Action action)
    {
        var app = Application.Current;
        if (app is null) return;
        if (app.Dispatcher.CheckAccess()) action();
        else app.Dispatcher.Invoke(action);
    }
}
