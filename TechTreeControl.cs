namespace AuroraMarvin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Windows.Forms;
    using Microsoft.Msagl.Drawing;
    using Color = System.Drawing.Color;
    using Font = System.Drawing.Font;
    using FontStyle = System.Drawing.FontStyle;

    internal sealed class TechTreeNodeInfo
    {
        public string FieldName { get; set; }
        public string Name { get; set; }
        public long DevelopCost { get; set; }
        public bool Researched { get; set; }
        public bool CurrentlyResearching { get; set; }
    }

    /// <summary>
    /// Aurora MarvinS custom Tech Tree renderer.
    /// The layout is deliberately deterministic: technologies flow left-to-right,
    /// direct prerequisite chains stay on the same horizontal track where possible,
    /// and each Research Field is rendered as its own coloured band.
    /// </summary>
    internal sealed class TechTreeControl : Control
    {
        private sealed class LayoutNode
        {
            public Node Node;
            public TechTreeNodeInfo Info;
            public int Column;
            public int Row;
            public RectangleF Bounds;
        }

        private readonly Graph graph;
        private readonly List<LayoutNode> nodes = new List<LayoutNode>();
        private readonly Dictionary<string, LayoutNode> byId = new Dictionary<string, LayoutNode>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Color> fieldColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "Power and Propulsion", Color.FromArgb(225, 55, 60) },
            { "Sensors and Control", Color.FromArgb(205, 145, 15) },
            { "Direct Fire Weapons", Color.FromArgb(205, 175, 15) },
            { "Missiles", Color.FromArgb(210, 65, 125) },
            { "Construction / Production", Color.FromArgb(25, 160, 90) },
            { "Logistics", Color.FromArgb(20, 155, 190) },
            { "Defensive Systems", Color.FromArgb(65, 105, 205) },
            { "Biology / Genetics", Color.FromArgb(135, 70, 190) },
            { "Ground Combat", Color.FromArgb(115, 115, 125) }
        };

        private readonly Color[] fallbackColors =
        {
            Color.FromArgb(225, 55, 60), Color.FromArgb(205, 145, 15), Color.FromArgb(25, 160, 90),
            Color.FromArgb(20, 155, 190), Color.FromArgb(135, 70, 190), Color.FromArgb(65, 105, 205)
        };

        private float zoom = 1.0f;
        private Point pan = new Point(20, 20);
        private Point lastMouse;
        private bool panning;
        private RectangleF contentBounds;
        private ComboBox researchStatusComboBox;

        // Same size for every technology node.
        private const float NodeWidth = 148f;
        private const float NodeHeight = 58f;
        private const float ColumnGap = 22f;
        private const float RowGap = 12f;
        private const float FieldGap = 26f;
        private const float HeaderHeight = 42f;
        private const float LeftMargin = 235f;
        private const float TopMargin = 18f;
        private const float BottomMargin = 24f;

        public TechTreeControl(Graph graph)
        {
            this.graph = graph;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(6, 20, 40);
            this.ForeColor = Color.White;
            this.Dock = DockStyle.Fill;
            this.TabStop = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            this.researchStatusComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Height = 24,
                Location = new Point(625, 5),
                Name = "researchStatusComboBox"
            };
            this.researchStatusComboBox.Items.AddRange(new object[]
            {
                "All",
                "Unresearched only",
                "Researched only",
                "Currently researching"
            });
            this.researchStatusComboBox.SelectedIndex = 0;
            this.researchStatusComboBox.SelectedIndexChanged += this.ResearchStatusComboBox_SelectedIndexChanged;
            this.Controls.Add(this.researchStatusComboBox);

            this.ApplyThemeToStatusSelector();
            this.BuildLayout();
        }

        public void RefreshTheme()
        {
            this.BackColor = this.DarkTheme ? Color.FromArgb(6, 20, 40) : Color.FromArgb(245, 247, 250);
            this.ApplyThemeToStatusSelector();
            this.Invalidate();
        }

        private void ResearchStatusComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.BuildLayout();
            this.Invalidate();
        }

        private void ApplyThemeToStatusSelector()
        {
            if (this.researchStatusComboBox == null) return;

            this.researchStatusComboBox.BackColor = this.DarkTheme
                ? Color.FromArgb(25, 40, 60)
                : Color.White;
            this.researchStatusComboBox.ForeColor = this.DarkTheme
                ? Color.White
                : Color.FromArgb(30, 35, 45);
        }

        private bool ShouldShowNode(TechTreeNodeInfo info)
        {
            if (this.researchStatusComboBox == null || this.researchStatusComboBox.SelectedIndex == 0)
                return true;

            if (this.researchStatusComboBox.SelectedIndex == 1)
                return !info.Researched && !info.CurrentlyResearching;

            if (this.researchStatusComboBox.SelectedIndex == 2)
                return info.Researched;

            return info.CurrentlyResearching;
        }

        private bool DarkTheme
        {
            get { return ThemeManager.CurrentTheme == MarvinTheme.Dark; }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.Width > 0 && this.Height > 0 && this.contentBounds.Width > 0)
                this.Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            float oldZoom = this.zoom;
            float factor = e.Delta > 0 ? 1.15f : 0.87f;
            this.zoom = Math.Max(0.25f, Math.Min(3.0f, this.zoom * factor));

            if (Math.Abs(this.zoom - oldZoom) > 0.001f)
            {
                float mx = e.X;
                float my = e.Y;
                this.pan.X = (int)(mx - (mx - this.pan.X) * (this.zoom / oldZoom));
                this.pan.Y = (int)(my - (my - this.pan.Y) * (this.zoom / oldZoom));
                this.ClampPan();
                this.Invalidate();
            }
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Middle ||
                e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                this.panning = true;
                this.lastMouse = e.Location;
                this.Cursor = Cursors.SizeAll;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!this.panning) return;

            this.pan.X += e.X - this.lastMouse.X;
            this.pan.Y += e.Y - this.lastMouse.Y;
            this.lastMouse = e.Location;
            this.ClampPan();
            this.Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Middle ||
                e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                this.panning = false;
                this.Cursor = Cursors.Default;
            }
        }

        private void BuildLayout()
        {
            this.nodes.Clear();
            this.byId.Clear();

            foreach (Node node in this.graph.Nodes)
            {
                TechTreeNodeInfo info = node.UserData as TechTreeNodeInfo;
                if (info == null || !this.ShouldShowNode(info)) continue;

                LayoutNode layoutNode = new LayoutNode
                {
                    Node = node,
                    Info = info,
                    Column = 0,
                    Row = 0
                };

                this.nodes.Add(layoutNode);
                this.byId[node.Id] = layoutNode;
            }

            Dictionary<string, List<string>> predecessors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<string>> successors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> indegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (LayoutNode node in this.nodes)
            {
                predecessors[node.Node.Id] = new List<string>();
                successors[node.Node.Id] = new List<string>();
                indegree[node.Node.Id] = 0;
            }

            foreach (Edge edge in this.graph.Edges)
            {
                if (!this.byId.ContainsKey(edge.Source) || !this.byId.ContainsKey(edge.Target))
                    continue;

                predecessors[edge.Target].Add(edge.Source);
                successors[edge.Source].Add(edge.Target);
                indegree[edge.Target]++;
            }

            // Give every technology a left-to-right depth. A technology is always
            // placed after all of its visible prerequisites. This keeps normal
            // research chains together while still allowing branches to split.
            Dictionary<string, int> depth = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Queue<string> queue = new Queue<string>();

            foreach (LayoutNode node in this.nodes
                .OrderBy(n => GetFieldOrder(n.Info.FieldName))
                .ThenBy(n => n.Info.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (indegree[node.Node.Id] == 0)
                {
                    depth[node.Node.Id] = 0;
                    queue.Enqueue(node.Node.Id);
                }
            }

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                int currentDepth = depth[current];

                foreach (string target in successors[current])
                {
                    if (!depth.ContainsKey(target) || depth[target] < currentDepth + 1)
                        depth[target] = currentDepth + 1;

                    indegree[target]--;
                    if (indegree[target] == 0)
                        queue.Enqueue(target);
                }
            }

            int maxDepth = depth.Count == 0 ? 0 : depth.Values.Max();
            foreach (LayoutNode node in this.nodes)
            {
                if (!depth.ContainsKey(node.Node.Id))
                    depth[node.Node.Id] = ++maxDepth;

                node.Column = depth[node.Node.Id];
            }

            List<string> fields = this.nodes
                .Select(n => string.IsNullOrWhiteSpace(n.Info.FieldName) ? "Other" : n.Info.FieldName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetFieldOrder)
                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            float y = TopMargin;

            foreach (string field in fields)
            {
                List<LayoutNode> fieldNodes = this.nodes
                    .Where(n => string.Equals(
                        string.IsNullOrWhiteSpace(n.Info.FieldName) ? "Other" : n.Info.FieldName,
                        field,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (fieldNodes.Count == 0)
                    continue;

                Dictionary<int, List<LayoutNode>> byColumn = new Dictionary<int, List<LayoutNode>>();
                foreach (LayoutNode node in fieldNodes)
                {
                    if (!byColumn.ContainsKey(node.Column))
                        byColumn[node.Column] = new List<LayoutNode>();
                    byColumn[node.Column].Add(node);
                }

                int minColumn = byColumn.Keys.Min();
                int maxColumn = byColumn.Keys.Max();

                // Initial ordering is stable. Subsequent barycentric sweeps move
                // a node toward the centre of the technologies it unlocks or is
                // unlocked by. This is important for Aurora because a single
                // technology can unlock several branches: the parent then sits
                // visually between those branches instead of above or below all of them.
                foreach (int column in byColumn.Keys.OrderBy(c => c))
                {
                    int row = 0;
                    foreach (LayoutNode node in byColumn[column].OrderBy(n => n.Info.Name, StringComparer.OrdinalIgnoreCase))
                        node.Row = row++;
                }

                for (int pass = 0; pass < 8; pass++)
                {
                    // Left-to-right: keep children close to their visible parents.
                    for (int column = minColumn + 1; column <= maxColumn; column++)
                    {
                        if (!byColumn.ContainsKey(column)) continue;

                        List<LayoutNode> ordered = byColumn[column]
                            .OrderBy(n => GetBarycenter(n, predecessors, field, true))
                            .ThenBy(n => n.Info.Name, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        AssignSequentialRows(ordered);
                    }

                    // Right-to-left: when a technology unlocks multiple technologies,
                    // put the parent between those children. This produces the visual
                    // "hub in the middle" effect used by the reference Tech Tree.
                    for (int column = maxColumn - 1; column >= minColumn; column--)
                    {
                        if (!byColumn.ContainsKey(column)) continue;

                        List<LayoutNode> ordered = byColumn[column]
                            .OrderBy(n => GetBarycenter(n, successors, field, false))
                            .ThenBy(n => n.Info.Name, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        AssignSequentialRows(ordered);
                    }
                }

                int maxRow = fieldNodes.Count == 0 ? 0 : fieldNodes.Max(n => n.Row);

                foreach (LayoutNode node in fieldNodes)
                {
                    float x = LeftMargin + node.Column * (NodeWidth + ColumnGap);
                    float nodeY = y + HeaderHeight + node.Row * (NodeHeight + RowGap);
                    node.Bounds = new RectangleF(x, nodeY, NodeWidth, NodeHeight);
                }

                float fieldHeight = HeaderHeight + (maxRow + 1) * NodeHeight +
                                    maxRow * RowGap + 12f;

                y += fieldHeight + FieldGap;
            }

            float right = this.nodes.Count == 0 ? LeftMargin + NodeWidth : this.nodes.Max(n => n.Bounds.Right);
            float bottom = this.nodes.Count == 0 ? TopMargin + HeaderHeight : this.nodes.Max(n => n.Bounds.Bottom);

            this.contentBounds = new RectangleF(0, 0, right + 45, bottom + BottomMargin);
            this.zoom = 1.0f;
            this.pan = new Point(20, 20);
            this.ClampPan();
        }

        private static void AssignSequentialRows(List<LayoutNode> ordered)
        {
            for (int i = 0; i < ordered.Count; i++)
                ordered[i].Row = i;
        }

        private double GetBarycenter(
            LayoutNode node,
            Dictionary<string, List<string>> links,
            string field,
            bool preferPredecessors)
        {
            List<int> rows = links[node.Node.Id]
                .Where(id => this.byId.ContainsKey(id))
                .Where(id => string.Equals(
                    string.IsNullOrWhiteSpace(this.byId[id].Info.FieldName) ? "Other" : this.byId[id].Info.FieldName,
                    field,
                    StringComparison.OrdinalIgnoreCase))
                .Select(id => this.byId[id].Row)
                .ToList();

            if (rows.Count > 0)
                return rows.Average();

            // Cross-field dependencies do not share the same row coordinate system,
            // so use the current row as a stable fallback rather than forcing one
            // research field to distort another.
            return node.Row;
        }

        private static int FindNearestFreeRow(HashSet<int> occupied, int preferred)
        {
            if (!occupied.Contains(preferred))
                return preferred;

            for (int distance = 1; distance < 10000; distance++)
            {
                int above = preferred - distance;
                if (above >= 0 && !occupied.Contains(above))
                    return above;

                int below = preferred + distance;
                if (!occupied.Contains(below))
                    return below;
            }

            return preferred;
        }

        private static int GetFieldOrder(string field)
        {
            string[] order =
            {
                "Power and Propulsion",
                "Sensors and Control",
                "Direct Fire Weapons",
                "Missiles",
                "Construction / Production",
                "Logistics",
                "Defensive Systems",
                "Biology / Genetics",
                "Ground Combat"
            };

            for (int i = 0; i < order.Length; i++)
            {
                if (string.Equals(order[i], field, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return order.Length + 1;
        }

        private Color FieldColor(string field)
        {
            Color color;
            if (this.fieldColors.TryGetValue(field ?? "", out color))
                return color;

            return this.fallbackColors[
                Math.Abs((field ?? "Other").GetHashCode()) % this.fallbackColors.Length];
        }

        private void ClampPan()
        {
            if (this.contentBounds.Width <= 0)
                return;

            float scaledW = this.contentBounds.Width * this.zoom;
            float scaledH = this.contentBounds.Height * this.zoom;

            int minX = scaledW < this.ClientSize.Width
                ? (int)((this.ClientSize.Width - scaledW) / 2)
                : (int)(this.ClientSize.Width - scaledW - 30);

            int maxX = scaledW < this.ClientSize.Width ? minX : 30;

            int minY = scaledH < this.ClientSize.Height
                ? (int)((this.ClientSize.Height - scaledH) / 2)
                : (int)(this.ClientSize.Height - scaledH - 30);

            int maxY = scaledH < this.ClientSize.Height ? minY : 30;

            this.pan.X = Math.Max(minX, Math.Min(maxX, this.pan.X));
            this.pan.Y = Math.Max(minY, Math.Min(maxY, this.pan.Y));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color background = this.DarkTheme
                ? Color.FromArgb(6, 20, 40)
                : Color.FromArgb(245, 247, 250);

            Color text = this.DarkTheme
                ? Color.White
                : Color.FromArgb(30, 35, 45);

            Color edge = this.DarkTheme
                ? Color.FromArgb(245, 210, 30)
                : Color.FromArgb(75, 85, 100);

            g.Clear(background);
            this.DrawToolbar(g, text);

            GraphicsState state = g.Save();
            g.TranslateTransform(this.pan.X, this.pan.Y + 35);
            g.ScaleTransform(this.zoom, this.zoom);

            this.DrawFields(g);
            this.DrawEdges(g, edge);
            this.DrawNodes(g);

            g.Restore(state);
        }

        private void DrawToolbar(Graphics g, Color text)
        {
            using (Brush b = new SolidBrush(
                this.DarkTheme ? Color.FromArgb(10, 28, 52) : Color.White))
            {
                g.FillRectangle(b, 0, 0, this.Width, 35);
            }

            using (Pen p = new Pen(
                this.DarkTheme ? Color.FromArgb(45, 75, 105) : Color.FromArgb(205, 210, 218)))
            {
                g.DrawLine(p, 0, 34, this.Width, 34);
            }

            using (Font f = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Brush b = new SolidBrush(text))
            {
                g.DrawString("Tech Tree", f, b, 12, 9);
            }

            using (Font f = new Font("Segoe UI", 8f))
            using (Brush b = new SolidBrush(text))
            {
                g.DrawString(
                    "Mouse wheel: zoom    Middle/right drag: pan    Fit: double-click",
                    f,
                    b,
                    90,
                    10);

                g.DrawString("Research status:", f, b, 515, 10);
            }
        }

        private void DrawFields(Graphics g)
        {
            string[] fields = this.nodes
                .Select(n => string.IsNullOrWhiteSpace(n.Info.FieldName) ? "Other" : n.Info.FieldName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetFieldOrder)
                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (string field in fields)
            {
                List<LayoutNode> fieldNodes = this.nodes
                    .Where(n => string.Equals(
                        string.IsNullOrWhiteSpace(n.Info.FieldName) ? "Other" : n.Info.FieldName,
                        field,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (fieldNodes.Count == 0)
                    continue;

                float top = fieldNodes.Min(n => n.Bounds.Top) - HeaderHeight + 8;
                float bottom = fieldNodes.Max(n => n.Bounds.Bottom) + 12;
                Color color = this.FieldColor(field);

                using (Pen pen = new Pen(
                    Color.FromArgb(145, color.R, color.G, color.B),
                    1.2f))
                {
                    g.DrawLine(pen, 0, top, this.contentBounds.Right, top);
                }

                using (Font f = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (Brush b = new SolidBrush(color))
                {
                    // Field name is placed in the dedicated left margin.
                    g.DrawString(field, f, b, 14, top + 8);
                }

                using (Pen pen = new Pen(
                    Color.FromArgb(55, color.R, color.G, color.B),
                    1f))
                {
                    g.DrawLine(pen, 0, bottom, this.contentBounds.Right, bottom);
                }
            }
        }

        private void DrawEdges(Graphics g, Color edgeColor)
        {
            foreach (Edge edge in this.graph.Edges)
            {
                LayoutNode source;
                LayoutNode target;

                if (!this.byId.TryGetValue(edge.Source, out source) ||
                    !this.byId.TryGetValue(edge.Target, out target))
                    continue;

                // A dependency line always carries the colour of the technology
                // it originates from. This makes cross-field dependencies obvious:
                // a blue Defensive Systems technology unlocking a yellow weapon
                // technology is still connected by a blue line.
                Color sourceColor = this.FieldColor(source.Info.FieldName);
                bool crossField = !string.Equals(
                    source.Info.FieldName,
                    target.Info.FieldName,
                    StringComparison.OrdinalIgnoreCase);

                using (Pen pen = new Pen(sourceColor, crossField ? 2.1f : 1.6f))
                {
                    pen.EndCap = LineCap.ArrowAnchor;

                    PointF a = new PointF(source.Bounds.Right, source.Bounds.Top + source.Bounds.Height / 2f);
                    PointF b = new PointF(target.Bounds.Left, target.Bounds.Top + target.Bounds.Height / 2f);

                    if (Math.Abs(a.Y - b.Y) < 1.0f)
                    {
                        g.DrawLine(pen, a, b);
                    }
                    else
                    {
                        float distance = Math.Max(20f, b.X - a.X);
                        float midX = a.X + distance / 2f;

                        using (GraphicsPath path = new GraphicsPath())
                        {
                            path.AddBezier(
                                a,
                                new PointF(midX, a.Y),
                                new PointF(midX, b.Y),
                                b);
                            g.DrawPath(pen, path);
                        }
                    }
                }
            }
        }

        private void DrawNodes(Graphics g)
        {
            using (Font nameFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font costFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            {
                foreach (LayoutNode node in this.nodes)
                {
                    List<Color> colors = GetNodeColors(node);
                    float segmentWidth = node.Bounds.Width / colors.Count;

                    for (int i = 0; i < colors.Count; i++)
                    {
                        RectangleF segment = new RectangleF(
                            node.Bounds.X + i * segmentWidth,
                            node.Bounds.Y,
                            i == colors.Count - 1 ? node.Bounds.Width - i * segmentWidth : segmentWidth + 0.5f,
                            node.Bounds.Height);

                        Color color = colors[i];
                        using (Brush fill = new SolidBrush(
                            Color.FromArgb(this.DarkTheme ? 235 : 245, color.R, color.G, color.B)))
                        {
                            g.FillRectangle(fill, segment);
                        }
                    }

                    Color primary = this.FieldColor(node.Info.FieldName);
                    using (Pen border = new Pen(
                        this.DarkTheme
                            ? ControlPaint.Light(primary, 0.15f)
                            : ControlPaint.Dark(primary, 0.15f),
                        1.2f))
                    {
                        g.DrawRectangle(border, node.Bounds.X, node.Bounds.Y, node.Bounds.Width, node.Bounds.Height);
                    }

                    if (node.Info.Researched)
                    {
                        using (Brush statusBrush = new SolidBrush(Color.FromArgb(45, 210, 105)))
                            g.FillRectangle(statusBrush, node.Bounds.X, node.Bounds.Y, 5f, node.Bounds.Height);
                    }
                    else if (node.Info.CurrentlyResearching)
                    {
                        using (Brush statusBrush = new SolidBrush(Color.FromArgb(255, 205, 45)))
                            g.FillRectangle(statusBrush, node.Bounds.X, node.Bounds.Y, 5f, node.Bounds.Height);
                    }

                    StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    RectangleF nameRect = new RectangleF(
                        node.Bounds.X + 5,
                        node.Bounds.Y + 3,
                        node.Bounds.Width - 10,
                        node.Bounds.Height - 23);

                    string displayName = node.Info.Name;
                    if (node.Info.Researched)
                        displayName = "✓ " + displayName;
                    else if (node.Info.CurrentlyResearching)
                        displayName = "● " + displayName;

                    g.DrawString(displayName, nameFont, Brushes.White, nameRect, format);

                    string cost = node.Info.DevelopCost > 0
                        ? "Cost: " + FormatCost(node.Info.DevelopCost)
                        : "Cost: N/A";

                    RectangleF costRect = new RectangleF(
                        node.Bounds.X + 5,
                        node.Bounds.Bottom - 19,
                        node.Bounds.Width - 10,
                        16);

                    g.DrawString(cost, costFont, Brushes.White, costRect, format);
                }
            }
        }

        private List<Color> GetNodeColors(LayoutNode node)
        {
            List<Color> colors = new List<Color>();
            AddUniqueColor(colors, this.FieldColor(node.Info.FieldName));

            foreach (Edge edge in this.graph.Edges)
            {
                if (!string.Equals(edge.Source, node.Node.Id, StringComparison.OrdinalIgnoreCase))
                    continue;

                LayoutNode target;
                if (!this.byId.TryGetValue(edge.Target, out target))
                    continue;

                if (!string.Equals(node.Info.FieldName, target.Info.FieldName, StringComparison.OrdinalIgnoreCase))
                    AddUniqueColor(colors, this.FieldColor(target.Info.FieldName));
            }

            return colors;
        }

        private static void AddUniqueColor(List<Color> colors, Color color)
        {
            if (!colors.Any(c => c.ToArgb() == color.ToArgb()))
                colors.Add(color);
        }

        private static string FormatCost(long cost)
        {
            return cost.ToString("N0");
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            this.zoom = 1.0f;
            this.pan = new Point(20, 20);
            this.ClampPan();
            this.Invalidate();
        }
    }
}
