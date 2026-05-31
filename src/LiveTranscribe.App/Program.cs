using System.Windows;
using LiveTranscribe.App.Infrastructure;
using Serilog;
using Velopack;

namespace LiveTranscribe.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Serilog is configured here so Velopack's own diagnostics share the same sink.
        AppLogging.Configure();

        // MUST be the very first thing the app does. Velopack uses this to handle
        // install/update/uninstall hook invocations and exits the process for them —
        // before any WPF window is created.
        VelopackApp.Build()
            .SetLogger(new SerilogVelopackLogger())
            .OnFirstRun(_ => Log.Information("First run after installation"))
            .OnBeforeUninstallFastCallback((_) => UninstallCleanup.Run())
            .Run();

        try
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception at startup");
            MessageBox.Show($"Live Transcribe konnte nicht gestartet werden:\n\n{ex.Message}",
                "Live Transcribe", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
