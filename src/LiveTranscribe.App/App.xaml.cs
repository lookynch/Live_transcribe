using System.Windows;
using LiveTranscribe.App.Infrastructure;
using LiveTranscribe.App.Services;
using LiveTranscribe.App.ViewModels;
using LiveTranscribe.App.Views;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Platform.Audio;
using LiveTranscribe.Platform.Interop;
using LiveTranscribe.Platform.Security;
using LiveTranscribe.Platform.Speech;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LiveTranscribe.App;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The overlay is shown manually by AppLifecycleService; exiting only via tray/quit.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var builder = Host.CreateApplicationBuilder();
        ConfigureServices(builder.Services);
        _host = builder.Build();
        _host.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _host?.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            _host?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Fehler beim Beenden des Hosts");
        }
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core/app services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAppBusyState, AppBusyState>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IOpenAiTextOptimizationService, OpenAiTextOptimizationService>();
        services.AddSingleton<IUpdateService, UpdateService>();

        // Platform services
        services.AddSingleton<ISecureCredentialService, DpapiCredentialService>();
        services.AddSingleton<IInputSimulator, InputSimulator>();
        services.AddSingleton<IActiveWindowService, ActiveWindowService>();
        services.AddSingleton<IFocusedFieldProbe, FocusedFieldProbe>();
        services.AddSingleton<ITextInsertionService, TextInsertionService>();
        services.AddSingleton<IAudioRecordingService, AudioRecordingService>();
        services.AddSingleton<IWhisperModelService, WhisperModelService>();
        services.AddSingleton<ILocalSpeechToTextService, LocalSpeechToTextService>();
        services.AddSingleton<ILiveTranscriptionService, LiveTranscriptionService>();
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        services.AddSingleton<IAutostartService>(_ => new AutostartService(Environment.ProcessPath!));

        // Pipeline + view models + views
        services.AddSingleton<DictationCoordinator>();
        services.AddSingleton<OverlayViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<OverlayWindow>();
        services.AddSingleton<Func<SettingsWindow>>(sp => () => sp.GetRequiredService<SettingsWindow>());
        services.AddTransient<SettingsWindow>();

        services.AddHostedService<AppLifecycleService>();
    }
}
