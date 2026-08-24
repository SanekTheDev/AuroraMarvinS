namespace AuroraMarvin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using System.Windows.Forms.DataVisualization.Charting;

    internal enum MarvinTheme
    {
        Light,
        Dark
    }

    internal static class ThemeManager
    {
        private static readonly Dictionary<string, Color> OriginalSeriesColors = new Dictionary<string, Color>();

        // Fixed high-contrast Aurora resource palette. The same palette is used in
        // both Light and Dark themes so a mineral always has the same visual identity.
        private static readonly Dictionary<string, Color> DefaultResourceColors =
            new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                { "Duranium", Color.FromArgb(220, 40, 45) },       // red
                { "Neutronium", Color.FromArgb(245, 135, 0) },     // orange
                { "Corbomite", Color.FromArgb(35, 170, 70) },      // green
                { "Tritanium", Color.FromArgb(0, 165, 145) },      // turquoise
                { "Boronide", Color.FromArgb(0, 180, 220) },       // cyan
                { "Mercassium", Color.FromArgb(125, 55, 190) },    // purple
                { "Vendarite", Color.FromArgb(235, 195, 0) },      // yellow
                { "Sorium", Color.FromArgb(230, 55, 135) },        // pink
                { "Uridium", Color.FromArgb(35, 55, 145) },        // navy
                { "Corundium", Color.FromArgb(25, 95, 235) },       // blue
                { "Gallicite", Color.FromArgb(150, 85, 55) }       // brown
            };

        private static readonly Dictionary<string, Color> ResourceColors =
            new Dictionary<string, Color>(DefaultResourceColors, StringComparer.OrdinalIgnoreCase);

        public static readonly string[] ResourceNames =
        {
            "Duranium", "Neutronium", "Corbomite", "Tritanium", "Boronide", "Mercassium",
            "Vendarite", "Sorium", "Uridium", "Corundium", "Gallicite"
        };

        public static MarvinTheme CurrentTheme { get; private set; } = MarvinTheme.Light;

        public static Color GetResourceColor(string resourceName)
        {
            Color color;
            return ResourceColors.TryGetValue(resourceName, out color) ? color : Color.White;
        }

        public static void SetResourceColor(string resourceName, Color color)
        {
            if (string.IsNullOrWhiteSpace(resourceName)) return;
            ResourceColors[resourceName] = color;
        }

        public static void ResetResourceColors()
        {
            ResourceColors.Clear();
            foreach (var pair in DefaultResourceColors)
            {
                ResourceColors[pair.Key] = pair.Value;
            }
        }

        public static Dictionary<string, Color> GetResourceColors()
        {
            return new Dictionary<string, Color>(ResourceColors, StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<string, Color> GetDefaultResourceColors()
        {
            return new Dictionary<string, Color>(DefaultResourceColors, StringComparer.OrdinalIgnoreCase);
        }

        public static void LoadResourceColors(IDictionary<string, Color> colors)
        {
            ResetResourceColors();
            if (colors == null) return;
            foreach (var pair in colors)
            {
                if (DefaultResourceColors.ContainsKey(pair.Key))
                {
                    ResourceColors[pair.Key] = pair.Value;
                }
            }
        }

        public static void Apply(Form form, MarvinTheme theme)
        {
            CurrentTheme = theme;
            bool dark = theme == MarvinTheme.Dark;

            Color back = dark ? Color.FromArgb(28, 30, 34) : SystemColors.Control;
            Color surface = dark ? Color.FromArgb(36, 39, 44) : SystemColors.Window;
            Color text = dark ? Color.FromArgb(235, 235, 235) : SystemColors.ControlText;
            Color border = dark ? Color.FromArgb(75, 80, 88) : SystemColors.ControlDark;

            form.BackColor = back;
            form.ForeColor = text;

            ApplyControlTree(form.Controls, back, surface, text, border, dark);

            foreach (Chart chart in FindControls<Chart>(form))
            {
                ApplyChartTheme(chart, dark);
            }
        }

        public static void ApplyChartTheme(Chart chart, bool dark)
        {
            Color chartBack = dark ? Color.FromArgb(24, 26, 30) : Color.White;
            Color text = dark ? Color.FromArgb(235, 235, 235) : Color.Black;
            Color grid = dark ? Color.FromArgb(70, 74, 82) : Color.Black;

            chart.BackColor = chartBack;
            chart.ForeColor = text;

            foreach (ChartArea area in chart.ChartAreas)
            {
                area.BackColor = chartBack;
                area.AxisX.LabelStyle.ForeColor = text;
                area.AxisY.LabelStyle.ForeColor = text;
                area.AxisX.TitleForeColor = text;
                area.AxisY.TitleForeColor = text;
                area.AxisX.LineColor = text;
                area.AxisY.LineColor = text;
                area.AxisX.MajorGrid.LineColor = grid;
                area.AxisY.MajorGrid.LineColor = grid;
                area.AxisX.MinorGrid.LineColor = dark ? Color.FromArgb(50, 54, 60) : Color.LightGray;
                area.AxisY.MinorGrid.LineColor = dark ? Color.FromArgb(50, 54, 60) : Color.LightGray;
            }

            foreach (Legend legend in chart.Legends)
            {
                legend.BackColor = chartBack;
                legend.ForeColor = text;
            }

            foreach (Series series in chart.Series)
            {
                if (series.Name.StartsWith("__", StringComparison.Ordinal))
                {
                    continue;
                }

                string key = chart.GetHashCode().ToString() + "|" + series.Name;
                if (!OriginalSeriesColors.ContainsKey(key))
                {
                    OriginalSeriesColors[key] = series.Color;
                }

                Color resourceColor;
                if (ResourceColors.TryGetValue(series.Name, out resourceColor))
                {
                    // Keep mineral identity identical in Light and Dark modes.
                    series.Color = resourceColor;
                }
                else if (dark)
                {
                    series.Color = MakeVisible(series.Color);
                }
                else
                {
                    series.Color = OriginalSeriesColors[key];
                }
            }
        }

        private static Color MakeVisible(Color color)
        {
            // Keep bright colors unchanged; lift darker series so they remain readable on a dark chart.
            int brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
            if (brightness >= 130)
            {
                return color;
            }

            return ControlPaint.LightLight(color);
        }

        private static void ApplyControlTree(Control.ControlCollection controls, Color back, Color surface, Color text, Color border, bool dark)
        {
            foreach (Control control in controls)
            {
                if (control is MenuStrip menuStrip)
                {
                    menuStrip.BackColor = surface;
                    menuStrip.ForeColor = text;
                    menuStrip.Renderer = dark ? new DarkToolStripRenderer() : new ToolStripProfessionalRenderer();
                    ApplyToolStripItems(menuStrip.Items, surface, text, dark);
                }
                else if (control is ToolStrip toolStrip)
                {
                    toolStrip.BackColor = surface;
                    toolStrip.ForeColor = text;
                    toolStrip.Renderer = dark ? new DarkToolStripRenderer() : new ToolStripProfessionalRenderer();
                    ApplyToolStripItems(toolStrip.Items, surface, text, dark);
                }
                else if (control is DataGridView grid)
                {
                    grid.BackgroundColor = surface;
                    grid.GridColor = border;
                    grid.DefaultCellStyle.BackColor = surface;
                    grid.DefaultCellStyle.ForeColor = text;
                    grid.DefaultCellStyle.SelectionBackColor = dark ? Color.FromArgb(55, 75, 100) : SystemColors.Highlight;
                    grid.DefaultCellStyle.SelectionForeColor = dark ? Color.White : SystemColors.HighlightText;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = dark ? Color.FromArgb(45, 48, 54) : SystemColors.Control;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = text;
                    grid.EnableHeadersVisualStyles = false;
                }
                else if (control is TabControl tabs)
                {
                    tabs.BackColor = surface;
                    tabs.ForeColor = text;
                }
                else if (control is TabPage)
                {
                    control.BackColor = surface;
                    control.ForeColor = text;
                }
                else if (control is Label label)
                {
                    label.ForeColor = text;
                    label.BackColor = Color.Transparent;
                }
                else if (control is Button button)
                {
                    button.BackColor = dark ? Color.FromArgb(48, 52, 58) : SystemColors.Control;
                    button.ForeColor = text;
                    button.FlatStyle = FlatStyle.System;
                }
                else if (control is ComboBox combo)
                {
                    combo.BackColor = surface;
                    combo.ForeColor = text;
                }
                else if (control is TextBoxBase textBox)
                {
                    textBox.BackColor = surface;
                    textBox.ForeColor = text;
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.BackColor = Color.Transparent;
                    checkBox.ForeColor = text;
                }
                else if (control is ListBox listBox)
                {
                    listBox.BackColor = surface;
                    listBox.ForeColor = text;
                }
                else if (control is NumericUpDown numeric)
                {
                    numeric.BackColor = surface;
                    numeric.ForeColor = text;
                }
                else if (control is GroupBox groupBox)
                {
                    groupBox.BackColor = surface;
                    groupBox.ForeColor = text;
                }
                else
                {
                    control.ForeColor = text;
                    if (control.BackColor == SystemColors.Window || control.BackColor == SystemColors.Control)
                    {
                        control.BackColor = surface;
                    }
                }

                if (control.HasChildren)
                {
                    ApplyControlTree(control.Controls, back, surface, text, border, dark);
                }
            }
        }

        private static void ApplyToolStripItems(ToolStripItemCollection items, Color surface, Color text, bool dark)
        {
            foreach (ToolStripItem item in items)
            {
                item.BackColor = surface;
                item.ForeColor = text;
                if (item is ToolStripMenuItem menuItem && menuItem.DropDownItems.Count > 0)
                {
                    ApplyToolStripItems(menuItem.DropDownItems, dark ? Color.FromArgb(36, 39, 44) : SystemColors.Menu, text, dark);
                }
            }
        }

        private static IEnumerable<T> FindControls<T>(Control root) where T : Control
        {
            foreach (Control control in root.Controls)
            {
                if (control is T match)
                {
                    yield return match;
                }

                if (control.HasChildren)
                {
                    foreach (T child in FindControls<T>(control))
                    {
                        yield return child;
                    }
                }
            }
        }
        private sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
        {
            public DarkToolStripRenderer() : base(new DarkColorTable())
            {
                this.RoundedEdges = false;
            }
        }

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            private static readonly Color Surface = Color.FromArgb(36, 39, 44);
            private static readonly Color Darker = Color.FromArgb(28, 30, 34);
            private static readonly Color Hover = Color.FromArgb(55, 60, 68);
            private static readonly Color Border = Color.FromArgb(75, 80, 88);

            public override Color MenuStripGradientBegin => Surface;
            public override Color MenuStripGradientEnd => Surface;
            public override Color ToolStripDropDownBackground => Surface;
            public override Color ImageMarginGradientBegin => Surface;
            public override Color ImageMarginGradientMiddle => Surface;
            public override Color ImageMarginGradientEnd => Surface;
            public override Color MenuBorder => Border;
            public override Color MenuItemSelected => Hover;
            public override Color MenuItemBorder => Border;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Color.FromArgb(50, 54, 60);
            public override Color ToolStripBorder => Border;
            public override Color ButtonSelectedGradientBegin => Hover;
            public override Color ButtonSelectedGradientEnd => Hover;
            public override Color ButtonPressedGradientBegin => Darker;
            public override Color ButtonPressedGradientEnd => Darker;
        }

    }
}