# Solution Operation Monitor (XrmToolBox Tool)

Zeigt live den Status laufender Solution-Vorgänge (Import / Upgrade / Uninstall) in einer Dataverse-Umgebung – inkl. Fortschritt in %, verstrichener Zeit und geschätzter Restzeit – sowie die komplette Solution History auf einen Blick.

## Features (v1.1.0)

- **Light & Dark Mode**: Umschalter „Design“ in der Toolbar (Hell / Dunkel). Beim Start wird automatisch der Windows-App-Modus erkannt (`AppsUseLightTheme`), fällt bei fehlendem Registry-Zugriff auf Hell zurück. Alle Bereiche sind durchgängig eingefärbt: Toolbar/Menüs (eigener `ToolStripRenderer`), Karten, Fortschritts-Diagramm, History-Grid inkl. Kopf-, Auswahl- und Statusfarben (aktiv/Fehler). Der Wechsel ist ohne aktive Verbindung möglich und wirkt sofort auf bereits geladene Daten.

## Features (v1.2)

- **Zeitverlaufs-Diagramm pro aktivem Vorgang**: unter dem Fortschrittsbalken zeigt ein Mini-Chart
  - die **Ist-Kurve** der echten Fortschritts-Messpunkte (blau),
  - die **Prognose-Linie** bis 100 % auf Basis der aktuellen Rate (grün gestrichelt, mit ETA-Uhrzeit),
  - die **Ø-Referenzlinie** aus der Historie (grau gepunktet) - läuft die Ist-Kurve flacher als die Referenz, ist der Vorgang langsamer als üblich,
  - einen **"jetzt"-Marker** (orange).
  Bei Uninstall/Upgrade ohne Plattform-Prozentwert wird der geschätzte Verlauf gestrichelt dargestellt.

## Features (v1.1)

- **Zweisprachig (DE/EN)**: Sprache wird automatisch aus Windows erkannt, Umschalter in der Toolbar
- **Flackerfreie Live-Karten**: Karten werden pro Vorgang wiederverwendet und nur die Werte aktualisiert (kein Neuaufbau pro Refresh), Double-Buffering aktiv, Grid behält Scroll-Position und Auswahl
- **Realistische ETA**: Restzeit basiert auf der gleitenden Fortschrittsrate der letzten 2 Minuten (nicht auf linearer Hochrechnung vom Start) + exponentielle Glättung gegen springende Werte. Wenn der Plattform-Fortschritt hängt (typisch: Import springt schnell auf ~90 % und verharrt dort), zeigt das Tool ehrlich "Fortschritt stockt" statt einer steigenden Restzeit
- **Kein Fake-Prozentwert**: Ohne Vergleichsdaten in der Historie läuft der Balken im Marquee-Modus statt bei ~90 % festzuhängen

## Features (Basis)

- **Aktive Vorgänge live** (Auto-Refresh 5/10/30/60 s):
  - Solution-Name, Version, Vorgang (Import/Upgrade/Uninstall) und Untervorgang
  - Fortschrittsbalken + Prozentanzeige
  - "Läuft seit" + geschätzte Restzeit (ETA)
- **Solution History** (letzte 200 Vorgänge) als sortier- und filterbares Grid:
  - Vorgang, Untervorgang, Status, Ergebnis, Start, Ende, Dauer, Fehlermeldung
  - Laufende Vorgänge gelb, fehlgeschlagene rot markiert
- **Benachrichtigung**, sobald ein laufender Vorgang abgeschlossen ist (inkl. Ergebnis)

## Wie der Fortschritt ermittelt wird

| Vorgang | Quelle | Genauigkeit |
|---|---|---|
| **Import** | `importjob.progress` (0–100 %, echter Plattform-Wert) | Exakt, ETA per linearer Extrapolation aus verstrichener Zeit |
| **Upgrade / Uninstall** | Dataverse liefert hier **keinen Prozentwert**. Das Tool nimmt die Durchschnittsdauer der letzten (max. 5) gleichartigen Vorgänge derselben Solution aus `msdyn_solutionhistory` | Schätzung (orange markiert) |
| **Historie** | `msdyn_solutionhistory` (Start-/Endzeit, Operation, Suboperation, Status, Ergebnis, Exception) | Exakt |

Wichtig zu wissen:
- `msdyn_solutionhistory`-Einträge werden von der Plattform **nach 180 Tagen automatisch gelöscht** – älter reicht die Historie also nie zurück.
- Optionset-Beschriftungen (Operation, Status, Ergebnis) werden **dynamisch über `FormattedValues`** aufgelöst statt hart codiert – dadurch keine Probleme mit abweichenden Werten je Version/Sprache.
- Es werden ausschließlich **late-bound Entities** verwendet (Early Bound ist laut XrmToolBox-Doku in Tools verboten, weil es Konflikte mit anderen Tools erzeugt).

## Projekt bauen

Voraussetzungen: Visual Studio 2019/2022, .NET Framework 4.8 Developer Pack.

Hinweis: Die Quelldateien sind UTF-8 (Umlaute!). Visual Studio kommt damit automatisch klar; bei manuellem Kompilieren `-codepage:utf8` verwenden.

```
git/Ordner öffnen -> SolutionOperationMonitor.csproj in Visual Studio öffnen
NuGet-Restore läuft automatisch (Paket: XrmToolBoxPackage)
Build (Debug oder Release)
```

Das Projekt ist eine SDK-Style-csproj mit `net48` + `UseWindowsForms` – kein Designer-Gedöns, die komplette UI wird in `src/MonitorControl.cs` im Code aufgebaut. Der Quellcode liegt in `src/`, die Icons in `assets/` (SDK-Projekte globben `.cs`-Dateien automatisch rekursiv, keine weitere Konfiguration nötig).

## Deployment / Testen

**Variante A – automatisch (Debug-Build):**
Im csproj ist ein `AfterTargets="Build"`-Target enthalten, das die DLL bei jedem Debug-Build automatisch nach
`%APPDATA%\MscrmTools\XrmToolBox\Plugins` kopiert. Danach XrmToolBox (neu) starten.

**Variante B – manuell:**
`bin\Debug\SolutionOperationMonitor.dll` selbst in den Plugins-Ordner kopieren.

**Debuggen:** In den Projekteigenschaften unter *Debug* als externes Programm `XrmToolBox.exe` eintragen, dann F5. (Siehe auch: https://www.xrmtoolbox.com/documentation/for-developers/debug/)

## Benötigte Berechtigungen in Dataverse

Der verbundene Benutzer braucht Leserechte auf:
- `msdyn_solutionhistory` (Solution History)
- `importjob` (Import Job) – optional; ohne diese Rechte funktioniert alles außer der exakten Import-Prozentanzeige

## XrmToolBox Tool Library – Validierungs-Checkliste

Der Stand gegenüber der offiziellen Checkliste (https://www.xrmtoolbox.com/documentation/for-users/tool-library/):

**NuGet-Paket**

| Punkt | Status | Umsetzung |
|---|---|---|
| Icon-URL | ✅ | `PackageIcon` (eingebettet) + `PackageIconUrl` in `SolutionOperationMonitor.csproj` |
| Project-URL | ✅ | `PackageProjectUrl` in der `.csproj` |
| Tool-DLL im Ordner `Plugins` | ✅ | `IncludeBuildOutput=false` + `None … PackagePath="Plugins"` – die DLL landet im Paket unter `Plugins/` |
| Paket-Version == Tool-Version | ✅ | `Version=1.1.0` (.csproj) == `AssemblyVersion 1.1.0.0` (`src/AssemblyInfo.cs`) |

> Hinweis: Der Paket-Tag **`XrmToolBox`** ist gesetzt – er ist Voraussetzung, damit die Tool Library das Paket überhaupt findet. Paket erzeugen mit `dotnet pack -c Release` (oder in Visual Studio „Pack“).

**Tool**

| Punkt | Status | Umsetzung |
|---|---|---|
| Bild für „Large“-Anzeige | ✅ | `BigImageBase64` (80×80) in `SolutionOperationMonitorPlugin.cs` |
| Bild für „Small“-Anzeige | ✅ | `SmallImageBase64` (32×32) in `SolutionOperationMonitorPlugin.cs` |
| Controls passen sich der Fenstergröße an | ✅ | `Dock`/`Anchor`, `SplitContainer`, `FlowLayoutPanel` + `Resize`-Handler skalieren Karten und Diagramm |
| Tool ohne Verbindung öffenbar | ✅ | `PluginControlBase`; `OnLoad` lädt nur bei `Service != null` |
| Controls ohne Verbindung nutzbar | ✅ | Design-/Sprach-/Filter-/Intervall-Controls arbeiten rein lokal, Auto-Refresh ist per `Service != null` abgesichert |
| Verbindungsdialog bei verbindungspflichtigen Aktionen | ✅ | „Aktualisieren“ läuft über `ExecuteMethod(...)`, das bei fehlender Verbindung den Auswahldialog öffnet |
| Controls mit Verbindung fehlerfrei | ✅ | Abfragen mit try/catch (z. B. `importjob` ohne Leserechte) |
| Lang laufende Vorgänge asynchron | ✅ | `WorkAsync(...)` bzw. `Task.Run(...)` – die Haupt-App friert nicht ein |

## Mögliche Erweiterungen

- Windows-Toast-Notification bei Abschluss (XrmToolBox unterstützt das: `Use Windows Toast Notification` in der Doku)
- Parsing der `importjob.data`-XML für Fortschritt **pro Komponente**
- „System“-Option für automatisches Umschalten des Designs bei Änderung des Windows-Modus zur Laufzeit
