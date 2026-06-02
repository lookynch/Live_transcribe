## Live Transcribe 1.3.0

Lokale Sprachdiktier-App mit OpenAI-Unterstützung.

**Neu in dieser Version**
- **Assistent-Modus (neuer Standard)**: Sprich eine Anweisung wie „Schreibe eine Mail an Herrn
  Büttner, dass die Teile später kommen" — OpenAI führt sie aus und gibt **nur das Ergebnis**
  (die fertige Mail) zurück, das direkt eingefügt/kopiert wird. Normaler Text wird verbessert.
  Die bisherigen Modi bleiben im Overlay wählbar.
- **OpenAI-Status sichtbar**: Das Overlay zeigt nach dem Diktat „mit OpenAI" bzw. den Grund,
  falls nicht überarbeitet wurde (z. B. „OpenAI-Kontingent erschöpft – Rohtext").

**Aus 1.2.x**
- Automatisches Einfügen ins fokussierte Textfeld (sonst Zwischenablage).
- Lesbare dunkle Dropdowns; abgehärtete Zwischenablage.
- Live-Vorschau während der Aufnahme, freie Hotkeys, modernes Overlay.

> Hinweis: Für die OpenAI-Überarbeitung wird ein gültiger OpenAI-Key **mit Guthaben** benötigt
> (Einstellungen → API-Key). Ohne Guthaben meldet OpenAI „Kontingent erschöpft" und es wird der
> Rohtext eingefügt.

> Hinweis: Beim ersten Start kann Windows SmartScreen eine Warnung anzeigen, da das
> Installationsprogramm (noch) nicht signiert ist. „Weitere Informationen" → „Trotzdem ausführen".
