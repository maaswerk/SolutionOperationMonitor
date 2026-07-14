using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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
            { "LastRefresh",      new[] { "Zuletzt aktualisiert: {0}", "Last refreshed: {0}" } },
            { "ActiveHeader",     new[] { "Aktive Solution-Vorgänge", "Active solution operations" } },
            { "HistoryHeader",    new[] { "Solution History (letzte 200 Vorgänge, max. 180 Tage rückwirkend)", "Solution history (last 200 operations, max. 180 days back)" } },
            { "NoActive",         new[] { "Zurzeit läuft kein Import / Upgrade / Uninstall.", "No import / upgrade / uninstall is currently running." } },
            { "Loading",          new[] { "Lade Solution History und aktive Vorgänge...", "Loading solution history and active operations..." } },
            { "LoadError",        new[] { "Fehler beim Laden: {0}", "Error while loading: {0}" } },
            { "AutoFailed",       new[] { "Auto-Refresh fehlgeschlagen: {0}", "Auto refresh failed: {0}" } },
            { "RunningSince",     new[] { "Läuft seit: {0}", "Running for: {0}" } },
            { "StartAt",          new[] { "Start: {0}", "Started: {0}" } },
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
            BackColor = Color.White;
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

            var m = _model;
            var plot = new Rectangle(36, 6, Width - 44, Height - 24);
            if (plot.Width < 60 || plot.Height < 30) return;

            using (var axisPen = new Pen(Color.FromArgb(200, 205, 215)))
            using (var gridPen = new Pen(Color.FromArgb(235, 238, 244)))
            using (var labelBrush = new SolidBrush(Color.FromArgb(110, 118, 135)))
            using (var labelFont = new Font(Font.FontFamily, 7f))
            {
                // Y-Grid: 0 / 25 / 50 / 75 / 100 %
                for (int p = 0; p <= 100; p += 25)
                {
                    var y = plot.Bottom - (float)(p / 100.0 * plot.Height);
                    g.DrawLine(p == 0 ? axisPen : gridPen, plot.Left, y, plot.Right, y);
                    if (p % 50 == 0)
                    {
                        g.DrawString(p + "%", labelFont, labelBrush, 2, y - 6);
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
                    using (var avgPen = new Pen(Color.FromArgb(165, 172, 188), 1.4f) { DashStyle = DashStyle.Dot })
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

                    using (var actualPen = new Pen(Color.FromArgb(63, 106, 216), 2.2f)
                        { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    {
                        if (pts.Length == 1)
                        {
                            g.FillEllipse(Brushes.RoyalBlue, pts[0].X - 2.5f, pts[0].Y - 2.5f, 5, 5);
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
                    using (var estPen = new Pen(Color.FromArgb(63, 106, 216), 2.2f) { DashStyle = DashStyle.Dash })
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
                    using (var projPen = new Pen(Color.FromArgb(34, 150, 83), 2f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(projPen, X(lastX), Y(lastY), X(m.ElapsedSeconds + m.RemainingSeconds.Value), Y(100));
                    }
                }

                // "jetzt"-Marker
                using (var nowPen = new Pen(Color.FromArgb(235, 140, 30), 1.6f))
                {
                    var nx = X(m.ElapsedSeconds);
                    g.DrawLine(nowPen, nx, plot.Top, nx, plot.Bottom);
                    g.DrawString(I18n.T("ChartNow"), labelFont, new SolidBrush(Color.FromArgb(200, 110, 10)),
                        Math.Min(nx + 2, plot.Right - 30), plot.Top);
                }

                // ETA-Marker mit Uhrzeit
                if (m.RemainingSeconds.HasValue)
                {
                    var etaSec = m.ElapsedSeconds + m.RemainingSeconds.Value;
                    var ex = X(etaSec);
                    using (var etaPen = new Pen(Color.FromArgb(34, 150, 83), 1.6f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(etaPen, ex, plot.Top, ex, plot.Bottom);
                    }
                    var etaClock = DateTime.Now.AddSeconds(m.RemainingSeconds.Value).ToString("HH:mm");
                    var etaLabel = I18n.F("ChartEta", etaClock);
                    var size = g.MeasureString(etaLabel, labelFont);
                    var lx = Math.Max(plot.Left, Math.Min(ex - size.Width / 2, plot.Right - size.Width));
                    g.DrawString(etaLabel, labelFont, new SolidBrush(Color.FromArgb(25, 120, 65)),
                        lx, plot.Bottom + 3);
                }

                // Mini-Legende oben links im Plot
                float lyy = plot.Top + 2;
                float lxx = plot.Left + 6;
                using (var actualPen = new Pen(Color.FromArgb(63, 106, 216), 2f))
                using (var projPen = new Pen(Color.FromArgb(34, 150, 83), 2f) { DashStyle = DashStyle.Dash })
                using (var avgPen = new Pen(Color.FromArgb(165, 172, 188), 1.4f) { DashStyle = DashStyle.Dot })
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
        private readonly ProgressChart _chart;

        public OperationCard(Guid operationId)
        {
            OperationId = operationId;
            DoubleBuffered = true;

            Height = 228;
            BorderStyle = BorderStyle.FixedSingle;
            Margin = new Padding(4);
            BackColor = Color.FromArgb(248, 250, 255);

            _title = new Label
            {
                Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
                AutoSize = false,
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
                TextAlign = ContentAlignment.MiddleRight,
                Height = 20,
                Width = 66,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _elapsed = new Label
            {
                AutoSize = false,
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

            _chart = new ProgressChart
            {
                Location = new Point(8, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.AddRange(new Control[] { _title, _bar, _percent, _elapsed, _eta, _chart });
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
            _chart.Size = new Size(Width - 16, Math.Max(40, Height - 108));
        }

        public void SetChartData(ChartModel model)
        {
            _chart.SetModel(model);
        }

        public void UpdateData(string title, string percentText, int barValue, bool marquee,
            string elapsedText, string etaText, Color etaColor)
        {
            // Nur bei tatsächlicher Änderung setzen -> kein unnötiges Repaint
            if (_title.Text != title) _title.Text = title;
            if (_percent.Text != percentText) _percent.Text = percentText;
            if (_elapsed.Text != elapsedText) _elapsed.Text = elapsedText;
            if (_eta.Text != etaText) _eta.Text = etaText;
            if (_eta.ForeColor != etaColor) _eta.ForeColor = etaColor;

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
            BuildUi();
            ApplyTexts();

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

        private void ApplyTexts()
        {
            _btnClose.Text = I18n.T("Close");
            _btnRefresh.Text = I18n.T("Refresh");
            UpdateAutoRefreshText();
            _lblInterval.Text = I18n.T("Interval");
            _lblFilterCaption.Text = I18n.T("Filter");
            _btnLanguage.Text = I18n.T("Language");
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
                ColumnSet = new ColumnSet("importjobid", "solutionname", "progress", "startedon", "completedon", "modifiedon"),
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

            double? progress = FindImportJobProgress(name, data.ActiveImportJobs);
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
                    etaColor = Color.DarkOrange;
                }
                else if (ratePerSecond.HasValue && ratePerSecond.Value > 0.005)
                {
                    var rawRemaining = (100 - progress.Value) / ratePerSecond.Value;
                    var smoothed = SmoothRemaining(tracker, rawRemaining, data.UtcNow);
                    etaText = I18n.F("RemainingApprox", FormatDuration(TimeSpan.FromSeconds(smoothed)));
                    etaColor = Color.DarkGreen;
                }
                else if (avgDuration.HasValue && elapsed.HasValue && avgDuration.Value > elapsed.Value)
                {
                    var smoothed = SmoothRemaining(tracker, (avgDuration.Value - elapsed.Value).TotalSeconds, data.UtcNow);
                    etaText = I18n.F("EstFromHistory",
                        FormatDuration(TimeSpan.FromSeconds(smoothed)),
                        FormatDuration(avgDuration.Value));
                    etaColor = Color.DarkOrange;
                }
                else
                {
                    tracker.LastRemainingSeconds = null;
                    etaText = I18n.T("Measuring");
                    etaColor = Color.Gray;
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
                    etaColor = Color.DarkOrange;
                }
                else
                {
                    // Länger als der Durchschnitt -> nicht bei 95-99 % festhängen, sondern Marquee
                    marquee = true;
                    percentText = "– %";
                    tracker.LastRemainingSeconds = null;
                    etaText = I18n.F("LongerThanUsual", FormatDuration(avgDuration.Value));
                    etaColor = Color.DarkOrange;
                }
            }
            else
            {
                // Solution war noch nie da und kein Prozentwert -> keine Fake-Prozente anzeigen
                marquee = true;
                percentText = "– %";
                tracker.LastRemainingSeconds = null;
                etaText = I18n.T("NoEstimate");
                etaColor = Color.Gray;
            }

            var title = name
                + (string.IsNullOrEmpty(version) ? "" : " (v" + version + ")")
                + "  –  " + operation
                + (string.IsNullOrEmpty(subOperation) ? "" : " / " + subOperation);

            var elapsedText = I18n.F("RunningSince",
                elapsed.HasValue ? FormatDuration(elapsed.Value) : I18n.T("Unknown"))
                + (start.HasValue
                    ? "   (" + I18n.F("StartAt", start.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")) + ")"
                    : "");

            card.UpdateData(title, percentText, barValue, marquee, elapsedText, etaText, etaColor);

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

        private static double? FindImportJobProgress(string solutionName, List<Entity> jobs)
        {
            if (string.IsNullOrEmpty(solutionName) || jobs == null || jobs.Count == 0) return null;

            var match = jobs
                .Where(j => string.Equals(j.GetAttributeValue<string>("solutionname"), solutionName,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(j => j.GetAttributeValue<DateTime?>("startedon") ?? DateTime.MinValue)
                .FirstOrDefault();

            return match == null ? (double?)null : match.GetAttributeValue<double?>("progress");
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

            if (endValue == null || endValue == DBNull.Value)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 220); // gelblich = aktiv
            }
            else if (result.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     result.IndexOf("fehl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     result.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230); // rötlich = Fehler
            }
            else
            {
                row.DefaultCellStyle.BackColor = SystemColors.Window;
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
