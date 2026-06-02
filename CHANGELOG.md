# Changelog

Alle nennenswerten Änderungen werden hier dokumentiert.
Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
Versionierung nach [SemVer](https://semver.org/lang/de/).

## [Unveröffentlicht]

## [1.3.0] — 2026-06-01

### Hinzugefügt
- **Assistent-Modus (neuer Standard)**: Das Diktat wird interpretiert. Ist es eine Anweisung
  (z. B. „Schreibe eine Mail an Herrn Büttner, dass die Teile später kommen"), führt OpenAI sie
  aus und gibt **nur das fertige Ergebnis** (die Mail) zurück — genau das wird eingefügt/kopiert.
  Ist es normaler Text, wird er verbessert. Die bisherigen Modi bleiben wählbar.

### Geändert
- **OpenAI-Status sichtbar**: Nach jedem Diktat zeigt das Overlay, ob OpenAI tatsächlich
  überarbeitet hat („mit OpenAI") oder warum nicht („OpenAI-Kontingent erschöpft – Rohtext",
  „OpenAI-Key ungültig – Rohtext"). Vorher fiel die App bei OpenAI-Fehlern still auf den Rohtext
  zurück, ohne das anzuzeigen.

## [1.2.0] — 2026-06-01

### Hinzugefügt
- **Automatisches Einfügen wie am Handy**: Ist der Cursor in einem Textfeld, wird der erkannte
  Text direkt dort eingefügt; ist kein Feld aktiv, landet er in der Zwischenablage. Die
  Feld-Erkennung nutzt den System-Caret bzw. bekannte Eingabe-Controls.

### Geändert
- **Einfacher, ohne tiefe Einstellungen**: Das frühere „Nach Aufnahme"-Auswahlfeld entfällt — das
  Verhalten (Einfügen vs. Zwischenablage) entscheidet sich jetzt automatisch.
- **KI-Überarbeitung übernimmt Formatierung**: Der System-Prompt weist das Modell an, sinnvolle
  Absätze, Zeilenumbrüche und Aufzählungen zu erhalten bzw. zu erzeugen.

### Behoben
- **Unlesbare Dropdowns**: Die Modus-/Ton- und Einstellungs-ComboBoxen zeigten helle Schrift auf
  hellem Aufklapp-Hintergrund. Sie haben jetzt ein dunkles Popup mit gut lesbaren Einträgen.
- **Diktat bricht nicht mehr ab, wenn die Zwischenablage kurz gesperrt ist**
  (`CLIPBRD_E_CANT_OPEN`): Beim Einfügen wird auf Tippen ausgewichen, der Zwischenablage-Weg
  wird abgefangen statt die ganze Verarbeitung fehlschlagen zu lassen.

## [1.1.1] — 2026-06-01

### Behoben
- Startabsturz des Overlays („Eine Storyboard-Struktur in einem ‚Style' kann keinen ‚TargetName'
  angeben"): Die Waveform-/Glow-Animation wird jetzt aus dem Code-Behind gegen den Fenster-Namescope
  gestartet statt aus einem Style-Trigger.

## [1.1.0] — 2026-06-01

### Hinzugefügt
- **Live-Vorschau während der Aufnahme**: ein schnelles Tiny-Modell transkribiert ein
  gleitendes Audiofenster (~15 s) periodisch und zeigt den vorläufigen Text direkt im Overlay.
  Der finale Text nutzt weiterhin das gewählte Hauptmodell. In den Einstellungen abschaltbar.
- **Eigene Aufnahme-Tastenkürzel**: Push-to-Talk, Start/Stopp und Overlay-Umschalten lassen sich
  im Einstellungsfenster frei belegen (Kombination drücken, Esc bricht ab).
- **Modell-Vorwärmen beim Start**: das gewählte Whisper-Modell wird beim Programmstart geladen
  (mit sichtbarem Fortschritt), sodass die erste Transkription sofort schnell ist.

### Geändert
- **Kompakteres, moderneres Overlay**: schlanke Pille mit animierter Waveform und Akzent-Glow
  bei Aufnahme; Modus-/Ton-Optionen klappen nur bei Bedarf aus.
- **Modernisiertes Einstellungsfenster** mit Karten-Layout und einheitlichen Controls; öffnet
  jetzt zuverlässig im Vordergrund (auch über dem Always-on-top-Overlay).

### Behoben
- **Einfügen zuverlässiger**: Fokus auf das Zielfenster wird vor dem Einfügen verifiziert
  (mit kurzem Retry); bei fehlgeschlagenem Fokus automatischer Tipp-Fallback statt stiller Fehler.
- Klare Rückmeldung bei fehlendem Mikrofon und leerer Aufnahme statt wirkungslosem Verhalten.

## [1.0.0]

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

[Unveröffentlicht]: ../../compare/v1.3.0...HEAD
[1.3.0]: ../../compare/v1.2.0...v1.3.0
[1.2.0]: ../../compare/v1.1.1...v1.2.0
[1.1.1]: ../../compare/v1.1.0...v1.1.1
[1.1.0]: ../../compare/v1.0.0...v1.1.0
[1.0.0]: ../../releases/tag/v1.0.0
