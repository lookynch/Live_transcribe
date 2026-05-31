# Installation

## Schnellinstallation (ohne Visual Studio)

1. Auf der [Releases-Seite](../../releases) die Datei **`LiveTranscribe-Setup.exe`** herunterladen.
2. Doppelklick. Es ist **kein Administrator** erforderlich — die Installation erfolgt pro Benutzer
   nach `%LocalAppData%\LiveTranscribe`.
3. Bei der SmartScreen-Warnung (unsigniertes MVP): „Weitere Informationen“ → „Trotzdem ausführen“.
4. Fehlt die **.NET 8 Desktop Runtime**, installiert das Setup sie automatisch mit (Velopack).
5. Nach der Installation startet die App und legt ein Startmenü-Eintrag an.

## Was wird wo abgelegt?

| Inhalt | Speicherort | Bei Update | Bei Deinstallation |
| --- | --- | --- | --- |
| Programmdateien | `%LocalAppData%\LiveTranscribe\current\` | ersetzt | entfernt |
| Stub/Updater | `%LocalAppData%\LiveTranscribe\` | bleibt | entfernt |
| Einstellungen | `%AppData%\LiveTranscribe\settings.json` | bleibt | optional |
| Whisper-Modelle | `%LocalAppData%\LiveTranscribe\models\` | bleibt | optional |
| Logs | `%LocalAppData%\LiveTranscribe\logs\` | bleibt | optional |
| OpenAI-Key | Windows DPAPI (kein Dateipfad im Programmordner) | bleibt | optional |

Die „optional“-Punkte richten sich nach der Auswahl unter
**Einstellungen → Bei Deinstallation** (Standard: Nutzerdaten behalten).

## Autostart

Unter **Einstellungen → Start → „Mit Windows starten“** aktivierbar. Dabei wird ein Eintrag
unter `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` gesetzt, der auf die stabile
Stub-EXE zeigt (überlebt Updates).

## Erststart-Checkliste

1. Mikrofon prüfen (Windows-Sound-Einstellungen).
2. Optional: OpenAI-API-Key in den Einstellungen hinterlegen (für Textoptimierung).
3. Whisper-Modell wählen — beim ersten Diktat wird es automatisch heruntergeladen.
4. **F8** in einer beliebigen App gedrückt halten und sprechen.

## Aus dem Quellcode bauen

```powershell
# .NET 8 SDK vorausgesetzt
dotnet build LiveTranscribe.sln -c Release
dotnet run --project src/LiveTranscribe.App
```
