## Live Transcribe 1.2.0

Lokale Sprachdiktier-App mit optionaler OpenAI-Textoptimierung.

**Neu in dieser Version**
- **Automatisches Einfügen wie am Handy**: Cursor in einem Textfeld → Text wird direkt dort
  eingefügt; kein Feld aktiv → Text landet in der Zwischenablage. Kein „Nach Aufnahme"-Schalter mehr.
- **KI-Überarbeitung übernimmt Formatierung** (Absätze, Zeilenumbrüche, Aufzählungen).

**Behoben**
- Unlesbare Dropdowns (helle Schrift auf hellem Aufklapp-Hintergrund) — jetzt dunkles, lesbares Popup.
- Diktat bricht nicht mehr ab, wenn die Zwischenablage kurzzeitig gesperrt ist
  (Tipp-Fallback statt Fehler).

**Aus 1.1.x**
- Live-Vorschau des gesprochenen Texts während der Aufnahme (schnelles Tiny-Modell).
- Frei belegbare Aufnahme-Tastenkürzel, kompaktes modernes Overlay, Modell-Vorwärmen beim Start.

Die vollständigen Änderungen dieser Version stehen in der [CHANGELOG.md](../CHANGELOG.md).

> Hinweis: Beim ersten Start kann Windows SmartScreen eine Warnung anzeigen,
> da das Installationsprogramm (noch) nicht signiert ist.
> „Weitere Informationen" → „Trotzdem ausführen".
