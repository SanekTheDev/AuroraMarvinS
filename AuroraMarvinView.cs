namespace AuroraMarvin
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using System.Windows.Forms.DataVisualization.Charting;

    public partial class AuroraMarvinView : Form, IAuroraMarvinView
    {
        private const string MARVINVERSION = "1.0.S (based on v2.2.0.0)";
        private const string AURORAVERSION = "2.7.1";
        private const string FORUMURL = "http://aurora2.pentarch.org/index.php?topic=12233.msg145907";
        private readonly Version marvinVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        private IAuroraMarvinController controller;
        private bool showTechTree;
        private Dictionary<string, bool> showMsgItem;
        private Dictionary<string, string[]> overviewMsgs;
        private int startYear;
        private DataTable resourceChartData;
        private DataTable wealthChartData;
        private DataTable populationMineralChartData;
        private ComboBox fuelChartRangeComboBox;
        private ComboBox maintenanceChartRangeComboBox;
        private ComboBox populationChartRangeComboBox;
        private ComboBox wealthChartRangeComboBox;
        private ComboBox populationMineralChartRangeComboBox;
        private ComboBox populationMineralChartDisplayComboBox;
        private ComboBox fuelChartXAxisComboBox;
        private ComboBox maintenanceChartXAxisComboBox;
        private ComboBox populationChartXAxisComboBox;
        private ComboBox wealthChartXAxisComboBox;
        private ComboBox mineralChartXAxisComboBox;
        private ComboBox populationMineralChartXAxisComboBox;
        private readonly Dictionary<Chart, ComboBox> chartTrendComboBoxes = new Dictionary<Chart, ComboBox>();
        private readonly Dictionary<Chart, ComboBox> chartOverlayComboBoxes = new Dictionary<Chart, ComboBox>();
        private ToolStripComboBox themeComboBox;
        private ToolStripButton resourceColorsButton;
        private readonly Dictionary<Chart, HashSet<string>> customMineralSelections = new Dictionary<Chart, HashSet<string>>();
        private readonly Dictionary<Chart, List<ChartEventMarker>> chartEventMarkers = new Dictionary<Chart, List<ChartEventMarker>>();
        private readonly Dictionary<Chart, Point> chartPanStartPoints = new Dictionary<Chart, Point>();
        private readonly Dictionary<Chart, double> chartPanStartMin = new Dictionary<Chart, double>();
        private readonly Dictionary<Chart, double> chartPanStartMax = new Dictionary<Chart, double>();

        private sealed class ChartEventMarker
        {
            public string Text { get; set; }
            public double GameTimeSeconds { get; set; }
        }

        public AuroraMarvinView()
        {
            this.InitializeComponent();
#if DEBUG
            this.Text += " [DEBUG]";
#endif
            this.Text += $" (v{MARVINVERSION} for Aurora 4x C# {AURORAVERSION})";
            this.InitializeThemeSelector();
            this.InitializeInfoTab();
            this.InitializeChartInteractions();
            this.FormClosing += this.AuroraMarvinView_FormClosing;

            this.mineralChartRangeComboBox.SelectedIndex = 1;
            this.mineralChartDisplayComboBox.SelectedIndex = 0;
            this.SetupStandardChartControls();
            this.LoadUserChartSettings();
            this.DisplayStartingYearWorkaroundWarning(false);
        }

        private void InitializeThemeSelector()
        {
            ToolStripLabel themeLabel = new ToolStripLabel("Theme:");
            this.themeComboBox = new ToolStripComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 95,
                Name = "themeComboBox"
            };
            this.themeComboBox.Items.Add("Light");
            this.themeComboBox.Items.Add("Dark");
            this.themeComboBox.SelectedIndex = 0;
            this.themeComboBox.SelectedIndexChanged += this.ThemeComboBox_SelectedIndexChanged;
            this.menuStrip1.Items.Add(themeLabel);
            this.menuStrip1.Items.Add(this.themeComboBox);

            this.resourceColorsButton = new ToolStripButton("Colors...")
            {
                Name = "resourceColorsButton",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = "Customize resource/mineral colors"
            };
            this.resourceColorsButton.Click += this.ResourceColorsButton_Click;
            this.menuStrip1.Items.Add(this.resourceColorsButton);

            ThemeManager.Apply(this, MarvinTheme.Light);
        }

        private void InitializeInfoTab()
        {
            TabPage infoTabPage = new TabPage
            {
                Name = "infoTabPage",
                Text = "Info",
                Padding = new Padding(12)
            };

            RichTextBox infoBox = new RichTextBox
            {
                Name = "infoBox",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                DetectUrls = true,
                Font = new Font("Segoe UI", 10F),
                Text =
                    "Aurora MarvinS\n" +
                    "v1.0.S (based on v2.2.0.0) for Aurora 4X C# 2.7.1\n\n" +
                    "ABOUT THIS VERSION\n" +
                    "This version was updated by Sanek and is a modification/update of the original Aurora Marvin project, for Aurora 4X C# 2.7.1.\n\n" +
                    "VERSION NOTE\n" +
                    "Aurora MarvinS is an unofficial continuation/modification of the original Aurora Marvin project. The original creator, Scnaeg, gave permission for the project to be continued and published as a free and open-source project and agreed to the plan to publish Aurora MarvinS under the GNU GPL v3.0. I made it because I wanted to use Marvin in the newer version of the game with features that I wanted to see that were not present before.\n\n" +
                    "GRAPHING AND ANALYSIS\n" +
                    "• Resource charts support All history, Last 20, Last 50 and Last 100 data points.\n" +
                    "• The X-axis can use Relative points or real Game time.\n" +
                    "• Save/data-point numbers and in-game dates are shown on the chart.\n" +
                    "• Individual minerals can be displayed with automatic Y-axis scaling.\n" +
                    "• Multiple minerals can be compared.\n" +
                    "• Charts use thicker lines and data-point markers for readability.\n" +
                    "• Trend visualization can show rising and falling segments between saves.\n" +
                    "• Moving averages and a start-value baseline are available as optional overlays.\n" +
                    "• Linear projection can be used to visualize a simple continuation of the current trend.\n" +
                    "• Chart zoom, panning, reset zoom, PNG export and CSV export are available where supported.\n" +
                    "• Event markers can be used to mark important moments in a campaign.\n" +
                    "• Tooltips provide save number, game date, value and change information where supported.\n\n" +
                    "BASE MINERAL COLORS\n" +
                    "Duranium - red\n" +
                    "Neutronium - orange\n" +
                    "Corbomite - green\n" +
                    "Tritanium - turquoise\n" +
                    "Boronide - cyan\n" +
                    "Mercassium - purple\n" +
                    "Vendarite - yellow\n" +
                    "Sorium - pink\n" +
                    "Uridium - navy blue\n" +
                    "Corundium - blue\n" +
                    "Gallicite - brown\n\n" +
                    "THEMES AND COLORS\n" +
                    "Light and Dark themes are available from the Theme dropdown. Resource colors are shared between themes so each resource keeps the same visual identity.\n" +
                    "Use the Colors... button next to the Theme selector to customize resource colors. Custom colors are saved between launches.\n" +
                    "The color customization is intended to make the charts easier to personalize and more accessible for users with different forms of color blindness, and for people who simply think my choices are bad, which is fair.\n\n" +
                    "YEAR 1 SUPPORT\n" +
                    "Chart date handling was changed so games starting in year 1 can be displayed without forcing their dates to year 100.\n\n" +
                    "TECH TREE\n" +
                    "The original Tech Tree in Aurora Marvin was replaced with a redesigned visualization focused on making technology dependencies easier to follow. The modified Tech Tree is organized into separate color-coded research-field sections and uses a left-to-right layout. Technologies are evenly spaced, with connected research chains arranged so that technologies are placed next to their successors where possible. Technologies that unlock multiple branches are positioned between those branches when possible to make the relationships easier to understand. Each technology displays its research cost and prerequisites are shown with directional arrows. Dependencies between different research fields are also visualized with colored nodes and connection lines to make their origins easier to identify. The Tech Tree can now show the player's research progress: researched technologies are marked with a green indicator and check mark, while the technology currently being researched is marked with a yellow indicator. The Research status dropdown can filter the tree to all technologies, unresearched technologies, researched technologies, or the technology currently being researched. Automatically researched technologies and technologies available at the start of the game are excluded from the displayed research layout, as are BioEnergy and Swarm technologies. These filters and status indicators only affect the Tech Tree visualization and do not modify the Aurora database.\n\n" +
                    "The Tech Tree layout implementation is inspired by the layout shared in the following Reddit post:\n" +
                    "https://www.reddit.com/r/aurora/comments/1vrzho8/complete_tech_tree_for_aurora_271/\n\n" +
                    "OTHER CHANGES\n" +
                    "• Application settings for themes, chart ranges and related chart preferences can be saved between launches.\n" +
                    "• The original Aurora database access model is retained; the added features are intended to analyze and visualize the data Marvin already reads.\n\n" +
                    "LICENSE\n" +
                    "Aurora MarvinS is released under the GNU General Public License v3.0 (GPL-3.0), with the original author's permission to continue and publish the project as free and open source. See the LICENSE file included with the source code.\n\n" +
                    "CREDITS\n" +
                    "Original Aurora Marvin creator: Scnaeg. Aurora MarvinS was updated by Sanek. See CREDITS.md for details.\n\n" +
                    "AI ASSISTANCE DISCLAIMER\n" +
                    "ChatGPT was used to assist with the development, implementation and refinement of features in Aurora MarvinS."
            };

            infoTabPage.Controls.Add(infoBox);
            this.tabControl1.Controls.Add(infoTabPage);
        }

        private void ResourceColorsButton_Click(object sender, EventArgs e)
        {
            using (ResourceColorDialog dialog = new ResourceColorDialog(ThemeManager.GetResourceColors()))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                ThemeManager.LoadResourceColors(dialog.SelectedColors);
                this.SaveUserChartSettings();
                foreach (Chart chart in new[] { this.chart1, this.chart2, this.chart3, this.chart4, this.chart5, this.chart6 })
                {
                    ThemeManager.ApplyChartTheme(chart, ThemeManager.CurrentTheme == MarvinTheme.Dark);
                }
                this.RefreshVisibleChartsAfterColorChange();
            }
        }

        private void RefreshVisibleChartsAfterColorChange()
        {
            foreach (Chart chart in new[] { this.chart1, this.chart2, this.chart3, this.chart4, this.chart5, this.chart6 })
            {
                foreach (Series series in chart.Series)
                {
                    if (!series.Name.StartsWith("__", StringComparison.Ordinal) && ThemeManager.ResourceNames.Contains(series.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        series.Color = ThemeManager.GetResourceColor(series.Name);
                    }
                }
                chart.Invalidate();
            }
        }

        private void ThemeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            MarvinTheme theme = this.themeComboBox != null && this.themeComboBox.SelectedItem != null && this.themeComboBox.SelectedItem.ToString() == "Dark"
                ? MarvinTheme.Dark
                : MarvinTheme.Light;
            ThemeManager.Apply(this, theme);
            foreach (Control control in this.tabPage10.Controls)
            {
                TechTreeControl techTree = control as TechTreeControl;
                if (techTree != null) techTree.RefreshTheme();
            }
        }

        private string GetSettingsPath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AuroraMarvinS");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "chart-settings.txt");
        }

        private void SaveUserChartSettings()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("Theme=" + (ThemeManager.CurrentTheme == MarvinTheme.Dark ? "Dark" : "Light"));
                foreach (var pair in ThemeManager.GetResourceColors())
                {
                    lines.Add("Color." + pair.Key + "=" + pair.Value.ToArgb());
                }
                lines.Add("MineralRange=" + this.mineralChartRangeComboBox.SelectedItem);
                lines.Add("MineralDisplay=" + this.mineralChartDisplayComboBox.SelectedItem);
                lines.Add("MineralXAxis=" + this.mineralChartXAxisComboBox.SelectedItem);
                lines.Add("FuelRange=" + this.fuelChartRangeComboBox.SelectedItem);
                lines.Add("FuelXAxis=" + this.fuelChartXAxisComboBox.SelectedItem);
                lines.Add("MaintenanceRange=" + this.maintenanceChartRangeComboBox.SelectedItem);
                lines.Add("MaintenanceXAxis=" + this.maintenanceChartXAxisComboBox.SelectedItem);
                lines.Add("PopulationRange=" + this.populationChartRangeComboBox.SelectedItem);
                lines.Add("PopulationXAxis=" + this.populationChartXAxisComboBox.SelectedItem);
                lines.Add("WealthRange=" + this.wealthChartRangeComboBox.SelectedItem);
                lines.Add("WealthXAxis=" + this.wealthChartXAxisComboBox.SelectedItem);
                lines.Add("PopulationMineralRange=" + this.populationMineralChartRangeComboBox.SelectedItem);
                lines.Add("PopulationMineralDisplay=" + this.populationMineralChartDisplayComboBox.SelectedItem);
                lines.Add("PopulationMineralXAxis=" + this.populationMineralChartXAxisComboBox.SelectedItem);
                lines.Add("MineralTrend=" + this.GetTrendComboBox(this.chart1).SelectedItem);
                lines.Add("MineralOverlay=" + this.GetOverlayComboBox(this.chart1).SelectedItem);
                lines.Add("FuelTrend=" + this.GetTrendComboBox(this.chart2).SelectedItem);
                lines.Add("FuelOverlay=" + this.GetOverlayComboBox(this.chart2).SelectedItem);
                lines.Add("MaintenanceTrend=" + this.GetTrendComboBox(this.chart3).SelectedItem);
                lines.Add("MaintenanceOverlay=" + this.GetOverlayComboBox(this.chart3).SelectedItem);
                lines.Add("PopulationTrend=" + this.GetTrendComboBox(this.chart4).SelectedItem);
                lines.Add("PopulationOverlay=" + this.GetOverlayComboBox(this.chart4).SelectedItem);
                lines.Add("WealthTrend=" + this.GetTrendComboBox(this.chart5).SelectedItem);
                lines.Add("WealthOverlay=" + this.GetOverlayComboBox(this.chart5).SelectedItem);
                lines.Add("PopulationMineralTrend=" + this.GetTrendComboBox(this.chart6).SelectedItem);
                lines.Add("PopulationMineralOverlay=" + this.GetOverlayComboBox(this.chart6).SelectedItem);
                File.WriteAllLines(this.GetSettingsPath(), lines.ToArray());
            }
            catch
            {
                // Preferences are optional and must never prevent Marvin from closing.
            }
        }

        private void LoadUserChartSettings()
        {
            try
            {
                if (!File.Exists(this.GetSettingsPath())) return;
                Dictionary<string, string> values = File.ReadAllLines(this.GetSettingsPath())
                    .Where(l => l.Contains("="))
                    .Select(l => l.Split(new[] { '=' }, 2))
                    .ToDictionary(a => a[0], a => a[1]);
                Dictionary<string, Color> savedColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
                foreach (string resourceName in ThemeManager.ResourceNames)
                {
                    string key = "Color." + resourceName;
                    string raw;
                    int argb;
                    if (values.TryGetValue(key, out raw) && int.TryParse(raw, out argb))
                    {
                        try { savedColors[resourceName] = Color.FromArgb(argb); } catch { }
                    }
                }
                ThemeManager.LoadResourceColors(savedColors);
                this.SelectToolStripComboValue(this.themeComboBox, values, "Theme", "Light");
                this.SelectComboValue(this.mineralChartRangeComboBox, values, "MineralRange", "Last 20 points");
                this.SelectComboValue(this.mineralChartDisplayComboBox, values, "MineralDisplay", "All minerals");
                this.SelectComboValue(this.fuelChartRangeComboBox, values, "FuelRange", "Last 20 points");
                this.SelectComboValue(this.maintenanceChartRangeComboBox, values, "MaintenanceRange", "Last 20 points");
                this.SelectComboValue(this.populationChartRangeComboBox, values, "PopulationRange", "Last 20 points");
                this.SelectComboValue(this.wealthChartRangeComboBox, values, "WealthRange", "Last 20 points");
                this.SelectComboValue(this.populationMineralChartRangeComboBox, values, "PopulationMineralRange", "Last 20 points");
                this.SelectComboValue(this.populationMineralChartDisplayComboBox, values, "PopulationMineralDisplay", "All minerals");
                this.SelectComboValue(this.mineralChartXAxisComboBox, values, "MineralXAxis", "Relative");
                this.SelectComboValue(this.fuelChartXAxisComboBox, values, "FuelXAxis", "Relative");
                this.SelectComboValue(this.maintenanceChartXAxisComboBox, values, "MaintenanceXAxis", "Relative");
                this.SelectComboValue(this.populationChartXAxisComboBox, values, "PopulationXAxis", "Relative");
                this.SelectComboValue(this.wealthChartXAxisComboBox, values, "WealthXAxis", "Relative");
                this.SelectComboValue(this.populationMineralChartXAxisComboBox, values, "PopulationMineralXAxis", "Relative");
                this.SelectComboValue(this.GetTrendComboBox(this.chart1), values, "MineralTrend", "Off");
                this.SelectComboValue(this.GetOverlayComboBox(this.chart1), values, "MineralOverlay", "None");
                this.SelectComboValue(this.GetTrendComboBox(this.chart2), values, "FuelTrend", "Off");
                this.SelectComboValue(this.GetOverlayComboBox(this.chart2), values, "FuelOverlay", "None");
                this.SelectComboValue(this.GetTrendComboBox(this.chart3), values, "MaintenanceTrend", "Off");
                this.SelectComboValue(this.GetOverlayComboBox(this.chart3), values, "MaintenanceOverlay", "None");
                this.SelectComboValue(this.GetTrendComboBox(this.chart4), values, "PopulationTrend", "Off");
                this.SelectComboValue(this.GetOverlayComboBox(this.chart4), values, "PopulationOverlay", "None");
                this.SelectComboValue(this.GetTrendComboBox(this.chart5), values, "WealthTrend", "Off");
                this.SelectComboValue(this.GetOverlayComboBox(this.chart5), values, "WealthOverlay", "None");
                this.SelectComboValue(this.GetTrendComboBox(this.chart6), values, "PopulationMineralTrend", "Off");
                this.SelectComboValue(this.GetOverlayComboBox(this.chart6), values, "PopulationMineralOverlay", "None");
                if (values.ContainsKey("Theme"))
                {
                    this.themeComboBox.SelectedItem = values["Theme"] == "Dark" ? "Dark" : "Light";
                }
            }
            catch
            {
                // Ignore malformed preferences and use defaults.
            }
        }

        private void SelectToolStripComboValue(ToolStripComboBox combo, Dictionary<string, string> values, string key, string fallback)
        {
            if (combo == null) return;
            string value;
            if (!values.TryGetValue(key, out value)) value = fallback;
            int index = combo.Items.IndexOf(value);
            if (index >= 0) combo.SelectedIndex = index;
        }

        private void SelectComboValue(ComboBox combo, Dictionary<string, string> values, string key, string fallback)
        {
            if (combo == null) return;
            string value;
            if (!values.TryGetValue(key, out value)) value = fallback;
            int index = combo.Items.IndexOf(value);
            if (index >= 0) combo.SelectedIndex = index;
        }

        private void AuroraMarvinView_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.SaveUserChartSettings();
        }

        private void InitializeChartInteractions()
        {
            foreach (Chart chart in new[] { this.chart1, this.chart2, this.chart3, this.chart4, this.chart5, this.chart6 })
            {
                if (!this.chartEventMarkers.ContainsKey(chart)) this.chartEventMarkers[chart] = new List<ChartEventMarker>();
                ChartArea area = chart.ChartAreas[0];
                area.CursorX.IsUserEnabled = true;
                area.CursorX.IsUserSelectionEnabled = true;
                area.CursorX.Interval = 0;
                area.AxisX.ScaleView.Zoomable = true;
                area.AxisY.ScaleView.Zoomable = true;
                chart.MouseWheel += this.Chart_MouseWheel;
                chart.MouseDown += this.Chart_MouseDown;
                chart.MouseMove += this.Chart_MouseMove;
                chart.MouseUp += this.Chart_MouseUp;
            }
        }

        private void Chart_MouseWheel(object sender, MouseEventArgs e)
        {
            Chart chart = sender as Chart;
            if (chart == null || chart.ChartAreas.Count == 0) return;
            ChartArea area = chart.ChartAreas[0];
            if (e.Delta == 0) return;
            double factor = e.Delta > 0 ? 0.8 : 1.25;
            double min = area.AxisX.ScaleView.ViewMinimum;
            double max = area.AxisX.ScaleView.ViewMaximum;
            if (double.IsNaN(min) || double.IsNaN(max) || max <= min)
            {
                min = area.AxisX.Minimum;
                max = area.AxisX.Maximum;
            }
            if (double.IsNaN(min) || double.IsNaN(max) || max <= min) return;
            double center;
            try { center = area.AxisX.PixelPositionToValue(e.X); } catch { center = (min + max) / 2.0; }
            double newWidth = (max - min) * factor;
            newWidth = Math.Max(newWidth, Math.Max(1.0, (max - min) * 0.02));
            double newMin = center - (center - min) * factor;
            double newMax = newMin + newWidth;
            if (newMin < area.AxisX.Minimum) { newMin = area.AxisX.Minimum; newMax = newMin + newWidth; }
            if (newMax > area.AxisX.Maximum) { newMax = area.AxisX.Maximum; newMin = newMax - newWidth; }
            if (newMax > newMin)
            {
                area.AxisX.ScaleView.Zoom(newMin, newMax);
            }
        }

        private void Chart_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Middle) return;
            Chart chart = sender as Chart;
            if (chart == null) return;
            ChartArea area = chart.ChartAreas[0];
            this.chartPanStartPoints[chart] = e.Location;
            this.chartPanStartMin[chart] = area.AxisX.ScaleView.IsZoomed ? area.AxisX.ScaleView.ViewMinimum : area.AxisX.Minimum;
            this.chartPanStartMax[chart] = area.AxisX.ScaleView.IsZoomed ? area.AxisX.ScaleView.ViewMaximum : area.AxisX.Maximum;
        }

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Middle) return;
            Chart chart = sender as Chart;
            if (chart == null || !this.chartPanStartPoints.ContainsKey(chart)) return;
            ChartArea area = chart.ChartAreas[0];
            double startValue = area.AxisX.PixelPositionToValue(this.chartPanStartPoints[chart].X);
            double currentValue = area.AxisX.PixelPositionToValue(e.X);
            double delta = startValue - currentValue;
            double min = this.chartPanStartMin[chart] + delta;
            double max = this.chartPanStartMax[chart] + delta;
            double fullMin = area.AxisX.Minimum;
            double fullMax = area.AxisX.Maximum;
            if (!double.IsNaN(fullMin) && !double.IsNaN(fullMax))
            {
                if (min < fullMin) { max += fullMin - min; min = fullMin; }
                if (max > fullMax) { min -= max - fullMax; max = fullMax; }
            }
            if (max > min) area.AxisX.ScaleView.Zoom(min, max);
        }

        private void Chart_MouseUp(object sender, MouseEventArgs e)
        {
            Chart chart = sender as Chart;
            if (chart != null && this.chartPanStartPoints.ContainsKey(chart)) this.chartPanStartPoints.Remove(chart);
        }

        private void ResetChartZoom(Chart chart)
        {
            if (chart == null || chart.ChartAreas.Count == 0) return;
            chart.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
        }

        private void ExportChart(Chart chart)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG image|*.png|CSV data|*.csv";
                dialog.FileName = "AuroraMarvinS_Chart";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (dialog.FilterIndex == 1)
                {
                    chart.SaveImage(dialog.FileName, ChartImageFormat.Png);
                    return;
                }
                DataTable data = this.GetCurrentChartData(chart);
                if (data == null) return;
                List<Series> visible = chart.Series.Cast<Series>().Where(s => s.Enabled && !s.Name.StartsWith("__", StringComparison.Ordinal)).ToList();
                StringBuilder csv = new StringBuilder();
                csv.Append("DataPointIndex,GameDate,GameTimeSeconds");
                foreach (Series series in visible) csv.Append(",").Append(this.CsvEscape(series.Name));
                csv.AppendLine();
                foreach (DataRow row in data.Rows)
                {
                    csv.Append(row["DataPointIndex"]).Append(",").Append(this.CsvEscape(((DateTime)row["DateTime"]).ToString("dd/MM/yyyy HH:mm:ss"))).Append(",").Append(row["GameTimeSeconds"]);
                    foreach (Series series in visible)
                    {
                        string column = series.YValueMembers;
                        csv.Append(",").Append(data.Columns.Contains(column) ? this.CsvEscape(row[column].ToString()) : "");
                    }
                    csv.AppendLine();
                }
                File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);
            }
        }

        private string CsvEscape(string value)
        {
            if (value == null) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n")) return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private DataTable GetCurrentChartData(Chart chart)
        {
            if (chart == this.chart1 || chart == this.chart2 || chart == this.chart3 || chart == this.chart4) return this.GetChartRangeData(this.resourceChartData, this.GetRangeComboBox(chart));
            if (chart == this.chart5) return this.GetChartRangeData(this.wealthChartData, this.wealthChartRangeComboBox);
            if (chart == this.chart6) return this.GetChartRangeData(this.populationMineralChartData, this.populationMineralChartRangeComboBox);
            return null;
        }

        private ComboBox GetRangeComboBox(Chart chart)
        {
            if (chart == this.chart2) return this.fuelChartRangeComboBox;
            if (chart == this.chart3) return this.maintenanceChartRangeComboBox;
            if (chart == this.chart4) return this.populationChartRangeComboBox;
            return this.mineralChartRangeComboBox;
        }

        private void AddChartEvent(Chart chart)
        {
            DataTable data = this.GetCurrentChartData(chart);
            if (data == null || data.Rows.Count == 0) return;
            using (Form dialog = new Form { Text = "Add chart event", StartPosition = FormStartPosition.CenterParent, Size = new Size(360, 170), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            {
                ComboBox points = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 12), Width = 320 };
                foreach (DataRow row in data.Rows) points.Items.Add(row["DataPointIndex"] + " - " + ((DateTime)row["DateTime"]).ToString("dd/MM/yyyy HH:mm:ss"));
                points.SelectedIndex = data.Rows.Count - 1;
                TextBox text = new TextBox { Location = new Point(12, 48), Width = 320 };
                Button ok = new Button { Text = "Add", DialogResult = DialogResult.OK, Location = new Point(170, 85), Width = 75 };
                Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(257, 85), Width = 75 };
                dialog.Controls.AddRange(new Control[] { points, text, ok, cancel });
                dialog.AcceptButton = ok; dialog.CancelButton = cancel;
                if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(text.Text)) return;
                DataRow selected = data.Rows[points.SelectedIndex];
                this.chartEventMarkers[chart].Add(new ChartEventMarker { Text = text.Text.Trim(), GameTimeSeconds = Convert.ToDouble(selected["GameTimeSeconds"]) });
                this.RefreshChartForAnalysis(chart);
            }
        }

        private void ClearChartEvents(Chart chart)
        {
            if (this.chartEventMarkers.ContainsKey(chart)) this.chartEventMarkers[chart].Clear();
            this.RefreshChartForAnalysis(chart);
        }

        private void ApplyChartEventMarkers(Chart chart)
        {
            ChartArea area = chart.ChartAreas[0];
            area.AxisX.StripLines.Clear();
            List<ChartEventMarker> markers;
            if (!this.chartEventMarkers.TryGetValue(chart, out markers)) return;
            DataTable data = this.GetCurrentChartData(chart);
            if (data == null || data.Rows.Count == 0) return;
            bool gameTime = this.IsGameTimeXAxis(this.GetXAxisComboBox(chart));
            foreach (ChartEventMarker marker in markers)
            {
                DataRow nearest = data.AsEnumerable().OrderBy(r => Math.Abs(Convert.ToDouble(r["GameTimeSeconds"]) - marker.GameTimeSeconds)).FirstOrDefault();
                if (nearest == null) continue;
                double x = gameTime ? Convert.ToDouble(nearest["GameTimeSeconds"]) : Convert.ToDouble(nearest["DataPointIndex"]);
                StripLine line = new StripLine
                {
                    Interval = 0,
                    IntervalOffset = x,
                    StripWidth = 0,
                    BorderWidth = 2,
                    BorderDashStyle = ChartDashStyle.Dash,
                    BorderColor = ThemeManager.CurrentTheme == MarvinTheme.Dark ? Color.Gold : Color.DarkOrange,
                    Text = marker.Text,
                    ForeColor = ThemeManager.CurrentTheme == MarvinTheme.Dark ? Color.Gold : Color.DarkOrange,
                    TextLineAlignment = StringAlignment.Near,
                    TextOrientation = TextOrientation.Rotated90
                };
                area.AxisX.StripLines.Add(line);
            }
        }


        public void SetController(IAuroraMarvinController cont)
        {
            this.controller = cont;

            DataTable menuItemState = this.controller.GetMenuItemState();

            string[] menuItems =
            {
                "ShowAetherRiftGrowthRateReductionToolStripMenuItem",
                "ShowDamagedShipsToolStripMenuItem",
                "ShowLowCrewMoraleToolStripMenuItem",
                "ShowFreeConstructionFactoriesToolStripMenuItem",
                "ShowFreeOrdnanceFactoriesToolStripMenuItem",
                "ShowFreeFighterFactoriesToolStripMenuItem",
                "ShowObsoleteShipsToolStripMenuItem",
                "ShowUnusedTerraformersToolStripMenuItem",
                "ShowUnusedMinesToolStripMenuItem",
                "ShowShipsWithArmorDamageToolStripMenuItem",
                "ShowWrecksToolStripMenuItem",
                "ShowObsoleteTooledShipyardsToolStripMenuItem",
                "ShowMissingSectorCommandersToolStripMenuItem",
                "ShowShipWithoutMspToolStripMenuItem",
                "ShowShipWithLowMspToolStripMenuItem",
                "ShowMissingAdminCommandersToolStripMenuItem",
                "ShowCivMinesWithoutMassDriverDestinationToolStripMenuItem",
                "ShowTaxedCivMinesToolStripMenuItem",
                "ShowLowPopulationEfficienyToolStripMenuItem",
                "ShowSelfSustainingColonistDestinationToolStripMenuItem",
                "ShowFullTrainedShipsinTrainingFleetsToolStripMenuItem",
                "ShowLifePodsToolStripMenuItem",
                "ShowPopulationsWithoutGovernorToolStripMenuItem",
                "ShowOpenFireFCToolStripMenuItem",
                "ShowResearchFieldMismatchToolStripMenuItem",
                "ShowResearchWithoutResearchFacilityToolStripMenuItem",
                "ShowSystemsWithUnsurveyedBodiesToolStripMenuItem",
                "ShowIdleSoriumHarvestersToolStripMenuItem",
                "ShowIdleOrbitalMinersToolStripMenuItem",
                "ShowPopulationsWithGroundSurveyPotentialToolStripMenuItem",
                "ShowIdleGeosurveyFormationsToolStripMenuItem",
                "ShowDormantAncientConstructsToolStripMenuItem",
                "ShowNotActiveAncientConstructsToolStripMenuItem",
                "ShowHostileShipContactsToolStripMenuItem",
                "ShowHostileGroundForceContactsToolStripMenuItem",
                "ShowIdleShipyardsToolStripMenuItem",
                "ShowIdleGroundForceConstructionComplexToolStripMenuItem",
            };

            this.showMsgItem = new Dictionary<string, bool>();
            this.overviewMsgs = new Dictionary<string, string[]>();

            if (menuItemState.Rows.Count != 0)
            {
                DataRow[] techTreeMenuItem = menuItemState.Select("Name = 'ShowTechTreeToolStripMenuItem'");
                if (techTreeMenuItem.Length != 0)
                {
                    this.showTechTree = Convert.ToBoolean(Convert.ToInt16(techTreeMenuItem[0]["state"].ToString()));
                    this.ShowTechTreeToolStripMenuItem.Checked = this.showTechTree;
                }
                else
                {
                    this.showTechTree = true;
                }

                foreach (string item in menuItems)
                {
                    this.showMsgItem[item] = true;
                    DataRow[] state = menuItemState.Select("Name = '" + item + "'");
                    if (state.Length != 0)
                    {
                        ToolStripMenuItem menuItem = this.viewToolStripMenuItem.DropDownItems[item] as ToolStripMenuItem;
                        bool c = Convert.ToBoolean(Convert.ToInt16(state[0]["state"].ToString()));
                        menuItem.Checked = c;
                        this.showMsgItem[item] = c;
                    }
                }
            }
            else
            {
                this.showTechTree = this.ShowTechTreeToolStripMenuItem.Checked;
                foreach (string item in menuItems)
                {
                    this.showMsgItem[item] = true;
                }
            }
        }

        public void SetGames(DataTable games)
        {
            this.gameSelector.DisplayMember = "GameName";
            this.gameSelector.ValueMember = "GameID";
            this.gameSelector.DataSource = games;
            DataRow[] lastGame = games.Select("LastViewed = 1");
            this.gameSelector.SelectedValue = lastGame[0]["GameID"];
        }

        public void SetRaces(DataTable races)
        {
            this.raceSelector.DisplayMember = "RaceName";
            this.raceSelector.ValueMember = "RaceID";
            this.raceSelector.DataSource = races;
        }

        public void SetPopulations(DataTable populations)
        {
            this.populationViewer.DataSource = populations;
            this.PopulationComboBox1.DisplayMember = "PopName";
            this.PopulationComboBox1.ValueMember = "PopulationID";
            this.PopulationComboBox1.DataSource = populations;
        }

        public void SetShips(DataTable ships)
        {
            this.shipViewer.DataSource = ships;
        }

        public void AddOverviewMessage(string key, List<string> msg)
        {
            this.overviewMsgs.Add(key, msg.ToArray());
        }

        public void SetHullDescriptions(DataTable hullDescriptions)
        {
            this.hullSelector.DataSource = hullDescriptions;
        }

        public void SetDesignsForHull(DataTable shipDesigns)
        {
            this.shipDesignSelector.DataSource = null;
            this.shipDesignSelector.Items.Clear();
            this.shipDesignSelector.DisplayMember = "Name";
            this.shipDesignSelector.ValueMember = "Name";
            this.shipDesignSelector.DataSource = shipDesigns;
        }

        public void SetShipDesign(DataTable design)
        {
            this.shipDisplay.Text = design.Rows[0]["Design"].ToString();
        }

        public void SetNotes(DataTable notes)
        {
            const string SZ_RTF_TAG = "{\\rtf";
            string note = notes.Rows[0]["Note"].ToString();
            if (note.StartsWith(SZ_RTF_TAG))
            {
                MemoryStream stream = new MemoryStream(ASCIIEncoding.Default.GetBytes(note));
                this.notesBox.LoadFile(stream, RichTextBoxStreamType.RichText);
            }
            else
            {
                this.notesBox.Text = note;
            }
        }

        public void SetGameNotes(DataTable notes)
        {
            if (notes.Rows.Count == 0)
            {
                this.gameNotesBox.Clear();
            }
            else
            {
                string note = notes.Rows[0]["Note"].ToString();
                MemoryStream stream = new MemoryStream(ASCIIEncoding.Default.GetBytes(note));
                this.gameNotesBox.LoadFile(stream, RichTextBoxStreamType.RichText);
            }
        }

        public void SetGameTime(DateTime gameTime)
        {
            this.GameTimeLabel.Text = gameTime.ToString();
        }

        public void SetResources(DataTable ressources)
        {
            DataTable re = this.AdjustResourceDataTime(ressources);
            this.resourceChartData = re;
            this.ApplyMineralChartRange();
            this.ApplyStandardChart(this.chart2, re, this.fuelChartRangeComboBox, "Fuel");
            this.ApplyStandardChart(this.chart3, re, this.maintenanceChartRangeComboBox, "Maintenance supplies");
            this.ApplyStandardChart(this.chart4, re, this.populationChartRangeComboBox, "Population");

            Dictionary<string, double> minerals = new Dictionary<string, double>
            {
                { "Duranium", 0 },
                { "Neutronium", 0 },
                { "Corbomite", 0 },
                { "Tritanium", 0 },
                { "Boronide", 0 },
                { "Mercassium", 0 },
                { "Vendarite", 0 },
                { "Sorium", 0 },
                { "Uridium", 0 },
                { "Corundium", 0 },
                { "Gallicite", 0 },
                { "Total", 0 },
            };

            Dictionary<string, double> lastMinerals = new Dictionary<string, double>(minerals);

            this.mineralChangesGridView.Rows.Clear();
            this.mineralChangesGridView.ColumnCount = 13;
            this.mineralChangesGridView.Columns[0].HeaderText = "GameTime";

            for (int i = 0; i < minerals.Count; i++)
            {
                this.mineralChangesGridView.Columns[i + 1].HeaderText = minerals.Keys.ElementAt(i);
            }

            foreach (DataRow r in ressources.Rows)
            {
                string gameTime = r["GameTime"].ToString();
                DataGridViewRow row = new DataGridViewRow();
                row.CreateCells(this.mineralChangesGridView);
                row.Cells[0].Value = gameTime;

                double total = 0;
                int i = 1;

                foreach (var m in lastMinerals)
                {
                    double mt;
                    if (m.Key == "Total")
                    {
                        mt = total;
                    }
                    else
                    {
                        mt = double.Parse(r["SUM(" + m.Key + ")"].ToString());
                    }

                    minerals[m.Key] = mt;
                    total += mt;
                    row.Cells[i].Value = mt;
                    if (mt > lastMinerals[m.Key])
                    {
                        row.Cells[i].Style.BackColor = Color.Green;
                    }

                    if (mt < lastMinerals[m.Key])
                    {
                        row.Cells[i].Style.BackColor = Color.Red;
                    }

                    i++;
                }

                row.Cells[1].Value = total;

                lastMinerals = new Dictionary<string, double>(minerals);

                this.mineralChangesGridView.Rows.Add(row);
            }

            this.mineralChangesGridView.Sort(this.mineralChangesGridView.Columns[0], System.ComponentModel.ListSortDirection.Descending);
        }

        public void SetWealthPoints(DataTable dataTable)
        {
            this.wealthChartData = this.AdjustResourceDataTime(dataTable);
            this.ApplyStandardChart(this.chart5, this.wealthChartData, this.wealthChartRangeComboBox, "Wealth");
        }

        public void SetEventColours(DataTable events)
        {
            this.eventColourView.Rows.Clear();
            this.eventColourView.ColumnCount = 1;
            this.eventColourView.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (DataRow row in events.Rows)
            {
                DataGridViewRow r = new DataGridViewRow();
                r.CreateCells(this.eventColourView);
                r.Cells[0].Value = row["Description"];
                r.Cells[0].Style.BackColor = Color.FromArgb(int.Parse(row["AlertColour"].ToString()));
                r.Cells[0].Style.ForeColor = Color.FromArgb(int.Parse(row["TextColour"].ToString()));
                this.eventColourView.Rows.Add(r);
            }
        }

        public void ClearOverviewMessages()
        {
            this.overviewMsgs.Clear();
        }

        public void RefreshOverviewMessage()
        {
            this.overviewList.Items.Clear();
            foreach (string key in this.overviewMsgs.Keys)
            {
                if (this.showMsgItem[key])
                {
                    this.overviewList.Items.AddRange(this.overviewMsgs[key]);
                }
            }
        }

        public void SetFuelInTankers(DataTable fuel)
        {
            string litre = fuel.Rows[0]["FuelInTankers"].ToString();
            this.fuelTankerLabel.Text = "Fuel in tankers: " + litre + " litre";
        }

        public void SetGameStartTime(int startyear)
        {
            this.startYear = startyear;
            this.ResetMineralChartXAxis();
            this.ResetChartXAxis(this.chart2);
            this.ResetChartXAxis(this.chart3);
            this.ResetChartXAxis(this.chart4);
            this.ResetChartXAxis(this.chart5);
            this.ResetChartXAxis(this.chart6);
            this.DisplayStartingYearWorkaroundWarning(false);
        }

        private void ResetChartXAxis(Chart chart)
        {
            chart.ChartAreas[0].AxisX.Minimum = double.NaN;
            chart.ChartAreas[0].AxisX.Maximum = double.NaN;
            chart.ChartAreas[0].AxisX.LabelStyle.Format = "0";
            chart.ChartAreas[0].AxisX.Interval = 0;
            chart.ChartAreas[0].AxisX.CustomLabels.Clear();
            chart.ChartAreas[0].AxisX.Title = "Save / data point";
        }

        private void ResetMineralChartXAxis()
        {
            this.ResetChartXAxis(this.chart1);
        }

        private void GameSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.gameSelector.DisplayMember = "GameName";
            this.gameSelector.ValueMember = "GameID";
            this.controller.SetGame(int.Parse(this.gameSelector.SelectedValue.ToString()));
            this.overviewList.Items.Clear();
            this.controller.GetRaces();
        }

        private void RaceSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.raceSelector.DisplayMember = "RaceName";
            this.raceSelector.ValueMember = "RaceID";
            this.controller.SetRace(int.Parse(this.raceSelector.SelectedValue.ToString()));
            this.overviewList.Items.Clear();
            this.controller.GetData();
        }

        private void FileButton_Click(object sender, EventArgs e)
        {
            DialogResult result = this.openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                string dbfile = this.openFileDialog1.FileName;
                this.overviewList.Items.Clear();
                this.controller.SetDatabaseFile(dbfile);
                this.controller.GetGames();

                this.RenderTechTree();

                this.tabControl1.Visible = true;

                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;

                this.fileSystemWatcher1.Path = Path.GetDirectoryName(dbfile);
                this.fileSystemWatcher1.Changed += this.FileSystemWatcher1_Changed;
                this.fileSystemWatcher1.NotifyFilter = NotifyFilters.LastWrite;
                this.fileSystemWatcher1.Filter = "AuroraDB.db";
            }
        }

        private void RenderTechTree()
        {
            if (this.showTechTree)
            {
                if (this.gameSelector.Items.Count != 0)
                {
                    Microsoft.Msagl.Drawing.Graph graph = this.controller.GetTechTree();
                    TechTreeControl viewer = new TechTreeControl(graph);
                    viewer.Dock = System.Windows.Forms.DockStyle.Fill;
                    this.tabPage10.SuspendLayout();
                    this.tabPage10.Controls.Clear();
                    this.tabPage10.Controls.Add(viewer);
                    this.tabPage10.ResumeLayout();

                    if (!this.tabControl1.TabPages.Contains(this.tabPage10))
                    {
                        this.tabControl1.TabPages.Add(this.tabPage10);
                    }
                }
            }
            else
            {
                this.tabControl1.TabPages.Remove(this.tabPage10);
            }
        }

        private void HullSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.controller.GetDesignsForHull(int.Parse(this.hullSelector.SelectedValue.ToString()));
        }

        private void ShipDesignSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.shipDesignSelector.SelectedValue != null)
            {
                this.controller.GetShipDesign(this.shipDesignSelector.SelectedValue.ToString());
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            this.controller.SaveShipDesign(this.hullSelector.SelectedValue.ToString(), this.shipName.Text, this.shipDisplay.Text);
            this.shipDisplay.Text = string.Empty;
            this.controller.GetDesignsForHull(int.Parse(this.hullSelector.SelectedValue.ToString()));
        }

        private void NotesSaveButton_Click(object sender, EventArgs e)
        {
            this.controller.UpdateNotes(this.notesBox.Rtf);
        }

        private void GameNotesSaveButton_Click(object sender, EventArgs e)
        {
            this.controller.UpdateGameNotes(this.gameNotesBox.Rtf);
        }

        private void FileSystemWatcher1_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                this.fileSystemWatcher1.EnableRaisingEvents = false;
                if (this.controller.CheckForChangedDatabase())
                {
                    Console.WriteLine("Database {0} changed. Reloading data.", e.Name);
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                    this.overviewList.Items.Clear();
                    this.controller.SavePopulationResources();
                    this.controller.SaveWealthPoints();
                    this.controller.GetData();
                    // Re-render the Tech Tree after an Aurora save changes the database.
                    // This refreshes researched/currently-researched indicators from the new save.
                    this.RenderTechTree();
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                }
            }
            finally
            {
                this.fileSystemWatcher1.EnableRaisingEvents = true;
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ShowTechTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            this.controller.SetMenuItemState(this.ShowTechTreeToolStripMenuItem.Name, this.ShowTechTreeToolStripMenuItem.Checked);
            this.showTechTree = this.ShowTechTreeToolStripMenuItem.Checked;
            this.RenderTechTree();
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
        }

        private void ShowAetherRiftGrowthRateReductionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowAetherRiftGrowthRateReductionToolStripMenuItem.Name, this.ShowAetherRiftGrowthRateReductionToolStripMenuItem.Checked);
            this.showMsgItem["ShowAetherRiftGrowthRateReductionToolStripMenuItem"] = this.ShowAetherRiftGrowthRateReductionToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowDamagedShipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowDamagedShipsToolStripMenuItem.Name, this.ShowDamagedShipsToolStripMenuItem.Checked);
            this.showMsgItem["ShowDamagedShipsToolStripMenuItem"] = this.ShowDamagedShipsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowLowCrewMoraleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowLowCrewMoraleToolStripMenuItem.Name, this.ShowLowCrewMoraleToolStripMenuItem.Checked);
            this.showMsgItem["ShowLowCrewMoraleToolStripMenuItem"] = this.ShowLowCrewMoraleToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowFreeConstructionFactoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowFreeConstructionFactoriesToolStripMenuItem.Name, this.ShowFreeConstructionFactoriesToolStripMenuItem.Checked);
            this.showMsgItem["ShowFreeConstructionFactoriesToolStripMenuItem"] = this.ShowFreeConstructionFactoriesToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowFreeOrdnanceFactoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowFreeOrdnanceFactoriesToolStripMenuItem.Name, this.ShowFreeOrdnanceFactoriesToolStripMenuItem.Checked);
            this.showMsgItem["ShowFreeOrdnanceFactoriesToolStripMenuItem"] = this.ShowFreeOrdnanceFactoriesToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowFreeFighterFactoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowFreeFighterFactoriesToolStripMenuItem.Name, this.ShowFreeFighterFactoriesToolStripMenuItem.Checked);
            this.showMsgItem["ShowFreeFighterFactoriesToolStripMenuItem"] = this.ShowFreeFighterFactoriesToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowObsoleteShipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowObsoleteShipsToolStripMenuItem.Name, this.ShowObsoleteShipsToolStripMenuItem.Checked);
            this.showMsgItem["ShowObsoleteShipsToolStripMenuItem"] = this.ShowObsoleteShipsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowUnusedTerraformersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowUnusedTerraformersToolStripMenuItem.Name, this.ShowUnusedTerraformersToolStripMenuItem.Checked);
            this.showMsgItem["ShowUnusedTerraformersToolStripMenuItem"] = this.ShowUnusedTerraformersToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowUnusedMinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowUnusedMinesToolStripMenuItem.Name, this.ShowUnusedMinesToolStripMenuItem.Checked);
            this.showMsgItem["ShowUnusedMinesToolStripMenuItem"] = this.ShowUnusedMinesToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowShipsWithArmorDamageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowShipsWithArmorDamageToolStripMenuItem.Name, this.ShowShipsWithArmorDamageToolStripMenuItem.Checked);
            this.showMsgItem["ShowShipsWithArmorDamageToolStripMenuItem"] = this.ShowShipsWithArmorDamageToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowWrecksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowWrecksToolStripMenuItem.Name, this.ShowWrecksToolStripMenuItem.Checked);
            this.showMsgItem["ShowWrecksToolStripMenuItem"] = this.ShowWrecksToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowObsoleteTooledShipyardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowObsoleteTooledShipyardsToolStripMenuItem.Name, this.ShowObsoleteTooledShipyardsToolStripMenuItem.Checked);
            this.showMsgItem["ShowObsoleteTooledShipyardsToolStripMenuItem"] = this.ShowObsoleteTooledShipyardsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowMissingSectorCommandersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowMissingSectorCommandersToolStripMenuItem.Name, this.ShowMissingSectorCommandersToolStripMenuItem.Checked);
            this.showMsgItem["ShowMissingSectorCommandersToolStripMenuItem"] = this.ShowMissingSectorCommandersToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowShipWithoutMspToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowShipWithoutMspToolStripMenuItem.Name, this.ShowShipWithoutMspToolStripMenuItem.Checked);
            this.showMsgItem["ShowShipWithoutMspToolStripMenuItem"] = this.ShowShipWithoutMspToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowShipWithLowMspToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowShipWithLowMspToolStripMenuItem.Name, this.ShowShipWithLowMspToolStripMenuItem.Checked);
            this.showMsgItem["ShowShipWithLowMspToolStripMenuItem"] = this.ShowShipWithLowMspToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowMissingAdminCommandersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowMissingAdminCommandersToolStripMenuItem.Name, this.ShowMissingAdminCommandersToolStripMenuItem.Checked);
            this.showMsgItem["ShowMissingAdminCommandersToolStripMenuItem"] = this.ShowMissingAdminCommandersToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowCivMinesWithoutMassDriverDestinationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowCivMinesWithoutMassDriverDestinationToolStripMenuItem.Name, this.ShowCivMinesWithoutMassDriverDestinationToolStripMenuItem.Checked);
            this.showMsgItem["ShowCivMinesWithoutMassDriverDestinationToolStripMenuItem"] = this.ShowCivMinesWithoutMassDriverDestinationToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowTaxedCivMinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowTaxedCivMinesToolStripMenuItem.Name, this.ShowTaxedCivMinesToolStripMenuItem.Checked);
            this.showMsgItem["ShowTaxedCivMinesToolStripMenuItem"] = this.ShowTaxedCivMinesToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowLowPopulationEfficienyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowLowPopulationEfficienyToolStripMenuItem.Name, this.ShowLowPopulationEfficienyToolStripMenuItem.Checked);
            this.showMsgItem["ShowLowPopulationEfficienyToolStripMenuItem"] = this.ShowLowPopulationEfficienyToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowSelfSustainingColonistDestinationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowSelfSustainingColonistDestinationToolStripMenuItem.Name, this.ShowSelfSustainingColonistDestinationToolStripMenuItem.Checked);
            this.showMsgItem["ShowSelfSustainingColonistDestinationToolStripMenuItem"] = this.ShowSelfSustainingColonistDestinationToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowFullTrainedShipsinTrainingFleetsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowFullTrainedShipsinTrainingFleetsToolStripMenuItem.Name, this.ShowFullTrainedShipsinTrainingFleetsToolStripMenuItem.Checked);
            this.showMsgItem["ShowFullTrainedShipsinTrainingFleetsToolStripMenuItem"] = this.ShowFullTrainedShipsinTrainingFleetsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowLifePodsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowLifePodsToolStripMenuItem.Name, this.ShowLifePodsToolStripMenuItem.Checked);
            this.showMsgItem["ShowLifePodsToolStripMenuItem"] = this.ShowLifePodsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowPopulationsWithoutGovernorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowPopulationsWithoutGovernorToolStripMenuItem.Name, this.ShowPopulationsWithoutGovernorToolStripMenuItem.Checked);
            this.showMsgItem["ShowPopulationsWithoutGovernorToolStripMenuItem"] = this.ShowPopulationsWithoutGovernorToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowOpenFireFCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowOpenFireFCToolStripMenuItem.Name, this.ShowOpenFireFCToolStripMenuItem.Checked);
            this.showMsgItem["ShowOpenFireFCToolStripMenuItem"] = this.ShowOpenFireFCToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowResearchFieldMismatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowResearchFieldMismatchToolStripMenuItem.Name, this.ShowResearchFieldMismatchToolStripMenuItem.Checked);
            this.showMsgItem["ShowResearchFieldMismatchToolStripMenuItem"] = this.ShowResearchFieldMismatchToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowResearchWithoutResearchFacilityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowResearchWithoutResearchFacilityToolStripMenuItem.Name, this.ShowResearchWithoutResearchFacilityToolStripMenuItem.Checked);
            this.showMsgItem["ShowResearchWithoutResearchFacilityToolStripMenuItem"] = this.ShowResearchWithoutResearchFacilityToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowSystemsWithUnsurveyedBodiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowSystemsWithUnsurveyedBodiesToolStripMenuItem.Name, this.ShowSystemsWithUnsurveyedBodiesToolStripMenuItem.Checked);
            this.showMsgItem["ShowSystemsWithUnsurveyedBodiesToolStripMenuItem"] = this.ShowSystemsWithUnsurveyedBodiesToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowIdleSoriumHarvestersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowIdleSoriumHarvestersToolStripMenuItem.Name, this.ShowIdleSoriumHarvestersToolStripMenuItem.Checked);
            this.showMsgItem["ShowIdleSoriumHarvestersToolStripMenuItem"] = this.ShowIdleSoriumHarvestersToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowIdleOrbitalMinersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowIdleOrbitalMinersToolStripMenuItem.Name, this.ShowIdleOrbitalMinersToolStripMenuItem.Checked);
            this.showMsgItem["ShowIdleOrbitalMinersToolStripMenuItem"] = this.ShowIdleOrbitalMinersToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowPopulationsWithGroundSurveyPotentialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowPopulationsWithGroundSurveyPotentialToolStripMenuItem.Name, this.ShowPopulationsWithGroundSurveyPotentialToolStripMenuItem.Checked);
            this.showMsgItem["ShowPopulationsWithGroundSurveyPotentialToolStripMenuItem"] = this.ShowPopulationsWithGroundSurveyPotentialToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowIdleGeosurveyFormationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowIdleGeosurveyFormationsToolStripMenuItem.Name, this.ShowIdleGeosurveyFormationsToolStripMenuItem.Checked);
            this.showMsgItem["ShowIdleGeosurveyFormationsToolStripMenuItem"] = this.ShowIdleGeosurveyFormationsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowDormantAncientConstructsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowDormantAncientConstructsToolStripMenuItem.Name, this.ShowDormantAncientConstructsToolStripMenuItem.Checked);
            this.showMsgItem["ShowDormantAncientConstructsToolStripMenuItem"] = this.ShowDormantAncientConstructsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowNotActiveAncientConstructsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowNotActiveAncientConstructsToolStripMenuItem.Name, this.ShowNotActiveAncientConstructsToolStripMenuItem.Checked);
            this.showMsgItem["ShowNotActiveAncientConstructsToolStripMenuItem"] = this.ShowNotActiveAncientConstructsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowHostileShipContactsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowHostileShipContactsToolStripMenuItem.Name, this.ShowHostileShipContactsToolStripMenuItem.Checked);
            this.showMsgItem["ShowHostileShipContactsToolStripMenuItem"] = this.ShowHostileShipContactsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowHostileGroundForceContactsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowHostileGroundForceContactsToolStripMenuItem.Name, this.ShowHostileGroundForceContactsToolStripMenuItem.Checked);
            this.showMsgItem["ShowHostileGroundForceContactsToolStripMenuItem"] = this.ShowHostileGroundForceContactsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowIdleShipyardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowIdleShipyardsToolStripMenuItem.Name, this.ShowIdleShipyardsToolStripMenuItem.Checked);
            this.showMsgItem["ShowIdleShipyardsToolStripMenuItem"] = this.ShowIdleShipyardsToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private void ShowIdleGroundForceConstructionComplexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.controller.SetMenuItemState(this.ShowIdleGroundForceConstructionComplexToolStripMenuItem.Name, this.ShowIdleGroundForceConstructionComplexToolStripMenuItem.Checked);
            this.showMsgItem["ShowIdleGroundForceConstructionComplexToolStripMenuItem"] = this.ShowIdleGroundForceConstructionComplexToolStripMenuItem.Checked;
            this.RefreshOverviewMessage();
        }

        private DataTable AdjustResourceDataTime(DataTable res)
        {
            if (!res.Columns.Contains("DateTime"))
            {
                res.Columns.Add("DateTime", typeof(DateTime));
            }

            if (!res.Columns.Contains("GameYear"))
            {
                res.Columns.Add("GameYear", typeof(int));
            }

            if (!res.Columns.Contains("DataPointIndex"))
            {
                res.Columns.Add("DataPointIndex", typeof(int));
            }

            if (!res.Columns.Contains("GameTimeSeconds"))
            {
                res.Columns.Add("GameTimeSeconds", typeof(double));
            }

            // DateTime supports Aurora start years from 1 onward. We no longer use DateTime/OADate
            // as the chart X value, so there is no need to remap years below 100 to year 100.
            int displayStartYear = this.startYear;
            DateTime dtDateTime = new DateTime(displayStartYear, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);

            int dataPointIndex = 1;
            foreach (DataRow row in res.Rows)
            {
                long gameTimeSeconds = long.Parse(row["GameTime"].ToString());
                DateTime dt = dtDateTime.AddSeconds(gameTimeSeconds).ToLocalTime();
                row["DateTime"] = dt;
                row["GameYear"] = dt.Year;
                row["DataPointIndex"] = dataPointIndex++;
                row["GameTimeSeconds"] = (double)gameTimeSeconds;
            }

            return res;
        }

        private void OpenMineralCompareDialog(Chart chart)
        {
            string[] minerals = { "Duranium", "Neutronium", "Corbomite", "Tritanium", "Boronide", "Mercassium", "Vendarite", "Sorium", "Uridium", "Corundium", "Gallicite" };
            using (Form dialog = new Form { Text = "Compare minerals", StartPosition = FormStartPosition.CenterParent, Size = new Size(340, 420), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            using (CheckedListBox list = new CheckedListBox { Location = new Point(12, 12), Size = new Size(300, 300), CheckOnClick = true })
            {
                HashSet<string> selected;
                if (!this.customMineralSelections.TryGetValue(chart, out selected)) selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string mineral in minerals) list.Items.Add(mineral, selected.Contains(mineral));
                Button ok = new Button { Text = "Apply", DialogResult = DialogResult.OK, Location = new Point(150, 330), Width = 75 };
                Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(237, 330), Width = 75 };
                dialog.Controls.Add(list); dialog.Controls.Add(ok); dialog.Controls.Add(cancel); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                selected = new HashSet<string>(list.CheckedItems.Cast<string>(), StringComparer.OrdinalIgnoreCase);
                if (selected.Count == 0) return;
                this.customMineralSelections[chart] = selected;
                ComboBox display = chart == this.chart1 ? this.mineralChartDisplayComboBox : this.populationMineralChartDisplayComboBox;
                int customIndex = display.Items.IndexOf("Custom selection");
                if (customIndex < 0) { display.Items.Add("Custom selection"); customIndex = display.Items.Count - 1; }
                display.SelectedIndex = customIndex;
            }
        }

        private void ApplyMineralChartRange()
        {
            if (this.resourceChartData == null)
            {
                return;
            }

            DataTable chartData = this.GetChartRangeData(this.resourceChartData, this.mineralChartRangeComboBox);
            string selectedMineral = this.mineralChartDisplayComboBox.SelectedItem == null
                ? "All minerals"
                : this.mineralChartDisplayComboBox.SelectedItem.ToString();

            foreach (Series series in this.chart1.Series)
            {
                series.XValueMember = this.IsGameTimeXAxis(this.mineralChartXAxisComboBox) ? "GameTimeSeconds" : "DataPointIndex";
                series.XValueType = this.IsGameTimeXAxis(this.mineralChartXAxisComboBox) ? ChartValueType.Double : ChartValueType.Int32;
                bool visible = selectedMineral == "All minerals" || series.Name == selectedMineral || (selectedMineral == "Custom selection" && this.customMineralSelections.ContainsKey(this.chart1) && this.customMineralSelections[this.chart1].Contains(series.Name));
                series.Enabled = visible;
                series.BorderWidth = visible ? 3 : 2;
                series.MarkerStyle = visible ? MarkerStyle.Circle : MarkerStyle.None;
                series.MarkerSize = visible ? 6 : 0;
            }

            this.RemoveAnalysisSeries(this.chart1);
            this.chart1.DataSource = chartData;
            this.chart1.DataBind();

            foreach (Series series in this.chart1.Series)
            {
                if (!series.Enabled)
                {
                    continue;
                }

                int pointCount = Math.Min(series.Points.Count, chartData.Rows.Count);
                for (int i = 0; i < pointCount; i++)
                {
                    DataRow row = chartData.Rows[i];
                    series.Points[i].ToolTip = this.BuildPointToolTip(series.Name, chartData, i, row);
                }
            }

            this.ApplyTrendIndicators(this.chart1);
            this.ApplyAnalysisOverlay(this.chart1, chartData);
            this.ConfigureChartXAxis(this.chart1, chartData, this.mineralChartXAxisComboBox);
            this.ApplyChartEventMarkers(this.chart1);
            this.chart1.ChartAreas[0].AxisY.Minimum = double.NaN;
            this.chart1.ChartAreas[0].AxisY.Maximum = double.NaN;
            this.chart1.ChartAreas[0].RecalculateAxesScale();
            this.chart1.Update();
            ThemeManager.ApplyChartTheme(this.chart1, ThemeManager.CurrentTheme == MarvinTheme.Dark);
        }

        private void SetupStandardChartControls()
        {
            this.CreateChartRangeControl(this.tabPage7, this.chart2, 8, out this.fuelChartRangeComboBox, out this.fuelChartXAxisComboBox);
            this.CreateChartRangeControl(this.tabPage8, this.chart3, 8, out this.maintenanceChartRangeComboBox, out this.maintenanceChartXAxisComboBox);
            this.CreateChartRangeControl(this.tabPage9, this.chart4, 8, out this.populationChartRangeComboBox, out this.populationChartXAxisComboBox);
            this.CreateChartRangeControl(this.tabPage11, this.chart5, 8, out this.wealthChartRangeComboBox, out this.wealthChartXAxisComboBox);
            this.CreateChartRangeControl(this.tabPage17, this.chart6, 195, out this.populationMineralChartRangeComboBox, out this.populationMineralChartXAxisComboBox);

            this.populationMineralChartDisplayComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(445, 4),
                Size = new Size(105, 21),
                Name = "populationMineralChartDisplayComboBox"
            };
            this.populationMineralChartDisplayComboBox.Items.Add("All minerals");
            foreach (string mineral in new[] { "Duranium", "Neutronium", "Corbomite", "Tritanium", "Boronide", "Mercassium", "Vendarite", "Sorium", "Uridium", "Corundium", "Gallicite" })
            {
                this.populationMineralChartDisplayComboBox.Items.Add(mineral);
            }
            this.populationMineralChartDisplayComboBox.SelectedIndex = 0;
            this.populationMineralChartDisplayComboBox.SelectedIndexChanged += this.PopulationMineralChartDisplayComboBox_SelectedIndexChanged;
            this.tabPage17.Controls.Add(this.populationMineralChartDisplayComboBox);

            // The Per population tab already has the population selector on the left.
            // Keep the first-row controls separated, and put trend/overlay controls on row two.
            this.populationMineralChartRangeComboBox.Location = new Point(255, 4);
            Label populationMineralRangeLabel = this.tabPage17.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "Range:");
            if (populationMineralRangeLabel != null) populationMineralRangeLabel.Location = new Point(215, 7);
            this.populationMineralChartDisplayComboBox.Location = new Point(445, 4);
            Label populationMineralDisplayLabel = new Label
            {
                AutoSize = true,
                Location = new Point(395, 7),
                Text = "Display:",
                BackColor = SystemColors.Window
            };
            this.tabPage17.Controls.Add(populationMineralDisplayLabel);

            Label populationMineralXAxisLabel = this.tabPage17.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "X-axis:");
            if (populationMineralXAxisLabel != null)
            {
                populationMineralXAxisLabel.Location = new Point(625, 7);
                populationMineralXAxisLabel.BringToFront();
            }
            this.populationMineralChartXAxisComboBox.Location = new Point(668, 4);
            this.populationMineralChartXAxisComboBox.Size = new Size(95, 21);

            // Minerals overview controls.
            this.mineralChartDisplayLabel.Location = new Point(385, 8);
            this.mineralChartDisplayComboBox.Location = new Point(430, 4);
            this.mineralChartXAxisComboBox = this.CreateChartXAxisControl(this.tabPage15, 610, "mineralChartXAxisComboBox");

            int chart1Bottom = this.chart1.Bottom;
            this.chart1.Location = new Point(this.chart1.Left, 58);
            this.chart1.Height = Math.Max(100, chart1Bottom - 58);
            this.CreateChartAnalysisControls(this.tabPage15, this.chart1, 8, 31);
            this.CreateChartAnalysisControls(this.tabPage7, this.chart2, 8, 31);
            this.CreateChartAnalysisControls(this.tabPage8, this.chart3, 8, 31);
            this.CreateChartAnalysisControls(this.tabPage9, this.chart4, 8, 31);
            this.CreateChartAnalysisControls(this.tabPage11, this.chart5, 8, 31);
            this.CreateChartAnalysisControls(this.tabPage17, this.chart6, 8, 31);

            // Compare belongs on the second row so it never overlaps Display/X-axis controls.
            Button mineralCompareButton = new Button { Text = "Compare...", Location = new Point(690, 30), Size = new Size(78, 23), Name = "mineralCompareButton" };
            mineralCompareButton.Click += (s, e) => this.OpenMineralCompareDialog(this.chart1);
            this.tabPage15.Controls.Add(mineralCompareButton);

            Button populationMineralCompareButton = new Button { Text = "Compare...", Location = new Point(690, 30), Size = new Size(78, 23), Name = "populationMineralCompareButton" };
            populationMineralCompareButton.Click += (s, e) => this.OpenMineralCompareDialog(this.chart6);
            this.tabPage17.Controls.Add(populationMineralCompareButton);

            this.fuelChartRangeComboBox.SelectedIndex = 1;
            this.maintenanceChartRangeComboBox.SelectedIndex = 1;
            this.populationChartRangeComboBox.SelectedIndex = 1;
            this.wealthChartRangeComboBox.SelectedIndex = 1;
            this.populationMineralChartRangeComboBox.SelectedIndex = 1;
            this.mineralChartXAxisComboBox.SelectedIndex = 0;
        }

        private void CreateChartRangeControl(TabPage page, Chart chart, int x, out ComboBox rangeComboBox, out ComboBox xAxisComboBox)
        {
            Label label = new Label
            {
                AutoSize = true,
                Location = new Point(x, 7),
                Text = "Range:",
                BackColor = SystemColors.Window
            };
            rangeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(x + 42, 4),
                Size = new Size(130, 21),
                Name = page.Name + "ChartRangeComboBox"
            };
            rangeComboBox.Items.Add("All history");
            rangeComboBox.Items.Add("Last 20 points");
            rangeComboBox.Items.Add("Last 50 points");
            rangeComboBox.Items.Add("Last 100 points");
            rangeComboBox.SelectedIndexChanged += this.StandardChartRangeComboBox_SelectedIndexChanged;
            page.Controls.Add(label);
            page.Controls.Add(rangeComboBox);

            xAxisComboBox = this.CreateChartXAxisControl(page, x + 180, page.Name + "ChartXAxisComboBox");

            int chartBottom = chart.Bottom;
            chart.Location = new Point(chart.Left, 58);
            chart.Height = Math.Max(100, chartBottom - 58);
            chart.BringToFront();
            label.BringToFront();
            rangeComboBox.BringToFront();
            xAxisComboBox.BringToFront();
        }

        private void CreateChartAnalysisControls(TabPage page, Chart chart, int x, int y)
        {
            Label trendLabel = new Label
            {
                AutoSize = true,
                Location = new Point(x, y + 4),
                Text = "Trend:",
                BackColor = SystemColors.Window
            };
            ComboBox trendCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(x + 42, y),
                Size = new Size(130, 21),
                Name = page.Name + "TrendComboBox"
            };
            trendCombo.Items.Add("Off");
            trendCombo.Items.Add("Show trend");
            trendCombo.SelectedIndex = 0;
            trendCombo.SelectedIndexChanged += this.ChartAnalysisComboBox_SelectedIndexChanged;

            Label overlayLabel = new Label
            {
                AutoSize = true,
                Location = new Point(x + 185, y + 4),
                Text = "Overlay:",
                BackColor = SystemColors.Window
            };
            ComboBox overlayCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(x + 235, y),
                Size = new Size(180, 21),
                Name = page.Name + "OverlayComboBox"
            };
            overlayCombo.Items.Add("None");
            overlayCombo.Items.Add("Moving average (3)");
            overlayCombo.Items.Add("Moving average (5)");
            overlayCombo.Items.Add("Start value baseline");
            overlayCombo.Items.Add("Linear projection");
            overlayCombo.SelectedIndex = 0;
            overlayCombo.SelectedIndexChanged += this.ChartAnalysisComboBox_SelectedIndexChanged;

            page.Controls.Add(trendLabel);
            page.Controls.Add(trendCombo);
            page.Controls.Add(overlayLabel);
            page.Controls.Add(overlayCombo);

            Button resetZoom = new Button { Text = "Reset zoom", Location = new Point(x + 425, y - 1), Size = new Size(78, 23), Name = page.Name + "ResetZoomButton" };
            resetZoom.Click += (s, e) => this.ResetChartZoom(chart);
            Button export = new Button { Text = "Export", Location = new Point(x + 508, y - 1), Size = new Size(60, 23), Name = page.Name + "ExportButton" };
            export.Click += (s, e) => this.ExportChart(chart);
            Button eventButton = new Button { Text = "Event", Location = new Point(x + 573, y - 1), Size = new Size(55, 23), Name = page.Name + "EventButton" };
            eventButton.Click += (s, e) => this.AddChartEvent(chart);
            Button clearEventButton = new Button { Text = "Clear", Location = new Point(x + 633, y - 1), Size = new Size(50, 23), Name = page.Name + "ClearEventButton" };
            clearEventButton.Click += (s, e) => this.ClearChartEvents(chart);
            page.Controls.Add(resetZoom); page.Controls.Add(export); page.Controls.Add(eventButton); page.Controls.Add(clearEventButton);
            this.chartTrendComboBoxes[chart] = trendCombo;
            this.chartOverlayComboBoxes[chart] = overlayCombo;

            chart.BringToFront();
            trendLabel.BringToFront();
            trendCombo.BringToFront();
            overlayLabel.BringToFront();
            overlayCombo.BringToFront();
        }

        private ComboBox CreateChartXAxisControl(TabPage page, int x, string name)
        {
            Label label = new Label
            {
                AutoSize = true,
                Location = new Point(x, 7),
                Text = "X-axis:",
                BackColor = SystemColors.Window
            };
            ComboBox comboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(x + 48, 4),
                Size = new Size(115, 21),
                Name = name
            };
            comboBox.Items.Add("Relative");
            comboBox.Items.Add("Game time");
            comboBox.SelectedIndex = 0;
            comboBox.SelectedIndexChanged += this.ChartXAxisComboBox_SelectedIndexChanged;
            page.Controls.Add(label);
            page.Controls.Add(comboBox);
            label.BringToFront();
            comboBox.BringToFront();
            return comboBox;
        }

        private bool IsGameTimeXAxis(ComboBox comboBox)
        {
            return comboBox != null && comboBox.SelectedItem != null && comboBox.SelectedItem.ToString() == "Game time";
        }

        private ComboBox GetTrendComboBox(Chart chart)
        {
            ComboBox combo;
            return this.chartTrendComboBoxes.TryGetValue(chart, out combo) ? combo : null;
        }

        private ComboBox GetOverlayComboBox(Chart chart)
        {
            ComboBox combo;
            return this.chartOverlayComboBoxes.TryGetValue(chart, out combo) ? combo : null;
        }

        private void RemoveOverlaySeries(Chart chart)
        {
            foreach (Series series in chart.Series.Cast<Series>().Where(s => s.Name.StartsWith("__Overlay_", StringComparison.Ordinal)).ToList())
            {
                chart.Series.Remove(series);
            }
        }

        private void RemoveTrendSeries(Chart chart)
        {
            foreach (Series series in chart.Series.Cast<Series>().Where(s => s.Name.StartsWith("__Trend_", StringComparison.Ordinal)).ToList())
            {
                chart.Series.Remove(series);
            }
        }

        private void RemoveAnalysisSeries(Chart chart)
        {
            this.RemoveTrendSeries(chart);
            this.RemoveOverlaySeries(chart);
        }

        private string GetTrendSuffix(Series series)
        {
            if (series.Points.Count < 2) return " →";
            double first = double.NaN;
            double last = double.NaN;
            for (int i = 0; i < series.Points.Count; i++)
            {
                if (series.Points[i].YValues.Length > 0 && !double.IsNaN(series.Points[i].YValues[0]))
                {
                    first = series.Points[i].YValues[0];
                    break;
                }
            }
            for (int i = series.Points.Count - 1; i >= 0; i--)
            {
                if (series.Points[i].YValues.Length > 0 && !double.IsNaN(series.Points[i].YValues[0]))
                {
                    last = series.Points[i].YValues[0];
                    break;
                }
            }
            if (double.IsNaN(first) || double.IsNaN(last)) return " →";
            double delta = last - first;
            double tolerance = Math.Max(0.000001, Math.Abs(first) * 0.0001);
            if (delta > tolerance) return " ↑";
            if (delta < -tolerance) return " ↓";
            return " →";
        }

        private void ApplyTrendIndicators(Chart chart)
        {
            this.RemoveTrendSeries(chart);

            ComboBox trendCombo = this.GetTrendComboBox(chart);
            bool showTrend = trendCombo != null && trendCombo.SelectedItem != null && trendCombo.SelectedItem.ToString() == "Show trend";

            List<Series> baseSeriesList = chart.Series.Cast<Series>()
                .Where(s => !s.Name.StartsWith("__Overlay_", StringComparison.Ordinal) && !s.Name.StartsWith("__Trend_", StringComparison.Ordinal))
                .ToList();

            foreach (Series series in baseSeriesList)
            {
                series.LegendText = series.Name;
                for (int i = 0; i < series.Points.Count; i++)
                {
                    series.Points[i].MarkerStyle = MarkerStyle.Circle;
                    series.Points[i].MarkerSize = 6;
                }

                if (!showTrend || !series.Enabled || series.Points.Count < 2)
                {
                    continue;
                }

                string suffix = this.GetTrendSuffix(series);
                series.LegendText = series.Name + suffix;

                // Keep the original series visible as a thin guide, then draw each
                // interval separately so rising/falling sections can have different colours.
                series.BorderWidth = 1;

                for (int i = 1; i < series.Points.Count; i++)
                {
                    DataPoint previous = series.Points[i - 1];
                    DataPoint current = series.Points[i];
                    if (previous.YValues.Length == 0 || current.YValues.Length == 0) continue;

                    double y1 = previous.YValues[0];
                    double y2 = current.YValues[0];
                    if (double.IsNaN(y1) || double.IsNaN(y2) || double.IsInfinity(y1) || double.IsInfinity(y2)) continue;

                    Color segmentColor;
                    double tolerance = Math.Max(0.000001, Math.Max(Math.Abs(y1), Math.Abs(y2)) * 0.0001);
                    if (y2 > y1 + tolerance)
                    {
                        segmentColor = Color.ForestGreen;
                    }
                    else if (y2 < y1 - tolerance)
                    {
                        segmentColor = Color.Firebrick;
                    }
                    else
                    {
                        segmentColor = Color.DimGray;
                    }

                    Series segment = new Series("__Trend_" + series.Name + "_" + i)
                    {
                        ChartArea = series.ChartArea,
                        ChartType = SeriesChartType.Line,
                        BorderWidth = 4,
                        Color = segmentColor,
                        IsVisibleInLegend = false,
                        XValueType = series.XValueType
                    };
                    segment.Points.AddXY(previous.XValue, y1);
                    segment.Points.AddXY(current.XValue, y2);
                    chart.Series.Add(segment);
                }

                if (series.Points.Count > 0)
                {
                    MarkerStyle marker = suffix.Contains("↑") ? MarkerStyle.Triangle : (suffix.Contains("↓") ? MarkerStyle.Square : MarkerStyle.Diamond);
                    series.Points[series.Points.Count - 1].MarkerStyle = marker;
                    series.Points[series.Points.Count - 1].MarkerSize = 9;
                }
            }
        }

        private void ApplyAnalysisOverlay(Chart chart, DataTable chartData)
        {
            this.RemoveOverlaySeries(chart);
            ComboBox overlayCombo = this.GetOverlayComboBox(chart);
            if (overlayCombo == null || overlayCombo.SelectedItem == null || overlayCombo.SelectedItem.ToString() == "None" || chartData == null || chartData.Rows.Count == 0) return;

            string mode = overlayCombo.SelectedItem.ToString();
            int window = mode == "Moving average (3)" ? 3 : (mode == "Moving average (5)" ? 5 : 1);
            bool baseline = mode == "Start value baseline";
            bool projection = mode == "Linear projection";

            foreach (Series baseSeries in chart.Series.Cast<Series>()
                .Where(s => !s.Name.StartsWith("__Overlay_", StringComparison.Ordinal) && !s.Name.StartsWith("__Trend_", StringComparison.Ordinal) && s.Enabled)
                .ToList())
            {
                string column = baseSeries.YValueMembers;
                if (string.IsNullOrWhiteSpace(column) || !chartData.Columns.Contains(column)) continue;
                Series overlay = new Series("__Overlay_" + baseSeries.Name)
                {
                    ChartArea = baseSeries.ChartArea,
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 2,
                    BorderDashStyle = ChartDashStyle.Dash,
                    Color = Color.FromArgb(180, baseSeries.Color),
                    IsVisibleInLegend = true,
                    LegendText = mode == "Linear projection" ? baseSeries.Name + " projection" : baseSeries.Name + " " + mode,
                    XValueType = baseSeries.XValueType
                };

                if (baseline || !projection)
                {
                    double firstValue = double.NaN;
                    for (int i = 0; i < chartData.Rows.Count; i++)
                    {
                        double value;
                        if (!double.TryParse(chartData.Rows[i][column].ToString(), out value)) continue;
                        if (double.IsNaN(firstValue)) firstValue = value;
                        double overlayValue = baseline ? firstValue : this.GetMovingAverage(chartData, column, i, window);
                        if (double.IsNaN(overlayValue) || double.IsInfinity(overlayValue)) continue;
                        double x = Convert.ToDouble(chartData.Rows[i][baseSeries.XValueMember]);
                        overlay.Points.AddXY(x, overlayValue);
                    }
                }
                else
                {
                    this.AddLinearProjectionPoints(overlay, chartData, column, baseSeries.XValueMember);
                }
                if (overlay.Points.Count > 0) chart.Series.Add(overlay);
            }
        }

        private void AddLinearProjectionPoints(Series overlay, DataTable data, string column, string xColumn)
        {
            List<double> xs = new List<double>();
            List<double> ys = new List<double>();
            foreach (DataRow row in data.Rows)
            {
                double x, y;
                if (double.TryParse(row[xColumn].ToString(), out x) && double.TryParse(row[column].ToString(), out y)) { xs.Add(x); ys.Add(y); }
            }
            if (xs.Count < 2) return;
            double meanX = xs.Average(), meanY = ys.Average(), num = 0, den = 0;
            for (int i = 0; i < xs.Count; i++) { num += (xs[i] - meanX) * (ys[i] - meanY); den += (xs[i] - meanX) * (xs[i] - meanX); }
            if (Math.Abs(den) < 0.0000001) return;
            double slope = num / den, intercept = meanY - slope * meanX;
            double step = xs.Count > 1 ? Math.Abs(xs[xs.Count - 1] - xs[xs.Count - 2]) : 1;
            if (step <= 0) step = 1;
            double startX = xs[xs.Count - 1];
            overlay.Points.AddXY(startX, ys[ys.Count - 1]);
            for (int i = 1; i <= 3; i++) { double x = startX + step * i; overlay.Points.AddXY(x, intercept + slope * x); }
        }

        private string BuildPointToolTip(string seriesName, DataTable data, int index, DataRow row)
        {
            StringBuilder text = new StringBuilder();
            text.Append(seriesName).Append("\nSave / data point: ").Append(row["DataPointIndex"]);
            text.Append("\nGame date: ").Append(((DateTime)row["DateTime"]).ToString("dd/MM/yyyy HH:mm:ss"));
            string column = null;
            foreach (Series chartSeries in this.FindSeriesForDataColumn(seriesName)) { column = chartSeries.YValueMembers; break; }
            if (column != null && data.Columns.Contains(column))
            {
                double current;
                if (double.TryParse(row[column].ToString(), out current))
                {
                    text.Append("\nValue: ").Append(current.ToString("N2"));
                    if (index > 0)
                    {
                        double previous, previousTime, currentTime;
                        if (double.TryParse(data.Rows[index - 1][column].ToString(), out previous) && double.TryParse(data.Rows[index - 1]["GameTimeSeconds"].ToString(), out previousTime) && double.TryParse(row["GameTimeSeconds"].ToString(), out currentTime))
                        {
                            double days = Math.Abs(currentTime - previousTime) / 86400.0;
                            if (days > 0.000001)
                            {
                                double perDay = (current - previous) / days;
                                double perMonth = perDay * 30.4375;
                                text.Append("\nChange: ").Append((current - previous).ToString("+#,##0.00;-#,##0.00;0.00"));
                                text.Append("\nRate: ").Append(perDay.ToString("+#,##0.00;-#,##0.00;0.00")).Append(" / day");
                                text.Append("\nRate: ").Append(perMonth.ToString("+#,##0.00;-#,##0.00;0.00")).Append(" / month");
                            }
                        }
                    }
                }
            }
            return text.ToString();
        }

        private IEnumerable<Series> FindSeriesForDataColumn(string seriesName)
        {
            foreach (Chart chart in new[] { this.chart1, this.chart2, this.chart3, this.chart4, this.chart5, this.chart6 })
                foreach (Series series in chart.Series)
                    if (series.Name == seriesName) yield return series;
        }

        private double GetMovingAverage(DataTable data, string column, int endIndex, int window)
        {
            int start = Math.Max(0, endIndex - window + 1);
            double sum = 0;
            int count = 0;
            for (int i = start; i <= endIndex; i++)
            {
                double value;
                if (double.TryParse(data.Rows[i][column].ToString(), out value))
                {
                    sum += value;
                    count++;
                }
            }
            return count == 0 ? double.NaN : sum / count;
        }

        private void ApplyStandardChart(Chart chart, DataTable source, ComboBox rangeComboBox, string axisTitle)
        {
            if (source == null || rangeComboBox == null)
            {
                return;
            }

            ComboBox xAxisComboBox = this.GetXAxisComboBox(chart);
            DataTable chartData = this.GetChartRangeData(source, rangeComboBox);
            bool gameTime = this.IsGameTimeXAxis(xAxisComboBox);

            foreach (Series series in chart.Series)
            {
                series.XValueMember = gameTime ? "GameTimeSeconds" : "DataPointIndex";
                series.XValueType = gameTime ? ChartValueType.Double : ChartValueType.Int32;
                series.BorderWidth = 3;
                series.MarkerStyle = MarkerStyle.Circle;
                series.MarkerSize = 6;
            }

            this.RemoveAnalysisSeries(chart);
            chart.DataSource = chartData;
            chart.DataBind();

            foreach (Series series in chart.Series)
            {
                int count = Math.Min(series.Points.Count, chartData.Rows.Count);
                for (int i = 0; i < count; i++)
                {
                    DataRow row = chartData.Rows[i];
                    series.Points[i].ToolTip = this.BuildPointToolTip(series.Name, chartData, i, row);
                }
            }

            this.ApplyTrendIndicators(chart);
            this.ApplyAnalysisOverlay(chart, chartData);
            this.ConfigureChartXAxis(chart, chartData, xAxisComboBox);
            this.ApplyChartEventMarkers(chart);
            ChartArea area = chart.ChartAreas[0];
            area.AxisY.Minimum = double.NaN;
            area.AxisY.Maximum = double.NaN;
            area.RecalculateAxesScale();
            chart.Update();
            ThemeManager.ApplyChartTheme(chart, ThemeManager.CurrentTheme == MarvinTheme.Dark);
        }

        private ComboBox GetXAxisComboBox(Chart chart)
        {
            if (chart == this.chart2) return this.fuelChartXAxisComboBox;
            if (chart == this.chart3) return this.maintenanceChartXAxisComboBox;
            if (chart == this.chart4) return this.populationChartXAxisComboBox;
            if (chart == this.chart5) return this.wealthChartXAxisComboBox;
            if (chart == this.chart6) return this.populationMineralChartXAxisComboBox;
            return this.mineralChartXAxisComboBox;
        }

        private void ConfigureChartXAxis(Chart chart, DataTable chartData, ComboBox xAxisComboBox)
        {
            ChartArea area = chart.ChartAreas[0];
            bool gameTime = this.IsGameTimeXAxis(xAxisComboBox);

            area.AxisX.CustomLabels.Clear();
            area.AxisX.Minimum = double.NaN;
            area.AxisX.Maximum = double.NaN;
            area.AxisX.Interval = 0;
            area.AxisX.LabelStyle.Enabled = true;
            area.AxisX.IsMarginVisible = true;

            if (!gameTime)
            {
                area.AxisX.Title = "Save / data point";
                area.AxisX.LabelStyle.Format = "0";

                int count = chartData.Rows.Count;
                int step = count <= 20 ? 1 : Math.Max(1, (int)Math.Ceiling(count / 12.0));
                for (int i = 0; i < count; i += step)
                {
                    DataRow row = chartData.Rows[i];
                    double x = Convert.ToDouble(row["DataPointIndex"]);
                    string label = row["DataPointIndex"] + "\n" + ((DateTime)row["DateTime"]).ToString("dd/MM/yyyy");
                    area.AxisX.CustomLabels.Add(new CustomLabel(x - 0.5, x + 0.5, label, 0, LabelMarkStyle.None));
                }
            }
            else
            {
                area.AxisX.Title = "Game time";
                area.AxisX.LabelStyle.Enabled = true;
                area.AxisX.LabelStyle.Format = "";

                int count = chartData.Rows.Count;
                int step = count <= 20 ? 1 : Math.Max(1, (int)Math.Ceiling(count / 12.0));
                double halfWidth = this.GetGameTimeLabelHalfWidth(chartData);
                for (int i = 0; i < count; i += step)
                {
                    DataRow row = chartData.Rows[i];
                    double x = Convert.ToDouble(row["GameTimeSeconds"]);
                    string label = row["DataPointIndex"] + "\n" + ((DateTime)row["DateTime"]).ToString("dd/MM/yyyy");
                    area.AxisX.CustomLabels.Add(new CustomLabel(x - halfWidth, x + halfWidth, label, 0, LabelMarkStyle.None));
                }
            }

            if (chartData.Rows.Count > 0)
            {
                DataRow first = chartData.Rows[0];
                DataRow last = chartData.Rows[chartData.Rows.Count - 1];
                if (gameTime)
                {
                    double firstX = Convert.ToDouble(first["GameTimeSeconds"]);
                    double lastX = Convert.ToDouble(last["GameTimeSeconds"]);
                    double overlayMax = chart.Series.Cast<Series>().Where(s => s.Name.StartsWith("__Overlay_", StringComparison.Ordinal) && s.Points.Count > 0).SelectMany(s => s.Points.Select(p => p.XValue)).DefaultIfEmpty(lastX).Max();
                    lastX = Math.Max(lastX, overlayMax);
                    if (firstX == lastX)
                    {
                        area.AxisX.Minimum = firstX - 1;
                        area.AxisX.Maximum = lastX + 1;
                    }
                    else
                    {
                        double padding = Math.Max(1.0, (lastX - firstX) * 0.01);
                        area.AxisX.Minimum = firstX - padding;
                        area.AxisX.Maximum = lastX + padding;
                    }
                }
                else
                {
                    double firstX = Convert.ToDouble(first["DataPointIndex"]);
                    double lastX = Convert.ToDouble(last["DataPointIndex"]);
                    double overlayMax = chart.Series.Cast<Series>().Where(s => s.Name.StartsWith("__Overlay_", StringComparison.Ordinal) && s.Points.Count > 0).SelectMany(s => s.Points.Select(p => p.XValue)).DefaultIfEmpty(lastX).Max();
                    lastX = Math.Max(lastX, overlayMax);
                    if (firstX == lastX)
                    {
                        area.AxisX.Minimum = firstX - 0.5;
                        area.AxisX.Maximum = lastX + 0.5;
                    }
                    else
                    {
                        area.AxisX.Minimum = firstX - 0.5;
                        area.AxisX.Maximum = lastX + 0.5;
                    }
                }
            }
        }

        private double GetGameTimeLabelHalfWidth(DataTable chartData)
        {
            if (chartData.Rows.Count < 2)
            {
                return 1.0;
            }

            double minGap = double.MaxValue;
            double previous = Convert.ToDouble(chartData.Rows[0]["GameTimeSeconds"]);
            for (int i = 1; i < chartData.Rows.Count; i++)
            {
                double current = Convert.ToDouble(chartData.Rows[i]["GameTimeSeconds"]);
                double gap = Math.Abs(current - previous);
                if (gap > 0 && gap < minGap)
                {
                    minGap = gap;
                }
                previous = current;
            }

            if (minGap == double.MaxValue)
            {
                return 1.0;
            }

            return Math.Max(1.0, minGap * 0.35);
        }

        private DataTable GetChartRangeData(DataTable source, ComboBox rangeComboBox)
        {
            int pointLimit = 0;
            string selected = rangeComboBox.SelectedItem == null ? "All history" : rangeComboBox.SelectedItem.ToString();
            if (selected.StartsWith("Last "))
            {
                int.TryParse(selected.Substring(5).Split(' ')[0], out pointLimit);
            }
            if (pointLimit <= 0 || source.Rows.Count <= pointLimit)
            {
                return source;
            }

            DataTable result = source.Clone();
            foreach (DataRow row in source.AsEnumerable().OrderBy(r => Convert.ToInt64(r["GameTime"])).Skip(source.Rows.Count - pointLimit))
            {
                result.ImportRow(row);
            }
            return result;
        }

        private void ChartAnalysisComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender == null) return;
            foreach (KeyValuePair<Chart, ComboBox> item in this.chartTrendComboBoxes)
            {
                if (item.Value == sender)
                {
                    this.RefreshChartForAnalysis(item.Key);
                    return;
                }
            }
            foreach (KeyValuePair<Chart, ComboBox> item in this.chartOverlayComboBoxes)
            {
                if (item.Value == sender)
                {
                    this.RefreshChartForAnalysis(item.Key);
                    return;
                }
            }
        }

        private void RefreshChartForAnalysis(Chart chart)
        {
            if (chart == this.chart1) this.ApplyMineralChartRange();
            else if (chart == this.chart2 && this.resourceChartData != null) this.ApplyStandardChart(this.chart2, this.resourceChartData, this.fuelChartRangeComboBox, "Fuel");
            else if (chart == this.chart3 && this.resourceChartData != null) this.ApplyStandardChart(this.chart3, this.resourceChartData, this.maintenanceChartRangeComboBox, "Maintenance supplies");
            else if (chart == this.chart4 && this.resourceChartData != null) this.ApplyStandardChart(this.chart4, this.resourceChartData, this.populationChartRangeComboBox, "Population");
            else if (chart == this.chart5 && this.wealthChartData != null) this.ApplyStandardChart(this.chart5, this.wealthChartData, this.wealthChartRangeComboBox, "Wealth");
            else if (chart == this.chart6) this.ApplyPopulationMineralChart();
            ThemeManager.ApplyChartTheme(chart, ThemeManager.CurrentTheme == MarvinTheme.Dark);
        }

        private void StandardChartRangeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender == this.fuelChartRangeComboBox && this.resourceChartData != null) this.ApplyStandardChart(this.chart2, this.resourceChartData, this.fuelChartRangeComboBox, "Fuel");
            else if (sender == this.maintenanceChartRangeComboBox && this.resourceChartData != null) this.ApplyStandardChart(this.chart3, this.resourceChartData, this.maintenanceChartRangeComboBox, "Maintenance supplies");
            else if (sender == this.populationChartRangeComboBox && this.resourceChartData != null) this.ApplyStandardChart(this.chart4, this.resourceChartData, this.populationChartRangeComboBox, "Population");
            else if (sender == this.wealthChartRangeComboBox && this.wealthChartData != null) this.ApplyStandardChart(this.chart5, this.wealthChartData, this.wealthChartRangeComboBox, "Wealth");
            else if (sender == this.populationMineralChartRangeComboBox) this.ApplyPopulationMineralChart();
        }

        private void ChartXAxisComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender == this.mineralChartXAxisComboBox) this.ApplyMineralChartRange();
            else if (sender == this.fuelChartXAxisComboBox && this.resourceChartData != null) this.ApplyStandardChart(this.chart2, this.resourceChartData, this.fuelChartRangeComboBox, "Fuel");
            else if (sender == this.maintenanceChartXAxisComboBox && this.resourceChartData != null) this.ApplyStandardChart(this.chart3, this.resourceChartData, this.maintenanceChartRangeComboBox, "Maintenance supplies");
            else if (sender == this.populationChartXAxisComboBox && this.resourceChartData != null) this.ApplyStandardChart(this.chart4, this.resourceChartData, this.populationChartRangeComboBox, "Population");
            else if (sender == this.wealthChartXAxisComboBox && this.wealthChartData != null) this.ApplyStandardChart(this.chart5, this.wealthChartData, this.wealthChartRangeComboBox, "Wealth");
            else if (sender == this.populationMineralChartXAxisComboBox) this.ApplyPopulationMineralChart();
        }

        private void ApplyPopulationMineralChart()
        {
            if (this.populationMineralChartData == null || this.populationMineralChartRangeComboBox == null) return;
            DataTable chartData = this.GetChartRangeData(this.populationMineralChartData, this.populationMineralChartRangeComboBox);
            string selected = this.populationMineralChartDisplayComboBox.SelectedItem == null ? "All minerals" : this.populationMineralChartDisplayComboBox.SelectedItem.ToString();
            bool gameTime = this.IsGameTimeXAxis(this.populationMineralChartXAxisComboBox);

            foreach (Series series in this.chart6.Series)
            {
                series.XValueMember = gameTime ? "GameTimeSeconds" : "DataPointIndex";
                series.XValueType = gameTime ? ChartValueType.Double : ChartValueType.Int32;
                bool visible = selected == "All minerals" || selected == series.Name || (selected == "Custom selection" && this.customMineralSelections.ContainsKey(this.chart6) && this.customMineralSelections[this.chart6].Contains(series.Name));
                series.Enabled = visible;
                series.BorderWidth = visible ? 3 : 2;
                series.MarkerStyle = visible ? MarkerStyle.Circle : MarkerStyle.None;
                series.MarkerSize = visible ? 6 : 0;
            }

            this.RemoveAnalysisSeries(this.chart6);
            this.chart6.DataSource = chartData;
            this.chart6.DataBind();
            foreach (Series series in this.chart6.Series)
            {
                if (!series.Enabled) continue;
                int count = Math.Min(series.Points.Count, chartData.Rows.Count);
                for (int i = 0; i < count; i++)
                {
                    DataRow row = chartData.Rows[i];
                    series.Points[i].ToolTip = this.BuildPointToolTip(series.Name, chartData, i, row);
                }
            }
            this.ApplyTrendIndicators(this.chart6);
            this.ApplyAnalysisOverlay(this.chart6, chartData);
            this.ConfigureChartXAxis(this.chart6, chartData, this.populationMineralChartXAxisComboBox);
            this.ApplyChartEventMarkers(this.chart6);
            ChartArea area = this.chart6.ChartAreas[0];
            area.AxisY.Minimum = double.NaN;
            area.AxisY.Maximum = double.NaN;
            area.RecalculateAxesScale();
            this.chart6.Update();
            ThemeManager.ApplyChartTheme(this.chart6, ThemeManager.CurrentTheme == MarvinTheme.Dark);
        }

        private void PopulationMineralChartDisplayComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ApplyPopulationMineralChart();
        }

        private void MineralChartRangeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ApplyMineralChartRange();
        }

        private void MineralChartDisplayComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ApplyMineralChartRange();
        }

        private void DisplayStartingYearWorkaroundWarning(bool enabled)
        {
            this.MineralPopulationDateWarningLabel.Visible = enabled;
            this.FuelDateWarningLabel.Visible = enabled;
            this.MaintenanceDateWarningLabel.Visible = enabled;
            this.PopulationDateWarningLabel.Visible = enabled;
            this.WealthDateWarningLabel.Visible = enabled;
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
$@"Aurora MarvinS v{MARVINVERSION} for Aurora 4x C# {AURORAVERSION}

Questions, bugs, suggestions?
Please head over to the Aurora 4x forum.",
"AuroraMarvin",
MessageBoxButtons.OK,
MessageBoxIcon.Information,
MessageBoxDefaultButton.Button1,
0,
FORUMURL,
"Forum");
        }

        private void OverviewList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                string s = this.overviewList.SelectedItem.ToString();
                Clipboard.SetData(DataFormats.StringFormat, s);
            }
        }

        private void PopulationComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.PopulationComboBox1.SelectedValue == null || this.controller == null) return;
            DataTable dt = this.controller.GetPopulationResources(int.Parse(this.PopulationComboBox1.SelectedValue.ToString()));
            this.populationMineralChartData = this.AdjustResourceDataTime(dt);
            this.ApplyPopulationMineralChart();
        }

        private sealed class ResourceColorDialog : Form
        {
            private readonly Dictionary<string, Color> workingColors;
            private readonly Dictionary<string, Panel> swatches = new Dictionary<string, Panel>(StringComparer.OrdinalIgnoreCase);
            private readonly FlowLayoutPanel colorPanel;

            public Dictionary<string, Color> SelectedColors { get; private set; }

            public ResourceColorDialog(Dictionary<string, Color> colors)
            {
                this.workingColors = new Dictionary<string, Color>(colors, StringComparer.OrdinalIgnoreCase);
                this.Text = "Customize Resource Colors";
                this.StartPosition = FormStartPosition.CenterParent;
                this.MinimizeBox = false;
                this.MaximizeBox = false;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.ClientSize = new Size(520, 500);

                Label explanation = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 52,
                    Text = "Choose a color for each resource. The selected colors are used in both Light and Dark mode and are saved for future launches.",
                    Padding = new Padding(8)
                };

                this.colorPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoScroll = true,
                    Padding = new Padding(8)
                };

                foreach (string name in ThemeManager.ResourceNames)
                {
                    Panel row = new Panel { Width = 465, Height = 32, Margin = new Padding(0, 0, 0, 4) };
                    Label label = new Label { Text = name, AutoSize = false, Width = 150, Height = 28, TextAlign = ContentAlignment.MiddleLeft };
                    Panel swatch = new Panel { Width = 45, Height = 24, Left = 160, Top = 2, BorderStyle = BorderStyle.FixedSingle, BackColor = this.workingColors[name] };
                    Button choose = new Button { Text = "Choose...", Width = 90, Height = 26, Left = 215, Top = 1 };
                    choose.Click += (sender, e) => this.ChooseColor(name);
                    row.Controls.Add(label);
                    row.Controls.Add(swatch);
                    row.Controls.Add(choose);
                    this.colorPanel.Controls.Add(row);
                    this.swatches[name] = swatch;
                }

                FlowLayoutPanel buttons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 42,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
                Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
                Button reset = new Button { Text = "Reset defaults", Width = 110 };
                reset.Click += (sender, e) =>
                {
                    this.workingColors.Clear();
                    foreach (var pair in ThemeManager.GetDefaultResourceColors()) this.workingColors[pair.Key] = pair.Value;
                    foreach (var pair in this.swatches) pair.Value.BackColor = this.workingColors[pair.Key];
                };
                buttons.Controls.Add(cancel);
                buttons.Controls.Add(ok);
                buttons.Controls.Add(reset);

                this.Controls.Add(this.colorPanel);
                this.Controls.Add(explanation);
                this.Controls.Add(buttons);
                this.AcceptButton = ok;
                this.CancelButton = cancel;
                this.FormClosing += (sender, e) =>
                {
                    if (this.DialogResult == DialogResult.OK)
                    {
                        this.SelectedColors = new Dictionary<string, Color>(this.workingColors, StringComparer.OrdinalIgnoreCase);
                    }
                };
            }

            private void ChooseColor(string name)
            {
                using (ColorDialog dialog = new ColorDialog())
                {
                    dialog.FullOpen = true;
                    dialog.Color = this.workingColors[name];
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        this.workingColors[name] = dialog.Color;
                        this.swatches[name].BackColor = dialog.Color;
                    }
                }
            }
        }

    }
}
