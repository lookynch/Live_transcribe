# Deinstallation

## So deinstallierst du

**Windows-Einstellungen → Apps → Installierte Apps → „Live Transcribe“ → Deinstallieren**
(oder Systemsteuerung → Programme).

## Was passiert dabei

Velopack entfernt automatisch:

- alle Programmdateien (`%LocalAppData%\LiveTranscribe\current\` und Updater/Stub),
- den Startmenü-Eintrag und ggf. die Desktop-Verknüpfung,
- den Eintrag unter „Apps & Features“.

Zusätzlich räumt die App im Deinstallations-Callback auf:

- **Autostart-Eintrag** (HKCU Run) wird gelöscht,
- noch laufende `LiveTranscribe`-Prozesse (Tray) werden beendet,
- temporäre Audiodateien werden gelöscht.

## Optionale Daten (vorab in den Einstellungen wählbar)

Da während der Deinstallation kein Dialog möglich ist, wird die Auswahl **vorab** unter
**Einstellungen → Bei Deinstallation** getroffen. Standard: **Nutzerdaten behalten**.

| Option | Wirkung bei aktivierter Checkbox |
| --- | --- |
| Einstellungen löschen | `%AppData%\LiveTranscribe\` wird entfernt |
| Modelle löschen | `%LocalAppData%\LiveTranscribe\models\` wird entfernt |
| API-Key löschen | Der per DPAPI gespeicherte OpenAI-Key wird entfernt (Standard: an) |

## Manuelles Aufräumen (falls gewünscht)

Falls du nach der Deinstallation Reste behalten hast und sie doch entfernen möchtest:

```powershell
Remove-Item "$env:AppData\LiveTranscribe" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LocalAppData\LiveTranscribe" -Recurse -Force -ErrorAction SilentlyContinue
```

Der OpenAI-Key ist über DPAPI an dein Windows-Benutzerkonto gebunden; ihn entfernst du am
einfachsten vor der Deinstallation über **Einstellungen → API-Key → Löschen**.
