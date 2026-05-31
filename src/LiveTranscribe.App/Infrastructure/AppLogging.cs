using LiveTranscribe.Core;
using Serilog;
using Serilog.Core;
using Velopack.Logging;

namespace LiveTranscribe.App.Infrastructure;

public static class AppLogging
{
    public static void Configure()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                AppPaths.AppLogFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                AppPaths.UpdateLogFile,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                levelSwitch: UpdateLogSwitch)
            .CreateLogger();
    }

    /// <summary>Update-related entries are tagged so they also land in update.log.</summary>
    public static readonly LoggingLevelSwitch UpdateLogSwitch = new(Serilog.Events.LogEventLevel.Information);
}

/// <summary>Bridges Velopack's diagnostics into Serilog so updates are fully traceable.</summary>
public sealed class SerilogVelopackLogger : IVelopackLogger
{
    public void Log(VelopackLogLevel logLevel, string? message, Exception? exception)
    {
        var level = logLevel switch
        {
            VelopackLogLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
            VelopackLogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            VelopackLogLevel.Information => Serilog.Events.LogEventLevel.Information,
            VelopackLogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            VelopackLogLevel.Error => Serilog.Events.LogEventLevel.Error,
            VelopackLogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Information
        };
        Serilog.Log.Write(level, exception, "[Velopack] {Message}", message ?? string.Empty);
    }
}
