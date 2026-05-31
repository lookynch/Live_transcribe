namespace LiveTranscribe.Core;

/// <summary>
/// Centralizes all per-user storage locations. Nothing is stored in the program
/// directory — settings in %AppData%, models/logs/temp in %LocalAppData%.
/// </summary>
public static class AppPaths
{
    public const string AppFolderName = "LiveTranscribe";

    public static string Roaming { get; } = Ensure(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName));

    public static string Local { get; } = Ensure(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName));

    public static string SettingsFile => Path.Combine(Roaming, "settings.json");
    public static string ModelsDir { get; } = Ensure(Path.Combine(Local, "models"));
    public static string LogsDir { get; } = Ensure(Path.Combine(Local, "logs"));
    public static string TempDir { get; } = Ensure(Path.Combine(Local, "temp"));

    public static string UpdateLogFile => Path.Combine(LogsDir, "update.log");
    public static string AppLogFile => Path.Combine(LogsDir, "app-.log");

    private static string Ensure(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
