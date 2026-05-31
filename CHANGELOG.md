# Changelog

Alle nennenswerten Änderungen werden hier dokumentiert.
Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
Versionierung nach [SemVer](https://semver.org/lang/de/).

## [Unveröffentlicht]

### Hinzugefügt
- Lokale Sprachtranskription mit Whisper (kein Audio-Upload).
- Schwebendes Overlay (fokus-neutral) + Tray-Icon.
- Globale Hotkeys: Push-to-Talk (F8), Overlay umschalten (F9), Start/Stop (F10).
- Einfügen an der Cursorposition mit Zwischenablage-Wiederherstellung und Tipp-Fallback.
- Optionale OpenAI-Textoptimierung: 11 Verarbeitungsmodi × 10 Tonalitäten.
- Sichere API-Key-Speicherung über Windows DPAPI.
- Einstellungen (Modell, Sprache, Verhalten, Updates, Deinstallationsoptionen).
- Auto-Update über GitHub Releases (Velopack), nur im Leerlauf, mit Rollback-Sicherheit.
- Saubere Deinstallation inkl. Autostart-, Temp- und optionaler Nutzerdaten-Bereinigung.
- GitHub-Actions-Release-Workflow (Tag `v*` → Build, Test, Pack, Upload).

[Unveröffentlicht]: ../../compare/HEAD
