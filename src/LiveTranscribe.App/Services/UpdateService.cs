using System.Reflection;
using LiveTranscribe.Core.Abstractions;
using LiveTranscribe.Core.Models;
using Serilog;
using Velopack;
using Velopack.Sources;

namespace LiveTranscribe.App.Services;

/// <summary>
/// Auto-update via Velopack against the public GitHub Releases of this app.
/// Updates come ONLY from finished release artifacts — never from source or a
/// branch build. Installs are gated on <see cref="IAppBusyState"/> so they never
/// interrupt recording/transcription/insertion.
/// </summary>
public sealed class UpdateService(IAppBusyState busy, ISettingsService settings) : IUpdateService
{
    private const string RepoUrl = "https://github.com/lookynch/Live_transcribe";

    private readonly object _gate = new();
    private UpdateManager? _manager;
    private bool _managerPrerelease;
    private UpdateInfo? _pending;

    public string CurrentVersion
    {
        get
        {
            try
            {
                var v = TryGetManager(settings.Current.Update.AllowPrerelease)?.CurrentVersion;
                if (v is not null) return v.ToString();
            }
            catch { /* not installed (dev run) — fall back to assembly version */ }

            return Assembly.GetExecutingAssembly()
                       .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                       .Split('+')[0]
                   ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                   ?? "0.0.0";
        }
    }

    public string SourceDescription => RepoUrl;

    public bool UpdatePending
    {
        get { lock (_gate) return _pending is not null; }
    }

    public async Task<UpdateCheckResult> CheckAsync(bool allowPrerelease, CancellationToken ct = default)
    {
        try
        {
            var mgr = TryGetManager(allowPrerelease);
            if (mgr is null || !mgr.IsInstalled)
            {
                Log.Information("Update-Prüfung übersprungen: App läuft nicht aus einer Velopack-Installation");
                return UpdateCheckResult.UpToDate;
            }

            var info = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);

            settings.Current.Update.LastCheckUtc = DateTimeOffset.UtcNow;
            await settings.SaveAsync().ConfigureAwait(false);

            if (info is null)
            {
                lock (_gate) _pending = null;
                Log.Information("Keine Updates verfügbar");
                return UpdateCheckResult.UpToDate;
            }

            lock (_gate) _pending = info;
            var asset = info.TargetFullRelease;
            Log.Information("Update verfügbar: {Version}", asset.Version);

            return new UpdateCheckResult
            {
                UpdateAvailable = true,
                NewVersion = asset.Version.ToString(),
                ReleaseNotes = asset.NotesMarkdown
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Update-Prüfung fehlgeschlagen");
            return UpdateCheckResult.Failed(ex.Message);
        }
    }

    public async Task<bool> DownloadAndApplyWhenIdleAsync(CancellationToken ct = default)
    {
        UpdateInfo? info;
        lock (_gate) info = _pending;
        if (info is null) return false;

        var mgr = TryGetManager(settings.Current.Update.AllowPrerelease);
        if (mgr is null || !mgr.IsInstalled) return false;

        try
        {
            Log.Information("Lade Update {Version} herunter", info.TargetFullRelease.Version);
            await mgr.DownloadUpdatesAsync(info, cancelToken: ct).ConfigureAwait(false);

            while (busy.IsBusy)
            {
                Log.Information("Update bereit — wird im Leerlauf installiert (App ist gerade beschäftigt)");
                await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
            }

            Log.Information("Wende Update {Version} an und starte neu", info.TargetFullRelease.Version);
            mgr.ApplyUpdatesAndRestart(info);
            return true; // process exits before reaching here
        }
        catch (OperationCanceledException)
        {
            Log.Information("Update-Installation abgebrochen");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Update-Installation fehlgeschlagen — vorherige Version bleibt aktiv");
            return false;
        }
    }

    private UpdateManager? TryGetManager(bool allowPrerelease)
    {
        lock (_gate)
        {
            if (_manager is null || _managerPrerelease != allowPrerelease)
            {
                var source = new GithubSource(RepoUrl, accessToken: null, prerelease: allowPrerelease);
                _manager = new UpdateManager(source);
                _managerPrerelease = allowPrerelease;
            }
            return _manager;
        }
    }
}
