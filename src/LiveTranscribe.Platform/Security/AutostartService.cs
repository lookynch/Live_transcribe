using LiveTranscribe.Core.Abstractions;
using Microsoft.Win32;

namespace LiveTranscribe.Platform.Security;

/// <summary>
/// Toggles "start with Windows" via the HKCU Run key. Uses the stable root stub exe
/// path (passed in) so the entry survives Velopack updates that replace current\.
/// </summary>
public sealed class AutostartService : IAutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LiveTranscribe";

    private readonly string _launchCommand;

    public AutostartService(string stubExePath)
    {
        _launchCommand = $"\"{stubExePath}\" --autostart";
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is not null;
    }

    public void Enable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        key.SetValue(ValueName, _launchCommand, RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue(ValueName) is not null)
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>Static removal used by the uninstall callback (no instance/stub path needed).</summary>
    public static void RemoveEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue(ValueName) is not null)
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
