using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Win32;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;
using Label = System.Windows.Forms.Label;

namespace SolutionOperationMonitor
{
    #region Lokalisierung

    internal static class I18n
    {
        public static bool German = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("de", StringComparison.OrdinalIgnoreCase);

        // key -> [Deutsch, English]
        private static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>
        {
            { "Close",            new[] { "Schließen", "Close" } },
            { "Refresh",          new[] { "Aktualisieren", "Refresh" } },
            { "AutoOn",           new[] { "Auto-Refresh: AN", "Auto refresh: ON" } },
            { "AutoOff",          new[] { "Auto-Refresh: AUS", "Auto refresh: OFF" } },
            { "Interval",         new[] { "Intervall:", "Interval:" } },
            { "Filter",           new[] { "Filter:", "Filter:" } },
            { "Language",         new[] { "Sprache", "Language" } },
            { "Theme",            new[] { "Design", "Theme" } },
            { "ThemeLight",       new[] { "Hell", "Light" } },
            { "ThemeDark",        new[] { "Dunkel", "Dark" } },
            { "LastRefresh",      new[] { "Zuletzt aktualisiert: {0}", "Last refreshed: {0}" } },
            { "ActiveHeader",     new[] { "Aktive Solution-Vorgänge", "Active solution operations" } },
            { "HistoryHeader",    new[] { "Solution History (letzte 200 Vorgänge, max. 180 Tage rückwirkend)", "Solution history (last 200 operations, max. 180 days back)" } },
            { "NoActive",         new[] { "Zurzeit läuft kein Import / Upgrade / Uninstall.", "No import / upgrade / uninstall is currently running." } },
            { "Loading",          new[] { "Lade Solution History und aktive Vorgänge...", "Loading solution history and active operations..." } },
            { "LoadError",        new[] { "Fehler beim Laden: {0}", "Error while loading: {0}" } },
            { "AutoFailed",       new[] { "Auto-Refresh fehlgeschlagen: {0}", "Auto refresh failed: {0}" } },
            { "RunningSince",     new[] { "Läuft seit: {0}", "Running for: {0}" } },
            { "StartAt",          new[] { "Start: {0}", "Started: {0}" } },
            { "TimeRange",        new[] { "Start {0} → Ziel ca. {1}", "Started {0} → target ~{1}" } },
            { "CompProcessed",    new[] { "Komponenten: {0} verarbeitet", "Components: {0} processed" } },
            { "CompOk",           new[] { "{0} ok", "{0} ok" } },
            { "CompWarn",         new[] { "{0} Warnung(en)", "{0} warning(s)" } },
            { "CompErr",          new[] { "{0} Fehler", "{0} error(s)" } },
            { "RemainingApprox",  new[] { "Restzeit ca. {0}", "Approx. {0} remaining" } },
            { "EstFromHistory",   new[] { "Restzeit ca. {0} (geschätzt aus Historie, Ø {1})", "Approx. {0} remaining (estimated from history, Ø {1})" } },
            { "LongerThanUsual",  new[] { "Dauert länger als üblich (Ø {0})", "Taking longer than usual (Ø {0})" } },
            { "NoEstimate",       new[] { "Keine Schätzung möglich (keine vergleichbaren Vorgänge in der Historie)", "No estimate available (no comparable operations in history)" } },
            { "Stalled",          new[] { "Fortschritt stockt bei {0} % – Restzeit aktuell nicht kalkulierbar", "Progress is stalled at {0}% – remaining time currently not calculable" } },
            { "Measuring",        new[] { "Ermittle Fortschrittsrate...", "Measuring progress rate..." } },
            { "Completed",        new[] { "Solution-Vorgang abgeschlossen: {0}", "Solution operation completed: {0}" } },
            { "ResultSuffix",     new[] { " – Ergebnis: {0}", " – result: {0}" } },
            { "Running",          new[] { "läuft ({0})", "running ({0})" } },
            { "Unknown",          new[] { "(unbekannt)", "(unknown)" } },
            { "ColSolution",      new[] { "Solution", "Solution" } },
            { "ColVersion",       new[] { "Version", "Version" } },
            { "ColPublisher",     new[] { "Publisher", "Publisher" } },
            { "ColOperation",     new[] { "Vorgang", "Operation" } },
            { "ColSubOperation",  new[] { "Untervorgang", "Sub operation" } },
            { "ColStatus",        new[] { "Status", "Status" } },
            { "ColResult",        new[] { "Ergebnis", "Result" } },
            { "ColStart",         new[] { "Start", "Start" } },
            { "ColEnd",           new[] { "Ende", "End" } },
            { "ColDuration",      new[] { "Dauer", "Duration" } },
            { "ColError",         new[] { "Fehlermeldung", "Error message" } },
            { "ChartNow",         new[] { "jetzt", "now" } },
            { "ChartEta",         new[] { "ETA ~{0}", "ETA ~{0}" } },
            { "ChartActual",      new[] { "Ist", "Actual" } },
            { "ChartProjection",  new[] { "Prognose", "Projection" } },
            { "ChartAvg",         new[] { "Ø-Verlauf (Historie)", "Ø trend (history)" } },
            { "ChartEstimate",    new[] { "Schätzung (Historie)", "Estimate (history)" } },
            { "ChartNoData",      new[] { "Noch keine Daten für das Diagramm", "No chart data yet" } }
        };

        public static string T(string key)
        {
            string[] v;
            return S.TryGetValue(key, out v) ? (German ? v[0] : v[1]) : key;
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }
    }

    #endregion

    #region Theme (Light / Dark)

    internal enum ThemeMode { Light, Dark }

    /// <summary>Farbpalette für einen Modus. Alle Farben des Tools werden hierüber bezogen.</summary>
    internal sealed class ThemePalette
    {
        public Color AppBackground;
        public Color PanelBackground;
        public Color HeaderText;

        public Color CardBackground;
        public Color PrimaryText;
        public Color SecondaryText;
        public Color MutedText;

        public Color ToolStripBackground;
        public Color ToolStripText;
        public Color ToolStripHover;
        public Color ToolStripBorder;

        public Color GridBackground;
        public Color GridCellBackground;
        public Color GridCellText;
        public Color GridHeaderBackground;
        public Color GridHeaderText;
        public Color GridLines;
        public Color GridSelectionBackground;
        public Color GridSelectionText;
        public Color RowActive;
        public Color RowError;

        public Color ChartBackground;
        public Color ChartAxis;
        public Color ChartGrid;
        public Color ChartLabel;
        public Color ChartActual;
        public Color ChartProjection;
        public Color ChartAvg;
        public Color ChartNow;
        public Color ChartNowText;
        public Color ChartEtaText;

        public Color EtaGood;
        public Color EtaWarn;
        public Color EtaNeutral;
    }

    /// <summary>Zentraler, statischer Theme-Zustand. Die Controls lesen <see cref="Current"/> beim (Neu-)Zeichnen.</summary>
    internal static class Theme
    {
        public static ThemeMode Mode { get; private set; }
        public static ThemePalette Current { get; private set; }

        private static readonly ThemePalette LightPalette = new ThemePalette
        {
            AppBackground = Color.FromArgb(240, 244, 255),
            PanelBackground = Color.FromArgb(240, 244, 255),
            HeaderText = Color.FromArgb(27, 42, 74),

            CardBackground = Color.FromArgb(248, 250, 255),
            PrimaryText = Color.FromArgb(27, 42, 74),
            SecondaryText = Color.FromArgb(90, 107, 140),
            MutedText = Color.Gray,

            ToolStripBackground = Color.FromArgb(248, 250, 255),
            ToolStripText = Color.FromArgb(27, 42, 74),
            ToolStripHover = Color.FromArgb(210, 222, 248),
            ToolStripBorder = Color.FromArgb(210, 218, 235),

            GridBackground = Color.FromArgb(250, 251, 255),
            GridCellBackground = Color.White,
            GridCellText = Color.FromArgb(27, 42, 74),
            GridHeaderBackground = Color.FromArgb(230, 236, 250),
            GridHeaderText = Color.FromArgb(27, 42, 74),
            GridLines = Color.FromArgb(225, 230, 240),
            GridSelectionBackground = Color.FromArgb(63, 106, 216),
            GridSelectionText = Color.White,
            RowActive = Color.FromArgb(255, 250, 220),
            RowError = Color.FromArgb(255, 230, 230),

            ChartBackground = Color.White,
            ChartAxis = Color.FromArgb(200, 205, 215),
            ChartGrid = Color.FromArgb(235, 238, 244),
            ChartLabel = Color.FromArgb(110, 118, 135),
            ChartActual = Color.FromArgb(63, 106, 216),
            ChartProjection = Color.FromArgb(34, 150, 83),
            ChartAvg = Color.FromArgb(165, 172, 188),
            ChartNow = Color.FromArgb(235, 140, 30),
            ChartNowText = Color.FromArgb(200, 110, 10),
            ChartEtaText = Color.FromArgb(25, 120, 65),

            EtaGood = Color.DarkGreen,
            EtaWarn = Color.DarkOrange,
            EtaNeutral = Color.Gray
        };

        private static readonly ThemePalette DarkPalette = new ThemePalette
        {
            AppBackground = Color.FromArgb(30, 32, 38),
            PanelBackground = Color.FromArgb(30, 32, 38),
            HeaderText = Color.FromArgb(225, 231, 245),

            CardBackground = Color.FromArgb(42, 45, 53),
            PrimaryText = Color.FromArgb(226, 232, 245),
            SecondaryText = Color.FromArgb(160, 170, 190),
            MutedText = Color.FromArgb(140, 148, 165),

            ToolStripBackground = Color.FromArgb(37, 40, 47),
            ToolStripText = Color.FromArgb(226, 232, 245),
            ToolStripHover = Color.FromArgb(58, 63, 74),
            ToolStripBorder = Color.FromArgb(64, 68, 78),

            GridBackground = Color.FromArgb(37, 40, 47),
            GridCellBackground = Color.FromArgb(42, 45, 53),
            GridCellText = Color.FromArgb(226, 232, 245),
            GridHeaderBackground = Color.FromArgb(50, 54, 63),
            GridHeaderText = Color.FromArgb(226, 232, 245),
            GridLines = Color.FromArgb(60, 64, 74),
            GridSelectionBackground = Color.FromArgb(56, 92, 170),
            GridSelectionText = Color.White,
            RowActive = Color.FromArgb(74, 66, 30),
            RowError = Color.FromArgb(86, 46, 46),

            ChartBackground = Color.FromArgb(42, 45, 53),
            ChartAxis = Color.FromArgb(96, 102, 116),
            ChartGrid = Color.FromArgb(58, 62, 72),
            ChartLabel = Color.FromArgb(168, 176, 194),
            ChartActual = Color.FromArgb(96, 148, 255),
            ChartProjection = Color.FromArgb(72, 200, 120),
            ChartAvg = Color.FromArgb(120, 128, 148),
            ChartNow = Color.FromArgb(240, 160, 60),
            ChartNowText = Color.FromArgb(240, 170, 80),
            ChartEtaText = Color.FromArgb(96, 210, 140),

            EtaGood = Color.FromArgb(96, 210, 140),
            EtaWarn = Color.FromArgb(240, 176, 80),
            EtaNeutral = Color.FromArgb(160, 170, 190)
        };

        static Theme()
        {
            Mode = ThemeMode.Light;
            Current = LightPalette;
        }

        public static void Set(ThemeMode mode)
        {
            Mode = mode;
            Current = mode == ThemeMode.Dark ? DarkPalette : LightPalette;
        }

        /// <summary>Windows-App-Modus auslesen (0 = Dark). Fällt bei fehlendem Registry-Zugriff auf Light zurück.</summary>
        public static ThemeMode DetectSystem()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    if (value is int i && i == 0) return ThemeMode.Dark;
                }
            }
            catch { /* Registry nicht lesbar -> Light */ }
            return ThemeMode.Light;
        }
    }

    /// <summary>ProfessionalColorTable, die Toolbar/Menü-Farben aus der aktiven Palette bezieht.</summary>
    internal sealed class ThemeColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette _p;

        public ThemeColorTable(ThemePalette p)
        {
            _p = p;
            UseSystemColors = false;
        }

        public override Color ToolStripGradientBegin => _p.ToolStripBackground;
        public override Color ToolStripGradientMiddle => _p.ToolStripBackground;
        public override Color ToolStripGradientEnd => _p.ToolStripBackground;
        public override Color ToolStripContentPanelGradientBegin => _p.ToolStripBackground;
        public override Color ToolStripContentPanelGradientEnd => _p.ToolStripBackground;
        public override Color ToolStripPanelGradientBegin => _p.ToolStripBackground;
        public override Color ToolStripPanelGradientEnd => _p.ToolStripBackground;
        public override Color ToolStripBorder => _p.ToolStripBorder;
        public override Color MenuStripGradientBegin => _p.ToolStripBackground;
        public override Color MenuStripGradientEnd => _p.ToolStripBackground;

        public override Color ImageMarginGradientBegin => _p.ToolStripBackground;
        public override Color ImageMarginGradientMiddle => _p.ToolStripBackground;
        public override Color ImageMarginGradientEnd => _p.ToolStripBackground;

        public override Color ButtonSelectedGradientBegin => _p.ToolStripHover;
        public override Color ButtonSelectedGradientMiddle => _p.ToolStripHover;
        public override Color ButtonSelectedGradientEnd => _p.ToolStripHover;
        public override Color ButtonSelectedHighlight => _p.ToolStripHover;
        public override Color ButtonSelectedBorder => _p.ToolStripBorder;
        public override Color ButtonPressedGradientBegin => _p.ToolStripHover;
        public override Color ButtonPressedGradientMiddle => _p.ToolStripHover;
        public override Color ButtonPressedGradientEnd => _p.ToolStripHover;
        public override Color ButtonCheckedGradientBegin => _p.ToolStripHover;
        public override Color ButtonCheckedGradientMiddle => _p.ToolStripHover;
        public override Color ButtonCheckedGradientEnd => _p.ToolStripHover;
        public override Color ButtonCheckedHighlight => _p.ToolStripHover;

        public override Color MenuItemSelected => _p.ToolStripHover;
        public override Color MenuItemSelectedGradientBegin => _p.ToolStripHover;
        public override Color MenuItemSelectedGradientEnd => _p.ToolStripHover;
        public override Color MenuItemPressedGradientBegin => _p.ToolStripBackground;
        public override Color MenuItemPressedGradientMiddle => _p.ToolStripBackground;
        public override Color MenuItemPressedGradientEnd => _p.ToolStripBackground;
        public override Color MenuItemBorder => _p.ToolStripBorder;
        public override Color MenuBorder => _p.ToolStripBorder;
        public override Color ToolStripDropDownBackground => _p.ToolStripBackground;

        public override Color SeparatorDark => _p.ToolStripBorder;
        public override Color SeparatorLight => _p.ToolStripBorder;
        public override Color GripDark => _p.ToolStripBorder;
        public override Color GripLight => _p.ToolStripBorder;
    }

    /// <summary>Renderer, der zusätzlich Text- und Pfeilfarbe der Toolbar an die Palette anpasst.</summary>
    internal sealed class ThemedToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemePalette _p;

        public ThemedToolStripRenderer(ThemePalette p) : base(new ThemeColorTable(p))
        {
            _p = p;
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = _p.ToolStripText;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = _p.ToolStripText;
            base.OnRenderArrow(e);
        }
    }

    #endregion

    #region Zeitverlaufs-Diagramm (Ist vs. Prognose vs. Historie)

    internal class ChartModel
    {
        /// <summary>Messpunkte: X = Sekunden seit Start des Vorgangs, Y = Fortschritt in %.</summary>
        public List<PointF> Samples = new List<PointF>();
        public double ElapsedSeconds;
        public double? RemainingSeconds;
        public double? AvgSeconds;
        public bool HasRealProgress;
        public double? CurrentPercent;

        public bool HasAnything
        {
            get { return Samples.Count > 0 || AvgSeconds.HasValue; }
        }
    }

    internal class ProgressChart : Panel
    {
        private ChartModel _model;

        public ProgressChart()
        {
            DoubleBuffered = true;
            BackColor = Theme.Current.ChartBackground;
            BorderStyle = BorderStyle.None;
            Resize += (s, e) => Invalidate();
        }

        public void SetModel(ChartModel model)
        {
            _model = model;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var p = Theme.Current;
            var m = _model;
            var plot = new Rectangle(36, 6, Width - 44, Height - 24);
            if (plot.Width < 60 || plot.Height < 30) return;

            using (var axisPen = new Pen(p.ChartAxis))
            using (var gridPen = new Pen(p.ChartGrid))
            using (var labelBrush = new SolidBrush(p.ChartLabel))
            using (var labelFont = new Font(Font.FontFamily, 7f))
            {
                // Y-Grid: 0 / 25 / 50 / 75 / 100 %
                for (int pc = 0; pc <= 100; pc += 25)
                {
                    var y = plot.Bottom - (float)(pc / 100.0 * plot.Height);
                    g.DrawLine(pc == 0 ? axisPen : gridPen, plot.Left, y, plot.Right, y);
                    if (pc % 50 == 0)
                    {
                        g.DrawString(pc + "%", labelFont, labelBrush, 2, y - 6);
                    }
                }
                g.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);

                if (m == null || !m.HasAnything)
                {
                    g.DrawString(I18n.T("ChartNoData"), labelFont, labelBrush,
                        plot.Left + 8, plot.Top + plot.Height / 2f - 6);
                    return;
                }

                // Zeitachse dimensionieren
                double totalSec = Math.Max(m.ElapsedSeconds * 1.15, 60);
                if (m.RemainingSeconds.HasValue)
                    totalSec = Math.Max(totalSec, (m.ElapsedSeconds + m.RemainingSeconds.Value) * 1.06);
                if (m.AvgSeconds.HasValue)
                    totalSec = Math.Max(totalSec, m.AvgSeconds.Value * 1.06);

                Func<double, float> X = s => plot.Left + (float)(Math.Max(0, Math.Min(s, totalSec)) / totalSec * plot.Width);
                Func<double, float> Y = p => plot.Bottom - (float)(Math.Max(0, Math.Min(p, 100)) / 100.0 * plot.Height);

                // Ø-Referenzlinie aus der Historie (0 % -> 100 % über die Durchschnittsdauer)
                if (m.AvgSeconds.HasValue && m.AvgSeconds.Value > 0)
                {
                    using (var avgPen = new Pen(p.ChartAvg, 1.4f) { DashStyle = DashStyle.Dot })
                    {
                        g.DrawLine(avgPen, X(0), Y(0), X(m.AvgSeconds.Value), Y(100));
                    }
                }

                // Ist-Kurve (echte Messpunkte) bzw. Schätzlinie ohne Plattform-Prozentwert
                double lastX = 0, lastY = 0;
                bool haveLast = false;

                if (m.Samples.Count > 0)
                {
                    var pts = m.Samples
                        .Select(s => new PointF(X(s.X), Y(s.Y)))
                        .ToArray();

                    using (var actualPen = new Pen(p.ChartActual, 2.2f)
                        { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    {
                        if (pts.Length == 1)
                        {
                            using (var dot = new SolidBrush(p.ChartActual))
                            {
                                g.FillEllipse(dot, pts[0].X - 2.5f, pts[0].Y - 2.5f, 5, 5);
                            }
                        }
                        else
                        {
                            g.DrawLines(actualPen, pts);
                        }
                    }

                    lastX = m.Samples[m.Samples.Count - 1].X;
                    lastY = m.Samples[m.Samples.Count - 1].Y;
                    haveLast = true;
                }
                else if (m.AvgSeconds.HasValue && m.CurrentPercent.HasValue)
                {
                    // Uninstall/Upgrade ohne Prozentwert: geschätzter Verlauf bis "jetzt"
                    using (var estPen = new Pen(p.ChartActual, 2.2f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(estPen, X(0), Y(0), X(m.ElapsedSeconds), Y(m.CurrentPercent.Value));
                    }
                    lastX = m.ElapsedSeconds;
                    lastY = m.CurrentPercent.Value;
                    haveLast = true;
                }

                // Prognose-Linie vom letzten Punkt bis 100 %
                if (haveLast && m.RemainingSeconds.HasValue && lastY < 100)
                {
                    using (var projPen = new Pen(p.ChartProjection, 2f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(projPen, X(lastX), Y(lastY), X(m.ElapsedSeconds + m.RemainingSeconds.Value), Y(100));
                    }
                }

                // "jetzt"-Marker
                using (var nowPen = new Pen(p.ChartNow, 1.6f))
                using (var nowBrush = new SolidBrush(p.ChartNowText))
                {
                    var nx = X(m.ElapsedSeconds);
                    g.DrawLine(nowPen, nx, plot.Top, nx, plot.Bottom);
                    g.DrawString(I18n.T("ChartNow"), labelFont, nowBrush,
                        Math.Min(nx + 2, plot.Right - 30), plot.Top);
                }

                // ETA-Marker mit Uhrzeit
                if (m.RemainingSeconds.HasValue)
                {
                    var etaSec = m.ElapsedSeconds + m.RemainingSeconds.Value;
                    var ex = X(etaSec);
                    using (var etaPen = new Pen(p.ChartProjection, 1.6f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(etaPen, ex, plot.Top, ex, plot.Bottom);
                    }
                    var etaClock = DateTime.Now.AddSeconds(m.RemainingSeconds.Value).ToString("HH:mm");
                    var etaLabel = I18n.F("ChartEta", etaClock);
                    var size = g.MeasureString(etaLabel, labelFont);
                    var lx = Math.Max(plot.Left, Math.Min(ex - size.Width / 2, plot.Right - size.Width));
                    using (var etaBrush = new SolidBrush(p.ChartEtaText))
                    {
                        g.DrawString(etaLabel, labelFont, etaBrush, lx, plot.Bottom + 3);
                    }
                }

                // Mini-Legende oben links im Plot
                float lyy = plot.Top + 2;
                float lxx = plot.Left + 6;
                using (var actualPen = new Pen(p.ChartActual, 2f))
                using (var projPen = new Pen(p.ChartProjection, 2f) { DashStyle = DashStyle.Dash })
                using (var avgPen = new Pen(p.ChartAvg, 1.4f) { DashStyle = DashStyle.Dot })
                {
                    var actualText = m.HasRealProgress ? I18n.T("ChartActual") : I18n.T("ChartEstimate");
                    lxx = DrawLegendItem(g, actualPen, actualText, labelFont, labelBrush, lxx, lyy);
                    if (m.RemainingSeconds.HasValue)
                        lxx = DrawLegendItem(g, projPen, I18n.T("ChartProjection"), labelFont, labelBrush, lxx, lyy);
                    if (m.AvgSeconds.HasValue)
                        DrawLegendItem(g, avgPen, I18n.T("ChartAvg"), labelFont, labelBrush, lxx, lyy);
                }
            }
        }

        private static float DrawLegendItem(Graphics g, Pen pen, string text, Font font, Brush brush, float x, float y)
        {
            g.DrawLine(pen, x, y + 6, x + 14, y + 6);
            g.DrawString(text, font, brush, x + 16, y);
            return x + 16 + g.MeasureString(text, font).Width + 10;
        }
    }

    #endregion

    #region Wiederverwendbare Karte für einen aktiven Vorgang (kein Neuaufbau = kein Flackern)

    internal class OperationCard : Panel
    {
        public Guid OperationId { get; private set; }

        private readonly Label _title;
        private readonly ProgressBar _bar;
        private readonly Label _percent;
        private readonly Label _elapsed;
        private readonly Label _eta;
        private readonly Label _components;
        private readonly ProgressChart _chart;

        public OperationCard(Guid operationId)
        {
            OperationId = operationId;
            DoubleBuffered = true;

            Height = 246;
            BorderStyle = BorderStyle.FixedSingle;
            Margin = new Padding(4);
            BackColor = Theme.Current.CardBackground;

            _title = new Label
            {
                Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
                AutoSize = false,
                ForeColor = Theme.Current.PrimaryText,
                Location = new Point(8, 6),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _bar = new ProgressBar
            {
                Location = new Point(8, 30),
                Height = 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Minimum = 0,
                Maximum = 100,
                MarqueeAnimationSpeed = 40
            };

            _percent = new Label
            {
                Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
                AutoSize = false,
                ForeColor = Theme.Current.PrimaryText,
                TextAlign = ContentAlignment.MiddleRight,
                Height = 20,
                Width = 66,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _elapsed = new Label
            {
                AutoSize = false,
                ForeColor = Theme.Current.SecondaryText,
                Location = new Point(8, 56),
                Height = 18,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _eta = new Label
            {
                AutoSize = false,
                Location = new Point(8, 78),
                Height = 18,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _components = new Label
            {
                AutoSize = false,
                ForeColor = Theme.Current.SecondaryText,
                Location = new Point(8, 98),
                Height = 18,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _chart = new ProgressChart
            {
                Location = new Point(8, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.AddRange(new Control[] { _title, _bar, _percent, _elapsed, _eta, _components, _chart });
            Resize += (s, e) => LayoutChildren();
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            _title.Size = new Size(Width - 16, 20);
            _bar.Width = Math.Max(50, Width - 90);
            _percent.Location = new Point(Width - 76, 30);
            _elapsed.Size = new Size(Width - 16, 18);
            _eta.Size = new Size(Width - 16, 18);
            _components.Size = new Size(Width - 16, 18);
            _chart.Size = new Size(Width - 16, Math.Max(40, Height - 128));
        }

        public void SetChartData(ChartModel model)
        {
            _chart.SetModel(model);
        }

        public void ApplyTheme(ThemePalette p)
        {
            BackColor = p.CardBackground;
            _title.ForeColor = p.PrimaryText;
            _percent.ForeColor = p.PrimaryText;
            _elapsed.ForeColor = p.SecondaryText;
            _components.ForeColor = p.SecondaryText;
            // _eta.ForeColor wird pro Status in UpdateData() gesetzt (EtaGood/Warn/Neutral)
            _chart.BackColor = p.ChartBackground;
            _chart.Invalidate();
            Invalidate();
        }

        public void UpdateData(string title, string percentText, int barValue, bool marquee,
            string elapsedText, string etaText, Color etaColor, string componentsText)
        {
            // Nur bei tatsächlicher Änderung setzen -> kein unnötiges Repaint
            if (_title.Text != title) _title.Text = title;
            if (_percent.Text != percentText) _percent.Text = percentText;
            if (_elapsed.Text != elapsedText) _elapsed.Text = elapsedText;
            if (_eta.Text != etaText) _eta.Text = etaText;
            if (_eta.ForeColor != etaColor) _eta.ForeColor = etaColor;
            var comp = componentsText ?? "";
            if (_components.Text != comp) _components.Text = comp;

            if (marquee)
            {
                if (_bar.Style != ProgressBarStyle.Marquee) _bar.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                if (_bar.Style != ProgressBarStyle.Continuous) _bar.Style = ProgressBarStyle.Continuous;
                var clamped = Math.Max(0, Math.Min(100, barValue));
                if (_bar.Value != clamped) _bar.Value = clamped;
            }
        }
    }

    #endregion

    public class MonitorControl : PluginControlBase
    {
        #region Felder / UI

        private ToolStrip _toolStrip;
        private ToolStripButton _btnClose;
        private ToolStripButton _btnRefresh;
        private ToolStripButton _btnAutoRefresh;
        private ToolStripLabel _lblInterval;
        private ToolStripComboBox _cmbInterval;
        private ToolStripLabel _lblFilterCaption;
        private ToolStripTextBox _txtFilter;
        private ToolStripDropDownButton _btnLanguage;
        private ToolStripDropDownButton _btnTheme;
        private ToolStripMenuItem _miThemeLight;
        private ToolStripMenuItem _miThemeDark;
        private ToolStripLabel _lblLastRefresh;

        private SplitContainer _split;
        private Label _activeHeader;
        private Label _historyHeader;
        private FlowLayoutPanel _activePanel;
        private Label _lblNoActive;
        private DataGridView _grid;

        private Timer _timer;
        private bool _isLoading;
        private DataTable _historyTable;
        private MonitorData _lastData;

        // Abschluss-Benachrichtigungen
        private readonly HashSet<Guid> _lastActiveIds = new HashSet<Guid>();
        private readonly Dictionary<Guid, string> _lastActiveNames = new Dictionary<Guid, string>();

        // Fortschritts-Messpunkte je Vorgang für ratenbasierte ETA
        private readonly Dictionary<Guid, ProgressTracker> _trackers = new Dictionary<Guid, ProgressTracker>();

        private class ProgressTracker
        {
            public readonly List<KeyValuePair<DateTime, double>> Samples = new List<KeyValuePair<DateTime, double>>();
            public double? LastRemainingSeconds;
            public DateTime? LastRemainingAt;
        }

        #endregion

        public MonitorControl()
        {
            Theme.Set(Theme.DetectSystem());

            BuildUi();
            ApplyTexts();
            ApplyTheme();

            _timer = new Timer { Interval = 5000 };
            _timer.Tick += (s, e) =>
            {
                if (_btnAutoRefresh.Checked && Service != null && !_isLoading)
                {
                    RefreshData(showOverlay: false);
                }
            };
        }

        #region UI Aufbau

        private void BuildUi()
        {
            SuspendLayout();

            _toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(24, 24) };

            _btnClose = new ToolStripButton { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _btnClose.Click += (s, e) => CloseTool();

            _btnRefresh = new ToolStripButton { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _btnRefresh.Click += (s, e) => ExecuteMethod(() => RefreshData(showOverlay: true));

            _btnAutoRefresh = new ToolStripButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                CheckOnClick = true,
                Checked = true
            };
            _btnAutoRefresh.CheckedChanged += (s, e) => UpdateAutoRefreshText();

            _lblInterval = new ToolStripLabel();
            _cmbInterval = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            _cmbInterval.Items.AddRange(new object[] { "3 s", "5 s", "10 s", "30 s", "60 s" });
            _cmbInterval.SelectedIndex = 1;
            _cmbInterval.SelectedIndexChanged += (s, e) =>
            {
                var seconds = int.Parse(_cmbInterval.SelectedItem.ToString().Replace(" s", ""));
                _timer.Interval = seconds * 1000;
            };

            _lblFilterCaption = new ToolStripLabel();
            _txtFilter = new ToolStripTextBox { Width = 180 };
            _txtFilter.TextChanged += (s, e) => ApplyGridFilter();

            _btnLanguage = new ToolStripDropDownButton { DisplayStyle = ToolStripItemDisplayStyle.Text };
            var deItem = new ToolStripMenuItem("Deutsch") { Checked = I18n.German };
            var enItem = new ToolStripMenuItem("English") { Checked = !I18n.German };
            deItem.Click += (s, e) => SwitchLanguage(true, deItem, enItem);
            enItem.Click += (s, e) => SwitchLanguage(false, deItem, enItem);
            _btnLanguage.DropDownItems.AddRange(new ToolStripItem[] { deItem, enItem });

            _btnTheme = new ToolStripDropDownButton { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _miThemeLight = new ToolStripMenuItem { Checked = Theme.Mode == ThemeMode.Light };
            _miThemeDark = new ToolStripMenuItem { Checked = Theme.Mode == ThemeMode.Dark };
            _miThemeLight.Click += (s, e) => SwitchTheme(ThemeMode.Light);
            _miThemeDark.Click += (s, e) => SwitchTheme(ThemeMode.Dark);
            _btnTheme.DropDownItems.AddRange(new ToolStripItem[] { _miThemeLight, _miThemeDark });

            _lblLastRefresh = new ToolStripLabel { Alignment = ToolStripItemAlignment.Right };

            _toolStrip.Items.AddRange(new ToolStripItem[]
            {
                _btnClose,
                new ToolStripSeparator(),
                _btnRefresh,
                new ToolStripSeparator(),
                _btnAutoRefresh,
                _lblInterval,
                _cmbInterval,
                new ToolStripSeparator(),
                _lblFilterCaption,
                _txtFilter,
                new ToolStripSeparator(),
                _btnLanguage,
                _btnTheme,
                _lblLastRefresh
            });

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel1
            };

            // Oben: aktive Vorgänge
            _activeHeader = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
                Height = 26,
                Padding = new Padding(6, 6, 0, 0)
            };

            _activePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(4)
            };
            EnableDoubleBuffering(_activePanel);
            _activePanel.Resize += (s, e) => ResizeCards();

            _lblNoActive = new Label
            {
                AutoSize = true,
                ForeColor = Color.Gray,
                Margin = new Padding(8)
            };
            _activePanel.Controls.Add(_lblNoActive);

            _split.Panel1.Controls.Add(_activePanel);
            _split.Panel1.Controls.Add(_activeHeader);

            // Unten: History-Grid
            _historyHeader = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
                Height = 26,
                Padding = new Padding(6, 6, 0, 0)
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.None
            };
            EnableDoubleBuffering(_grid);
            _grid.CellFormatting += Grid_CellFormatting;

            _split.Panel2.Controls.Add(_grid);
            _split.Panel2.Controls.Add(_historyHeader);

            Controls.Add(_split);
            Controls.Add(_toolStrip);

            Dock = DockStyle.Fill;
            ResumeLayout(false);
        }

        private static void EnableDoubleBuffering(Control control)
        {
            try
            {
                typeof(Control)
                    .GetProperty("DoubleBuffered",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(control, true, null);
            }
            catch { /* rein kosmetisch */ }
        }

        private void SwitchLanguage(bool german, ToolStripMenuItem deItem, ToolStripMenuItem enItem)
        {
            I18n.German = german;
            deItem.Checked = german;
            enItem.Checked = !german;
            ApplyTexts();

            // Zuletzt geladene Daten mit neuer Sprache neu rendern (History-Grid + Karten)
            if (_lastData != null)
            {
                BindData(_lastData, refreshTimestamp: false);
            }
        }

        private void SwitchTheme(ThemeMode mode)
        {
            if (Theme.Mode == mode) return;
            Theme.Set(mode);
            ApplyTheme();
        }

        /// <summary>Wendet die aktive Palette auf alle Controls an. Läuft ohne aktive Verbindung.</summary>
        private void ApplyTheme()
        {
            var p = Theme.Current;

            BackColor = p.AppBackground;

            // Toolbar
            _toolStrip.Renderer = new ThemedToolStripRenderer(p);
            _toolStrip.BackColor = p.ToolStripBackground;
            _toolStrip.ForeColor = p.ToolStripText;
            _cmbInterval.FlatStyle = FlatStyle.Flat;
            _cmbInterval.BackColor = p.ToolStripBackground;
            _cmbInterval.ForeColor = p.ToolStripText;
            _txtFilter.BorderStyle = BorderStyle.FixedSingle;
            _txtFilter.BackColor = p.GridCellBackground;
            _txtFilter.ForeColor = p.PrimaryText;
            _lblLastRefresh.ForeColor = p.SecondaryText;

            if (_miThemeLight != null) _miThemeLight.Checked = Theme.Mode == ThemeMode.Light;
            if (_miThemeDark != null) _miThemeDark.Checked = Theme.Mode == ThemeMode.Dark;

            // Split + Header
            _split.BackColor = p.PanelBackground;
            _split.Panel1.BackColor = p.PanelBackground;
            _split.Panel2.BackColor = p.PanelBackground;
            _activeHeader.BackColor = p.PanelBackground;
            _activeHeader.ForeColor = p.HeaderText;
            _historyHeader.BackColor = p.PanelBackground;
            _historyHeader.ForeColor = p.HeaderText;

            // Aktive Vorgänge
            _activePanel.BackColor = p.PanelBackground;
            _lblNoActive.BackColor = p.PanelBackground;
            _lblNoActive.ForeColor = p.MutedText;
            foreach (var card in _activePanel.Controls.OfType<OperationCard>())
            {
                card.ApplyTheme(p);
            }

            ApplyGridTheme(p);

            Invalidate(true);
        }

        private void ApplyGridTheme(ThemePalette p)
        {
            _grid.EnableHeadersVisualStyles = false;
            _grid.BackgroundColor = p.GridBackground;
            _grid.GridColor = p.GridLines;
            _grid.DefaultCellStyle.BackColor = p.GridCellBackground;
            _grid.DefaultCellStyle.ForeColor = p.GridCellText;
            _grid.DefaultCellStyle.SelectionBackColor = p.GridSelectionBackground;
            _grid.DefaultCellStyle.SelectionForeColor = p.GridSelectionText;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = p.GridHeaderBackground;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = p.GridHeaderText;
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = p.GridHeaderBackground;
            _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = p.GridHeaderText;
            _grid.RowHeadersDefaultCellStyle.BackColor = p.GridHeaderBackground;

            // Bereits gebundene Zeilen neu einfärben (CellFormatting nutzt die neue Palette)
            if (_grid.DataSource != null) _grid.Invalidate();
        }

        private void ApplyTexts()
        {
            _btnClose.Text = I18n.T("Close");
            _btnRefresh.Text = I18n.T("Refresh");
            UpdateAutoRefreshText();
            _lblInterval.Text = I18n.T("Interval");
            _lblFilterCaption.Text = I18n.T("Filter");
            _btnLanguage.Text = I18n.T("Language");
            _btnTheme.Text = I18n.T("Theme");
            _miThemeLight.Text = I18n.T("ThemeLight");
            _miThemeDark.Text = I18n.T("ThemeDark");
            _activeHeader.Text = I18n.T("ActiveHeader");
            _historyHeader.Text = I18n.T("HistoryHeader");
            _lblNoActive.Text = I18n.T("NoActive");
            ApplyGridHeaders();
        }

        private void UpdateAutoRefreshText()
        {
            _btnAutoRefresh.Text = _btnAutoRefresh.Checked ? I18n.T("AutoOn") : I18n.T("AutoOff");
        }

        private void ApplyGridHeaders()
        {
            if (_grid.DataSource == null) return;

            _grid.Columns["Solution"].HeaderText = I18n.T("ColSolution");
            _grid.Columns["Version"].HeaderText = I18n.T("ColVersion");
            _grid.Columns["Publisher"].HeaderText = I18n.T("ColPublisher");
            _grid.Columns["Operation"].HeaderText = I18n.T("ColOperation");
            _grid.Columns["SubOperation"].HeaderText = I18n.T("ColSubOperation");
            _grid.Columns["Status"].HeaderText = I18n.T("ColStatus");
            _grid.Columns["Result"].HeaderText = I18n.T("ColResult");
            _grid.Columns["Start"].HeaderText = I18n.T("ColStart");
            _grid.Columns["End"].HeaderText = I18n.T("ColEnd");
            _grid.Columns["Duration"].HeaderText = I18n.T("ColDuration");
            _grid.Columns["Error"].HeaderText = I18n.T("ColError");
        }

        #endregion

        #region Lifecycle

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                if (_split.Height > 420) _split.SplitterDistance = 300;
                else if (_split.Height > 260) _split.SplitterDistance = 190;
            }
            catch { /* Layout noch nicht bereit - Standardwert behalten */ }

            if (Service != null)
            {
                ExecuteMethod(() => RefreshData(showOverlay: true));
            }
            _timer.Start();
        }

        public override void UpdateConnection(IOrganizationService newService,
            McTools.Xrm.Connection.ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            _lastActiveIds.Clear();
            _lastActiveNames.Clear();
            _trackers.Clear();
            ExecuteMethod(() => RefreshData(showOverlay: true));
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _timer.Stop();
            _timer.Dispose();
            base.ClosingPlugin(info);
        }

        #endregion

        #region Daten laden

        private class MonitorData
        {
            public List<Entity> History { get; set; }
            public List<Entity> ActiveImportJobs { get; set; }
            public DateTime UtcNow { get; set; }
        }

        private void RefreshData(bool showOverlay)
        {
            if (_isLoading || Service == null) return;
            _isLoading = true;

            if (showOverlay)
            {
                WorkAsync(new WorkAsyncInfo
                {
                    Message = I18n.T("Loading"),
                    Work = (worker, args) => args.Result = QueryData(),
                    PostWorkCallBack = args =>
                    {
                        _isLoading = false;
                        if (args.Error != null)
                        {
                            ShowErrorNotification(I18n.F("LoadError", args.Error.Message), null);
                            return;
                        }
                        BindData((MonitorData)args.Result, refreshTimestamp: true);
                    }
                });
            }
            else
            {
                // Stiller Refresh im Hintergrund (kein Overlay, damit die UI nicht flackert)
                Task.Run(() => QueryData()).ContinueWith(t =>
                {
                    _isLoading = false;
                    if (IsDisposed) return;
                    if (t.Exception != null)
                    {
                        LogError(I18n.F("AutoFailed", t.Exception.GetBaseException().Message));
                        return;
                    }
                    BindData(t.Result, refreshTimestamp: true);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private MonitorData QueryData()
        {
            var data = new MonitorData { UtcNow = DateTime.UtcNow };

            // 1) Solution History (Quelle für aktive + vergangene Vorgänge)
            var historyQuery = new QueryExpression("msdyn_solutionhistory")
            {
                ColumnSet = new ColumnSet(
                    "msdyn_solutionhistoryid",
                    "msdyn_name",
                    "msdyn_solutionversion",
                    "msdyn_publishername",
                    "msdyn_ismanaged",
                    "msdyn_operation",
                    "msdyn_suboperation",
                    "msdyn_status",
                    "msdyn_result",
                    "msdyn_starttime",
                    "msdyn_endtime",
                    "msdyn_totaltime",
                    "msdyn_exceptionmessage"),
                TopCount = 200
            };
            historyQuery.AddOrder("msdyn_starttime", OrderType.Descending);

            data.History = Service.RetrieveMultiple(historyQuery).Entities.ToList();

            // 2) Aktive Import-Jobs (liefern den echten Prozentwert beim Import)
            var jobQuery = new QueryExpression("importjob")
            {
                ColumnSet = new ColumnSet("importjobid", "solutionname", "progress", "startedon", "completedon", "modifiedon", "data"),
                TopCount = 20
            };
            jobQuery.Criteria.AddCondition("completedon", ConditionOperator.Null);
            jobQuery.AddOrder("startedon", OrderType.Descending);

            try
            {
                data.ActiveImportJobs = Service.RetrieveMultiple(jobQuery).Entities.ToList();
            }
            catch
            {
                // importjob kann ohne Leserechte nicht abfragbar sein -> dann nur History anzeigen
                data.ActiveImportJobs = new List<Entity>();
            }

            return data;
        }

        #endregion

        #region Daten binden

        private void BindData(MonitorData data, bool refreshTimestamp)
        {
            _lastData = data;

            if (refreshTimestamp)
            {
                _lblLastRefresh.Text = I18n.F("LastRefresh", DateTime.Now.ToString("HH:mm:ss"));
            }

            var active = data.History
                .Where(h => h.GetAttributeValue<DateTime?>("msdyn_endtime") == null)
                .ToList();

            BindActiveOperations(active, data);
            NotifyCompletedOperations(active, data.History);
            BindHistoryGrid(data.History, data.UtcNow);
        }

        private void BindActiveOperations(List<Entity> active, MonitorData data)
        {
            var activeIds = new HashSet<Guid>(active.Select(a => a.Id));

            // Karten entfernen, deren Vorgang beendet ist (kein Controls.Clear -> kein Flackern)
            foreach (var card in _activePanel.Controls.OfType<OperationCard>().ToList())
            {
                if (!activeIds.Contains(card.OperationId))
                {
                    _activePanel.Controls.Remove(card);
                    card.Dispose();
                    _trackers.Remove(card.OperationId);
                }
            }

            var existing = _activePanel.Controls.OfType<OperationCard>().ToDictionary(c => c.OperationId);

            _lblNoActive.Visible = active.Count == 0;

            foreach (var op in active)
            {
                OperationCard card;
                if (!existing.TryGetValue(op.Id, out card))
                {
                    card = new OperationCard(op.Id) { Width = CardWidth() };
                    _activePanel.Controls.Add(card);
                }

                UpdateOperationCard(card, op, data);
            }
        }

        private int CardWidth()
        {
            return Math.Max(400, _activePanel.ClientSize.Width - 30);
        }

        private void ResizeCards()
        {
            foreach (var card in _activePanel.Controls.OfType<OperationCard>())
            {
                card.Width = CardWidth();
            }
        }

        private void UpdateOperationCard(OperationCard card, Entity op, MonitorData data)
        {
            var name = op.GetAttributeValue<string>("msdyn_name") ?? I18n.T("Unknown");
            var version = op.GetAttributeValue<string>("msdyn_solutionversion");
            var operation = GetFormatted(op, "msdyn_operation");
            var subOperation = GetFormatted(op, "msdyn_suboperation");
            var start = op.GetAttributeValue<DateTime?>("msdyn_starttime");

            TimeSpan? elapsed = null;
            if (start.HasValue)
            {
                elapsed = data.UtcNow - DateTime.SpecifyKind(start.Value, DateTimeKind.Utc);
                if (elapsed.Value < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            }

            var importJob = FindImportJob(name, data.ActiveImportJobs);
            double? progress = importJob == null ? (double?)null : importJob.GetAttributeValue<double?>("progress");
            var components = importJob == null ? null : ParseImportJobData(importJob.GetAttributeValue<string>("data"));
            string componentsText = FormatComponents(components);

            TimeSpan? avgDuration = GetAverageDuration(data.History, name, operation, op.Id);

            // Messpunkte sammeln (für ratenbasierte ETA)
            ProgressTracker tracker;
            if (!_trackers.TryGetValue(op.Id, out tracker))
            {
                tracker = new ProgressTracker();
                _trackers[op.Id] = tracker;
            }

            if (progress.HasValue)
            {
                tracker.Samples.Add(new KeyValuePair<DateTime, double>(data.UtcNow, progress.Value));

                // Fürs Diagramm alle Punkte behalten, aber die Menge deckeln:
                // bei > 600 Punkten jeden zweiten alten Punkt ausdünnen (Verlauf bleibt sichtbar)
                if (tracker.Samples.Count > 600)
                {
                    for (int i = tracker.Samples.Count - 100; i > 0; i -= 2)
                    {
                        tracker.Samples.RemoveAt(i);
                    }
                }
            }

            string etaText;
            Color etaColor;
            int barValue = 0;
            bool marquee = false;
            string percentText;

            if (progress.HasValue)
            {
                barValue = (int)Math.Round(Math.Max(0, Math.Min(100, progress.Value)));
                percentText = progress.Value.ToString("0.#") + " %";

                // Rate aus den letzten Messpunkten (statt linear vom Start hochzurechnen -
                // der Plattform-Fortschritt springt anfangs schnell hoch und hängt dann)
                double? ratePerSecond = ComputeRate(tracker, data.UtcNow);
                bool stalled = IsStalled(tracker, data.UtcNow);

                if (stalled && progress.Value < 100)
                {
                    // Ehrlich sein statt einer immer weiter steigenden Fantasie-Restzeit
                    tracker.LastRemainingSeconds = null;
                    etaText = I18n.F("Stalled", progress.Value.ToString("0.#"));
                    etaColor = Theme.Current.EtaWarn;
                }
                else if (ratePerSecond.HasValue && ratePerSecond.Value > 0.005)
                {
                    var rawRemaining = (100 - progress.Value) / ratePerSecond.Value;
                    var smoothed = SmoothRemaining(tracker, rawRemaining, data.UtcNow);
                    etaText = I18n.F("RemainingApprox", FormatDuration(TimeSpan.FromSeconds(smoothed)));
                    etaColor = Theme.Current.EtaGood;
                }
                else if (avgDuration.HasValue && elapsed.HasValue && avgDuration.Value > elapsed.Value)
                {
                    var smoothed = SmoothRemaining(tracker, (avgDuration.Value - elapsed.Value).TotalSeconds, data.UtcNow);
                    etaText = I18n.F("EstFromHistory",
                        FormatDuration(TimeSpan.FromSeconds(smoothed)),
                        FormatDuration(avgDuration.Value));
                    etaColor = Theme.Current.EtaWarn;
                }
                else
                {
                    tracker.LastRemainingSeconds = null;
                    etaText = I18n.T("Measuring");
                    etaColor = Theme.Current.EtaNeutral;
                }
            }
            else if (avgDuration.HasValue && elapsed.HasValue && avgDuration.Value.TotalSeconds > 0)
            {
                // Kein Plattform-Prozentwert (Uninstall/Upgrade) -> Schätzung aus Historie
                if (elapsed.Value <= avgDuration.Value)
                {
                    var estPercent = elapsed.Value.TotalSeconds / avgDuration.Value.TotalSeconds * 100;
                    barValue = (int)Math.Round(Math.Min(95, estPercent));
                    percentText = "~" + barValue + " %";
                    var smoothed = SmoothRemaining(tracker, (avgDuration.Value - elapsed.Value).TotalSeconds, data.UtcNow);
                    etaText = I18n.F("EstFromHistory",
                        FormatDuration(TimeSpan.FromSeconds(smoothed)),
                        FormatDuration(avgDuration.Value));
                    etaColor = Theme.Current.EtaWarn;
                }
                else
                {
                    // Länger als der Durchschnitt -> nicht bei 95-99 % festhängen, sondern Marquee
                    marquee = true;
                    percentText = "– %";
                    tracker.LastRemainingSeconds = null;
                    etaText = I18n.F("LongerThanUsual", FormatDuration(avgDuration.Value));
                    etaColor = Theme.Current.EtaWarn;
                }
            }
            else
            {
                // Solution war noch nie da und kein Prozentwert -> keine Fake-Prozente anzeigen
                marquee = true;
                percentText = "– %";
                tracker.LastRemainingSeconds = null;
                etaText = I18n.T("NoEstimate");
                etaColor = Theme.Current.EtaNeutral;
            }

            var title = name
                + (string.IsNullOrEmpty(version) ? "" : " (v" + version + ")")
                + "  –  " + operation
                + (string.IsNullOrEmpty(subOperation) ? "" : " / " + subOperation);

            // Zeitraum: Start-Uhrzeit und - wenn eine Restzeit bekannt ist - die voraussichtliche
            // Ziel-Uhrzeit (Start -> Ziel). Ohne Restzeit-Schätzung nur das Startdatum.
            string rangeSuffix = "";
            if (start.HasValue)
            {
                if (tracker.LastRemainingSeconds.HasValue)
                {
                    var target = DateTime.Now.AddSeconds(tracker.LastRemainingSeconds.Value);
                    rangeSuffix = "   (" + I18n.F("TimeRange",
                        start.Value.ToLocalTime().ToString("HH:mm:ss"),
                        target.ToString("HH:mm")) + ")";
                }
                else
                {
                    rangeSuffix = "   (" + I18n.F("StartAt",
                        start.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")) + ")";
                }
            }

            var elapsedText = I18n.F("RunningSince",
                elapsed.HasValue ? FormatDuration(elapsed.Value) : I18n.T("Unknown")) + rangeSuffix;

            card.UpdateData(title, percentText, barValue, marquee, elapsedText, etaText, etaColor, componentsText);

            // Diagramm-Daten: Ist-Kurve, Prognose, Ø-Referenz, "jetzt"- und ETA-Marker
            var chart = new ChartModel
            {
                ElapsedSeconds = elapsed.HasValue ? elapsed.Value.TotalSeconds : 0,
                RemainingSeconds = tracker.LastRemainingSeconds,
                AvgSeconds = avgDuration.HasValue ? avgDuration.Value.TotalSeconds : (double?)null,
                HasRealProgress = progress.HasValue,
                CurrentPercent = progress.HasValue ? progress : (marquee ? (double?)null : barValue)
            };

            if (start.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(start.Value, DateTimeKind.Utc);
                foreach (var s in tracker.Samples)
                {
                    var x = (s.Key - startUtc).TotalSeconds;
                    if (x >= 0)
                    {
                        chart.Samples.Add(new PointF((float)x, (float)s.Value));
                    }
                }
            }

            card.SetChartData(chart);
        }

        private static double? ComputeRate(ProgressTracker tracker, DateTime utcNow)
        {
            // Rate (%/s) über die Messpunkte der letzten 2 Minuten
            var window = tracker.Samples
                .Where(s => (utcNow - s.Key).TotalSeconds <= 120)
                .ToList();
            if (window.Count < 2) return null;

            var first = window[0];
            var last = window[window.Count - 1];
            var seconds = (last.Key - first.Key).TotalSeconds;
            if (seconds < 5) return null;

            return (last.Value - first.Value) / seconds;
        }

        private static bool IsStalled(ProgressTracker tracker, DateTime utcNow)
        {
            // Stockt: seit mindestens 45 s Messpunkte vorhanden, aber quasi keine Bewegung
            var window = tracker.Samples
                .Where(s => (utcNow - s.Key).TotalSeconds <= 120)
                .ToList();
            if (window.Count < 2) return false;

            var first = window[0];
            var last = window[window.Count - 1];
            var seconds = (last.Key - first.Key).TotalSeconds;

            return seconds >= 45 && (last.Value - first.Value) < 0.5;
        }

        private static double SmoothRemaining(ProgressTracker tracker, double rawSeconds, DateTime utcNow)
        {
            // Exponentielle Glättung, damit die Restzeit nicht bei jedem Refresh springt.
            // Erwartung: Restzeit sinkt mit der Zeit - der alte Wert wird daher um die
            // vergangene Zeit reduziert, bevor er mit dem neuen Wert gemischt wird.
            double smoothed = rawSeconds;

            if (tracker.LastRemainingSeconds.HasValue && tracker.LastRemainingAt.HasValue)
            {
                var dt = (utcNow - tracker.LastRemainingAt.Value).TotalSeconds;
                var decayed = Math.Max(0, tracker.LastRemainingSeconds.Value - dt);
                smoothed = decayed * 0.6 + rawSeconds * 0.4;
            }

            tracker.LastRemainingSeconds = smoothed;
            tracker.LastRemainingAt = utcNow;
            return Math.Max(0, smoothed);
        }

        private static Entity FindImportJob(string solutionName, List<Entity> jobs)
        {
            if (string.IsNullOrEmpty(solutionName) || jobs == null || jobs.Count == 0) return null;

            return jobs
                .Where(j => string.Equals(j.GetAttributeValue<string>("solutionname"), solutionName,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(j => j.GetAttributeValue<DateTime?>("startedon") ?? DateTime.MinValue)
                .FirstOrDefault();
        }

        // Per-Komponenten-Fortschritt aus der importjob.data-XML.
        private class ComponentProgress
        {
            public int Processed;
            public int Succeeded;
            public int Warnings;
            public int Failed;
        }

        /// <summary>
        /// Wertet die (während des Imports fortlaufend gefüllte) importjob.data-XML aus und zählt
        /// die pro Komponente eingetragenen &lt;result&gt;-Elemente nach ihrem Ergebnis. Rein additive,
        /// vollständig abgesicherte Auswertung: Bei unbekanntem/unvollständigem XML wird null geliefert
        /// und das restliche Tool läuft unverändert weiter.
        /// </summary>
        private static ComponentProgress ParseImportJobData(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;

            try
            {
                var doc = XDocument.Parse(xml);

                // Pro Komponente trägt der Import ein <result result="success|warning|failure" .../> ein.
                // Die zusammenfassenden Knoten unter <results> (Plural) übergehen wir.
                var results = doc.Descendants()
                    .Where(r => r.Name.LocalName == "result"
                             && r.Attribute("result") != null
                             && (r.Parent == null || r.Parent.Name.LocalName != "results"))
                    .ToList();
                if (results.Count == 0) return null;

                var cp = new ComponentProgress();
                foreach (var r in results)
                {
                    var res = ((string)r.Attribute("result") ?? "").Trim();
                    cp.Processed++;

                    if (res.Equals("success", StringComparison.OrdinalIgnoreCase))
                        cp.Succeeded++;
                    else if (res.Equals("warning", StringComparison.OrdinalIgnoreCase))
                        cp.Warnings++;
                    else if (res.Equals("failure", StringComparison.OrdinalIgnoreCase) ||
                             res.Equals("error", StringComparison.OrdinalIgnoreCase))
                        cp.Failed++;
                }

                return cp;
            }
            catch
            {
                // Schema unbekannt / XML noch unvollständig -> Feature still deaktivieren
                return null;
            }
        }

        private static string FormatComponents(ComponentProgress cp)
        {
            if (cp == null || cp.Processed == 0) return "";

            var detail = new List<string> { I18n.F("CompOk", cp.Succeeded) };
            if (cp.Warnings > 0) detail.Add(I18n.F("CompWarn", cp.Warnings));
            if (cp.Failed > 0) detail.Add(I18n.F("CompErr", cp.Failed));

            var prefix = cp.Failed > 0 ? "⚠ " : "";
            return prefix + I18n.F("CompProcessed", cp.Processed) + " (" + string.Join(" · ", detail) + ")";
        }

        private static TimeSpan? GetAverageDuration(List<Entity> history, string solutionName, string operation, Guid excludeId)
        {
            var samples = history
                .Where(h => h.Id != excludeId)
                .Where(h => string.Equals(h.GetAttributeValue<string>("msdyn_name"), solutionName, StringComparison.OrdinalIgnoreCase))
                .Where(h => string.Equals(GetFormatted(h, "msdyn_operation"), operation, StringComparison.OrdinalIgnoreCase))
                .Where(h => h.GetAttributeValue<DateTime?>("msdyn_starttime") != null
                         && h.GetAttributeValue<DateTime?>("msdyn_endtime") != null)
                .Select(h => h.GetAttributeValue<DateTime>("msdyn_endtime") - h.GetAttributeValue<DateTime>("msdyn_starttime"))
                .Where(d => d > TimeSpan.Zero)
                .Take(5)
                .ToList();

            if (samples.Count == 0) return null;
            return TimeSpan.FromSeconds(samples.Average(d => d.TotalSeconds));
        }

        private void NotifyCompletedOperations(List<Entity> active, List<Entity> history)
        {
            var currentIds = new HashSet<Guid>(active.Select(a => a.Id));

            foreach (var oldId in _lastActiveIds.Where(id => !currentIds.Contains(id)).ToList())
            {
                var finished = history.FirstOrDefault(h => h.Id == oldId);
                string n;
                var name = _lastActiveNames.TryGetValue(oldId, out n) ? n : I18n.T("Unknown");
                var result = finished != null ? GetFormatted(finished, "msdyn_result") : null;

                ShowInfoNotification(
                    I18n.F("Completed", name) + (string.IsNullOrEmpty(result) ? "" : I18n.F("ResultSuffix", result)),
                    null);
            }

            _lastActiveIds.Clear();
            _lastActiveNames.Clear();
            foreach (var a in active)
            {
                _lastActiveIds.Add(a.Id);
                _lastActiveNames[a.Id] = a.GetAttributeValue<string>("msdyn_name") ?? I18n.T("Unknown");
            }
        }

        private void BindHistoryGrid(List<Entity> history, DateTime utcNow)
        {
            if (_historyTable == null)
            {
                // Interne Spaltennamen bleiben fix (sprachneutral); nur HeaderText wird übersetzt
                _historyTable = new DataTable();
                _historyTable.Columns.Add("Solution", typeof(string));
                _historyTable.Columns.Add("Version", typeof(string));
                _historyTable.Columns.Add("Publisher", typeof(string));
                _historyTable.Columns.Add("Operation", typeof(string));
                _historyTable.Columns.Add("SubOperation", typeof(string));
                _historyTable.Columns.Add("Status", typeof(string));
                _historyTable.Columns.Add("Result", typeof(string));
                _historyTable.Columns.Add("Start", typeof(DateTime));
                _historyTable.Columns.Add("End", typeof(DateTime));
                _historyTable.Columns.Add("Duration", typeof(string));
                _historyTable.Columns.Add("Error", typeof(string));
            }

            // Scroll-Position und Auswahl merken, damit der Refresh nicht "springt"
            var firstVisibleRow = _grid.FirstDisplayedScrollingRowIndex;
            var selectedRow = _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].Index : -1;

            _grid.SuspendLayout();
            _historyTable.BeginLoadData();
            _historyTable.Rows.Clear();

            foreach (var h in history)
            {
                var start = h.GetAttributeValue<DateTime?>("msdyn_starttime");
                var end = h.GetAttributeValue<DateTime?>("msdyn_endtime");

                string duration;
                if (start.HasValue && end.HasValue)
                {
                    duration = FormatDuration(end.Value - start.Value);
                }
                else if (start.HasValue)
                {
                    duration = I18n.F("Running", FormatDuration(utcNow - DateTime.SpecifyKind(start.Value, DateTimeKind.Utc)));
                }
                else
                {
                    duration = "";
                }

                var row = _historyTable.NewRow();
                row["Solution"] = h.GetAttributeValue<string>("msdyn_name") ?? "";
                row["Version"] = h.GetAttributeValue<string>("msdyn_solutionversion") ?? "";
                row["Publisher"] = h.GetAttributeValue<string>("msdyn_publishername") ?? "";
                row["Operation"] = GetFormatted(h, "msdyn_operation");
                row["SubOperation"] = GetFormatted(h, "msdyn_suboperation");
                row["Status"] = GetFormatted(h, "msdyn_status");
                row["Result"] = end.HasValue ? GetFormatted(h, "msdyn_result") : "";
                if (start.HasValue) row["Start"] = start.Value.ToLocalTime();
                if (end.HasValue) row["End"] = end.Value.ToLocalTime();
                row["Duration"] = duration;
                row["Error"] = h.GetAttributeValue<string>("msdyn_exceptionmessage") ?? "";
                _historyTable.Rows.Add(row);
            }

            _historyTable.EndLoadData();

            if (_grid.DataSource == null)
            {
                _grid.DataSource = _historyTable;
                _grid.Columns["Start"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
                _grid.Columns["End"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
                _grid.Columns["Error"].FillWeight = 200;
                ApplyGridHeaders();
            }

            ApplyGridFilter();

            try
            {
                if (selectedRow >= 0 && selectedRow < _grid.Rows.Count)
                {
                    _grid.Rows[selectedRow].Selected = true;
                }
                if (firstVisibleRow >= 0 && firstVisibleRow < _grid.Rows.Count)
                {
                    _grid.FirstDisplayedScrollingRowIndex = firstVisibleRow;
                }
            }
            catch { /* Scroll-Wiederherstellung ist optional */ }

            _grid.ResumeLayout();
        }

        private void ApplyGridFilter()
        {
            if (_historyTable == null) return;

            var filter = _txtFilter.Text == null ? null : _txtFilter.Text.Trim().Replace("'", "''");
            _historyTable.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                ? string.Empty
                : string.Format("Solution LIKE '%{0}%' OR Operation LIKE '%{0}%' OR Publisher LIKE '%{0}%'", filter);
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = _grid.Rows[e.RowIndex];
            var endValue = row.Cells["End"].Value;
            var result = (row.Cells["Result"].Value ?? "").ToString();

            var p = Theme.Current;

            // Text- und Auswahlfarben immer aus der Palette setzen. Sonst bleibt der Text
            // in eingefärbten Zeilen auf der geerbten (dunklen) Farbe hängen und ist im
            // Dark Mode nicht lesbar.
            row.DefaultCellStyle.ForeColor = p.GridCellText;
            row.DefaultCellStyle.SelectionBackColor = p.GridSelectionBackground;
            row.DefaultCellStyle.SelectionForeColor = p.GridSelectionText;

            if (endValue == null || endValue == DBNull.Value)
            {
                row.DefaultCellStyle.BackColor = p.RowActive; // aktiv
            }
            else if (result.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     result.IndexOf("fehl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     result.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                row.DefaultCellStyle.BackColor = p.RowError; // Fehler
            }
            else
            {
                row.DefaultCellStyle.BackColor = p.GridCellBackground;
            }
        }

        #endregion

        #region Helpers

        private static string GetFormatted(Entity e, string attribute)
        {
            if (e.FormattedValues.ContainsKey(attribute))
            {
                return e.FormattedValues[attribute];
            }

            if (e.Contains(attribute))
            {
                var value = e[attribute];
                if (value is OptionSetValue) return ((OptionSetValue)value).Value.ToString();
                return value == null ? "" : value.ToString();
            }

            return "";
        }

        private static string FormatDuration(TimeSpan ts)
        {
            if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
            if (ts.TotalHours >= 1) return string.Format("{0} h {1} min {2} s", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            if (ts.TotalMinutes >= 1) return string.Format("{0} min {1} s", (int)ts.TotalMinutes, ts.Seconds);
            return ts.Seconds + " s";
        }

        #endregion
    }
}
