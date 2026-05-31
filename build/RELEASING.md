# RELEASING — reproduzierbare vpk-Schritte

Diese Datei dokumentiert, wie sich ein Release **lokal** exakt so erzeugen lässt, wie es die
CI tut. Nützlich zum Debuggen der Pipeline.

## Voraussetzungen

```powershell
dotnet --version           # 8.x
dotnet tool install -g vpk # Velopack CLI
```

`vpk` muss auf dem PATH sein (`%USERPROFILE%\.dotnet\tools`).

## Schritte (Beispiel v1.0.0)

```powershell
$ver  = "1.0.0"
$repo = "https://github.com/<OWNER>/<REPO>"   # echte URL einsetzen

# 1. Aufräumen
Remove-Item publish, Releases -Recurse -Force -ErrorAction SilentlyContinue

# 2. Tests
dotnet test src/LiveTranscribe.Tests/LiveTranscribe.Tests.csproj -c Release

# 3. Publish (framework-dependent, win-x64)
dotnet publish src/LiveTranscribe.App/LiveTranscribe.App.csproj `
  -c Release -r win-x64 --self-contained false -p:Version=$ver -o publish

# 4. Letztes Release für Delta-Updates laden (optional, ignoriert Fehler beim ersten Release)
vpk download github --repoUrl $repo

# 5. Paketieren
vpk pack `
  --packId LiveTranscribe `
  --packVersion $ver `
  --packDir publish `
  --mainExe LiveTranscribe.exe `
  --icon build/app.ico `
  --releaseNotes build/notes.md `
  --channel win

# 6. Veröffentlichen (Token mit repo-Scope nötig; in CI = GITHUB_TOKEN)
vpk upload github --repoUrl $repo --publish `
  --releaseName "Live Transcribe $ver" --tag v$ver --token $env:GH_TOKEN
```

Pre-Release: bei Schritt 6 zusätzlich `--pre` und einen Tag wie `v1.1.0-beta.1`.

## Wichtige Konstanten

- `--packId` muss stabil bleiben (`LiveTranscribe`) — er identifiziert die App über Updates hinweg.
- `--mainExe LiveTranscribe.exe` entspricht `AssemblyName=LiveTranscribe`.
- Die in der App fest hinterlegte Update-Quelle steht in
  `src/LiveTranscribe.App/Services/UpdateService.cs` (`RepoUrl`) und muss mit `$repo` übereinstimmen.

## Artefakte

Liegen nach `vpk pack` unter `.\Releases\win\` und werden von `vpk upload github` ins
GitHub-Release geladen. Sie gehören **nicht** ins Repo (siehe `.gitignore`).
