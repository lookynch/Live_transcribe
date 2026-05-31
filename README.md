# Live Transcribe

Eine unauffällige Windows-Diktier-App: Sprich an beliebiger Stelle in Windows,
der Text wird **lokal** transkribiert und an der aktuellen Cursorposition eingefügt
— in Outlook, Word, Teams, im Browser, überall.

- **Transkription immer lokal** (Whisper). Es wird **kein Audio** an externe Dienste gesendet.
- **OpenAI nur für optionale Textoptimierung** (Rechtschreibung, E-Mail formulieren, Ton anpassen …).
  Bei „Nur transkribieren“ entstehen **keine API-Kosten** und nichts verlässt den Rechner.
- **Push-to-Talk** über einen globalen Hotkey (Standard: **F8** halten).
- Schwebendes **Overlay** + **Tray-Icon**, die nie den Fokus stehlen.
- **Auto-Update** über GitHub Releases (Velopack).

## Installation

`LiveTranscribe-Setup.exe` aus den [Releases](../../releases) herunterladen und ausführen.
Kein Administrator nötig — die App installiert sich pro Benutzer.

> **SmartScreen-Hinweis:** Das Installationsprogramm ist im MVP **nicht signiert**.
> Windows kann beim ersten Start warnen → „Weitere Informationen“ → „Trotzdem ausführen“.

> **.NET-Runtime:** Fehlt die .NET 8 Desktop Runtime, installiert das Setup sie automatisch mit.

Details in [INSTALL.md](INSTALL.md).

## Bedienung

1. Beim Start erscheinen Overlay und Tray-Icon.
2. **F8 gedrückt halten**, sprechen, loslassen → der Text wird lokal transkribiert und eingefügt.
3. Im Overlay den **Verarbeitungsmodus** (z. B. „Rechtschreibung & Grammatik“, „E-Mail verfassen“)
   und die **Tonalität** wählen. Modi außer „Nur transkribieren“ nutzen die OpenAI-Textoptimierung.

### Hotkeys (Standard, in den Einstellungen änderbar)

| Aktion | Taste |
| --- | --- |
| Push-to-Talk (halten) | F8 |
| Overlay ein-/ausblenden | F9 |
| Aufnahme starten/stoppen | F10 |

## OpenAI-Key einrichten

Für die optionale Textoptimierung in **Einstellungen → OpenAI-Textoptimierung** einen API-Key
eintragen. Der Key wird verschlüsselt über **Windows DPAPI** gespeichert — nie im
Installationsordner und nie im Klartext.

## Lokale Transkription & Modelle

Beim ersten Gebrauch wird das gewählte Whisper-Modell automatisch nach
`%LocalAppData%\LiveTranscribe\models\` heruntergeladen. Standard ist **Base Q5_0**
(guter Kompromiss aus Genauigkeit und CPU-Last). Größere Modelle = genauer, aber langsamer.

## Updates

Die App prüft beim Start auf Updates und installiert sie **vollautomatisch im Leerlauf**
(nie während Aufnahme/Transkription/Einfügen), danach startet sie neu — kein Klick nötig.
Updates kommen **ausschließlich aus fertigen GitHub-Release-Artefakten**. Schlägt ein Update
fehl, läuft die bisherige Version unverändert weiter. Manuell prüfen lässt sich jederzeit über
**Einstellungen → Updates**; den Auto-Check beim Start kann man dort auch abschalten.

## Speicherorte

| Was | Pfad |
| --- | --- |
| Einstellungen | `%AppData%\LiveTranscribe\settings.json` |
| Whisper-Modelle | `%LocalAppData%\LiveTranscribe\models\` |
| Logs | `%LocalAppData%\LiveTranscribe\logs\` |
| OpenAI-Key | Windows DPAPI (verschlüsselt, keine Datei im Programmordner) |

## Build aus dem Quellcode

```powershell
dotnet build LiveTranscribe.sln -c Release
dotnet run --project src/LiveTranscribe.App
```

Voraussetzung: .NET 8 SDK. Tests: `dotnet test`.

## Lizenz & Datenschutz

Audio verlässt den Rechner nie. Nur der bereits lokal transkribierte **Text** wird an OpenAI
gesendet — und nur, wenn ein Optimierungsmodus aktiv ist.
