# Release erstellen

Releases entstehen automatisch per GitHub Actions, sobald ein **Tag `vX.Y.Z`** gepusht wird.
Der Tag ist die **einzige Quelle der Wahrheit** für die Version — App, Installer und
GitHub-Release-Tag stimmen dadurch immer überein.

## Ablauf

1. `CHANGELOG.md` um einen Abschnitt für die neue Version ergänzen.
2. Änderungen committen und pushen.
3. Tag setzen und pushen:

   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```

4. Der Workflow [`.github/workflows/release.yml`](.github/workflows/release.yml) läuft auf
   `windows-latest` und führt aus:
   - Version aus dem Tag ableiten (führendes `v` entfernen),
   - `dotnet test`,
   - `dotnet publish` (win-x64, framework-dependent) mit `-p:Version=$ver`,
   - `vpk download github` (lädt das letzte Release für Delta-Updates),
   - `vpk pack` (erstellt Setup, Portable-ZIP, full/delta `.nupkg`, `releases.win.json`),
   - `vpk upload github --publish` (lädt die Artefakte ins GitHub-Release).

## Pre-Releases

Tag mit Suffix verwenden, z. B. `v1.1.0-beta.1`. Der Workflow erkennt das Suffix und
veröffentlicht mit `--pre`. Nutzer erhalten Pre-Releases nur, wenn sie in den Einstellungen
„Pre-Releases erlauben“ aktiviert haben.

## Release-Assets

`vpk pack` erzeugt und lädt hoch:

| Asset | Zweck |
| --- | --- |
| `LiveTranscribe-Setup.exe` | Installer für Endnutzer |
| `LiveTranscribe-Portable.zip` | portable Variante |
| `LiveTranscribe-{ver}-full.nupkg` | Voll-Paket (Update-Quelle) |
| `LiveTranscribe-{ver}-delta.nupkg` | Delta gegen die Vorversion |
| `releases.win.json` | wird vom `UpdateManager` der App gelesen |
| `RELEASES` | Legacy-Index |

## Voraussetzungen am Repo

- Repository ist **öffentlich** (die App liest Releases ohne Token).
- `GITHUB_TOKEN` (von Actions bereitgestellt) genügt zum Veröffentlichen.
- **Wichtig:** Nach dem Anlegen des Repos die Konstante `RepoUrl` in
  `src/LiveTranscribe.App/Services/UpdateService.cs` auf die echte Repo-URL setzen.

## Code-Signing (produktiv)

Im MVP unsigniert. Für signierte Builds:

1. Zertifikat als Secret `CODE_SIGN_PFX_BASE64` und Passwort als `CODE_SIGN_PASSWORD` anlegen.
2. Im Workflow das `.pfx` in eine temporäre Datei dekodieren und `vpk pack` um
   `--signParams "/td sha256 /fd sha256 /tr <rfc3161-URL>"` ergänzen, danach die Datei löschen.
3. **Niemals** ein `.pfx` ins Repo committen (`.gitignore` schließt `*.pfx` aus).

Eine reproduzierbare Schritt-für-Schritt-Anleitung steht in [build/RELEASING.md](build/RELEASING.md).
