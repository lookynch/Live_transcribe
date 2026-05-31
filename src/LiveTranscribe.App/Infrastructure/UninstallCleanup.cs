using System.Diagnostics;
using System.IO;
using System.Text.Json;
using LiveTranscribe.Core;
using LiveTranscribe.Core.Models;
using LiveTranscribe.Platform.Security;
using Serilog;

namespace LiveTranscribe.App.Infrastructure;

/// <summary>
/// Runs inside Velopack's OnBeforeUninstallFastCallback. Velopack removes the program
/// files, Start Menu and Desktop shortcuts and the Apps&amp;Features entry itself; this
/// handles the rest. Velopack cannot show an interactive prompt during uninstall, so the
/// keep/delete choices are read from settings the user configured beforehand. Must finish
/// quickly (~30 s budget).
/// </summary>
public static class UninstallCleanup
{
    public static void Run()
    {
        try
        {
            var settings = LoadSettings();

            // Always remove machine-touching state.
            AutostartService.RemoveEntry();
            TerminateRunningInstances();
            DeleteTempFiles();

            if (settings.Uninstall.DeleteApiKey)
                new DpapiCredentialService().DeleteApiKey();

            if (settings.Uninstall.DeleteModels)
                SafeDeleteDirectory(AppPaths.ModelsDir);

            if (settings.Uninstall.DeleteSettings)
            {
                SafeDelete(AppPaths.SettingsFile);
                SafeDeleteDirectory(AppPaths.Roaming);
            }

            Log.Information("Uninstall cleanup complete (deleteApiKey={Key}, deleteModels={Models}, deleteSettings={Settings})",
                settings.Uninstall.DeleteApiKey, settings.Uninstall.DeleteModels, settings.Uninstall.DeleteSettings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Uninstall cleanup failed");
        }
    }

    private static AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(AppPaths.SettingsFile))
            {
                var json = File.ReadAllText(AppPaths.SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* fall through to defaults */ }
        return new AppSettings();
    }

    private static void TerminateRunningInstances()
    {
        var self = Environment.ProcessId;
        foreach (var p in Process.GetProcessesByName("LiveTranscribe"))
        {
            try { if (p.Id != self) { p.Kill(); p.WaitForExit(3000); } }
            catch { /* best effort */ }
        }
    }

    private static void DeleteTempFiles() => SafeDeleteDirectory(AppPaths.TempDir);

    private static void SafeDelete(string file)
    {
        try { if (File.Exists(file)) File.Delete(file); } catch { }
    }

    private static void SafeDeleteDirectory(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { }
    }
}
