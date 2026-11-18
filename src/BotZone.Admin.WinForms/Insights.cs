using System.Drawing.Drawing2D;
using System.Globalization;

namespace BotZone.Admin.WinForms;

internal sealed record InsightPoint(string Label, decimal Value, string? Key = null);
internal enum InsightKind { Bars, Line, Donut }

// Charts use the same values as the accompanying records. Selection is available by
// mouse and keyboard, with the exact value always visible (not only in a tooltip).
internal class InsightChart : ScrollableControl
{
    private List<InsightPoint> points = [];
    private readonly List<(RectangleF Bounds, int Index)> targets = [];
    private int selection;
    private string caption = "Đang tải…", unit = "";
    private InsightKind kind;
    internal event Action<int>? RowSelected;
    internal event Action? RowActivated;
    internal IReadOnlyList<InsightPoint> Points => points;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal InsightKind Kind { get => kind; set { kind = value; AutoScrollPosition = Point.Empty; Measure(); Invalidate(); } }
    private int S(float value) => (int)Math.Round(value * DeviceDpi / 96f);
    internal InsightChart()
    {
        DoubleBuffered = true; AutoScroll = true; TabStop = true; BackColor = Ui.Surface;
        AccessibleName = "Biểu đồ"; AccessibleRole = AccessibleRole.Chart;
    }
    internal void SetPoints(IEnumerable<InsightPoint> values, string title, string suffix = "")
    {
        points = values.ToList(); caption = title; unit = suffix; selection = Math.Clamp(selection, 0, Math.Max(0, points.Count - 1));
        AutoScrollPosition = Point.Empty; Measure(); UpdateDescription(); Invalidate();
    }
    private string Value(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + (unit.Length > 0 ? " " + unit : "");
    private void UpdateDescription() => AccessibleDescription = caption + ". " + (points.Count == 0 ? "Chưa có dữ liệu." : points[selection].Label + ": " + Value(points[selection].Value)) + ". Mũi tên chọn mục; Enter mở chi tiết.";
    private void Measure() => AutoScrollMinSize = new Size(0, kind == InsightKind.Bars ? S(98 + points.Count * 46) : kind == InsightKind.Donut ? S(Math.Max(300, 92 + points.Count * 40)) : S(250));
    protected override void OnResize(EventArgs e) { base.OnResize(e); Measure(); Invalidate(); }
    protected override void OnScroll(ScrollEventArgs e) { base.OnScroll(e); Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    internal void SelectRow(int index)
    {
        if (points.Count == 0) return;
        selection = Math.Clamp(index, 0, points.Count - 1);
        if (Kind == InsightKind.Bars) { var top = S(90 + selection * 46); if (top < -AutoScrollPosition.Y || top + S(46) > -AutoScrollPosition.Y + Height) AutoScrollPosition = new Point(0, Math.Max(0, top - S(90))); }
        UpdateDescription(); RowSelected?.Invoke(selection); Invalidate();
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (points.Count > 0)
        {
            if (keyData is Keys.Right or Keys.Down or Keys.Left or Keys.Up or Keys.Home or Keys.End)
            { SelectRow(keyData switch { Keys.Home => 0, Keys.End => points.Count - 1, Keys.Left or Keys.Up => selection - 1, _ => selection + 1 }); return true; }
            if (keyData is Keys.Enter or Keys.Space) { RowActivated?.Invoke(); return true; }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e); Focus();
        var hit = targets.FirstOrDefault(x => x.Bounds.Contains(e.Location));
        if (hit.Bounds.Width > 0) SelectRow(hit.Index);
    }
    protected override void OnMouseDoubleClick(MouseEventArgs e) { base.OnMouseDoubleClick(e); if (targets.Any(x => x.Bounds.Contains(e.Location))) RowActivated?.Invoke(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; targets.Clear();
        var y = AutoScrollPosition.Y;
        Draw(g, caption, new Rectangle(S(20), S(16) + y, Width - S(40), S(26)), Ui.Ink, true);
        Draw(g, points.Count == 0 ? "Chưa có dữ liệu trong phạm vi này." : points[selection].Label + "  ·  " + Value(points[selection].Value), new Rectangle(S(20), S(48) + y, Width - S(40), S(26)), Ui.Primary);
        if (points.Count == 0) return;
        if (kind == InsightKind.Line) PaintLine(g, y); else if (kind == InsightKind.Donut && points.All(x => x.Value >= 0)) PaintDonut(g, y); else PaintBars(g, y);
        if (Focused) ControlPaint.DrawFocusRectangle(g, new Rectangle(2, 2, Math.Max(1, ClientSize.Width - 5), Math.Max(1, ClientSize.Height - 5)), Ui.Focus, BackColor);
    }
    private void Draw(Graphics g, string text, Rectangle area, Color color, bool bold = false, TextFormatFlags align = TextFormatFlags.Left)
    {
        using var font = new Font(Font.FontFamily, bold ? 11 : 10, bold ? FontStyle.Bold : FontStyle.Regular);
        TextRenderer.DrawText(g, text, font, area, color, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | align);
    }
    private void PaintBars(Graphics g, int offset)
    {
        var max = points.Max(x => Math.Abs(x.Value)); var label = Math.Min(S(230), Width / 3); var numbers = Math.Min(S(180), Width / 3);
        var left = label + S(30); var span = Math.Max(1, Width - left - numbers - S(35)); var signed = points.Any(x => x.Value < 0); var zero = left + (signed ? span / 2 : 0); var extent = signed ? span / 2 : span;
        using var track = new SolidBrush(Ui.Paper); using var fill = new SolidBrush(Ui.Primary); using var axis = new Pen(Ui.ControlBorder);
        for (var i = 0; i < points.Count; i++)
        {
            var row = points[i]; var y = S(88 + i * 46) + offset; if(y+S(42)<0||y>ClientSize.Height)continue; var area = new Rectangle(S(12), y, Width - S(24), S(42)); targets.Add((area, i));
            if (i == selection) { using var bg = new SolidBrush(Ui.Soft); g.FillRectangle(bg, area); }
            Draw(g, row.Label, new Rectangle(S(20), y, label, S(42)), Ui.Ink);
            g.FillRectangle(track, left, y + S(15), span, S(12));
            var length = max == 0 ? 0 : (float)(Math.Abs(row.Value) / max * extent);
            g.FillRectangle(fill, row.Value < 0 ? zero - length : zero, y + S(15), length, S(12));
            if (signed) g.DrawLine(axis, zero, y + S(9), zero, y + S(33));
            Draw(g, Value(row.Value), new Rectangle(Width - numbers - S(20), y, numbers, S(42)), Ui.Ink, false, TextFormatFlags.Right);
        }
    }
    private void PaintLine(Graphics g, int offset)
    {
        var plot = new Rectangle(S(100), S(102) + offset, Math.Max(1, Width - S(132)), Math.Max(S(90), ClientSize.Height - S(160)));
        var low = Math.Min(0, points.Min(x => x.Value)); var high = Math.Max(0, points.Max(x => x.Value)); var range = high - low; if (range == 0) range = 1;
        float Y(decimal v) => plot.Bottom - (float)((v - low) / range * plot.Height);
        using var grid = new Pen(Ui.Line); using var stroke = new Pen(Ui.Primary, S(2)); using var fill = new SolidBrush(Ui.Primary);
        for (var i = 0; i <= 4; i++) { var v = low + range * i / 4; var y = Y(v); g.DrawLine(grid, plot.Left, y, plot.Right, y); Draw(g, Compact(v), new Rectangle(0, (int)y - S(12), plot.Left - S(10), S(24)), Ui.Muted, false, TextFormatFlags.Right); }
        var positions = points.Select((p, i) => new PointF(plot.Left + (points.Count == 1 ? plot.Width / 2f : i * plot.Width / (float)(points.Count - 1)), Y(p.Value))).ToArray();
        if (positions.Length > 1) g.DrawLines(stroke, positions);
        for (var i = 0; i < positions.Length; i++)
        {
            var p = positions[i]; var size = S(i == selection ? 8 : 4); g.FillEllipse(fill, p.X - size / 2f, p.Y - size / 2f, size, size);
            var hitWidth = Math.Max(S(8), plot.Width / (float)Math.Max(1, points.Count)); targets.Add((new RectangleF(p.X - hitWidth / 2, plot.Top, hitWidth, plot.Height), i));
            if (i == 0 || i == points.Count - 1 || i % Math.Max(1, (int)Math.Ceiling(points.Count / 6d)) == 0) Draw(g, points[i].Label, new Rectangle((int)p.X - S(40), plot.Bottom + S(8), S(80), S(28)), Ui.Muted, false, TextFormatFlags.HorizontalCenter);
        }
        using var focus = new Pen(Ui.Focus) { DashStyle = DashStyle.Dot }; var selected = positions[selection]; g.DrawLine(focus, selected.X, plot.Top, selected.X, plot.Bottom);
    }
    private void PaintDonut(Graphics g, int offset)
    {
        var total = points.Sum(x => x.Value); var diameter = Math.Min(S(180), Width / 3); var circle = new Rectangle(S(24), S(104) + offset, diameter, diameter);
        Color[] colors = [Ui.Primary, Ui.Success, Color.FromArgb(81,112,145), Ui.Warning, Ui.Danger, Color.FromArgb(130,120,143), Ui.Muted];
        float start = -90;
        for (var i = 0; i < points.Count; i++) { var sweep = total == 0 ? 0 : (float)(points[i].Value / total * 360); using var b = new SolidBrush(colors[i % colors.Length]); if (sweep > 0) g.FillPie(b, circle, start, sweep); start += sweep; }
        if (total == 0) { using var neutral = new SolidBrush(Ui.Line); g.FillEllipse(neutral, circle); }
        using (var hole = new SolidBrush(BackColor)) g.FillEllipse(hole, Rectangle.Inflate(circle, -diameter / 5, -diameter / 5));
        Draw(g, total.ToString("N0"), new Rectangle(circle.Left, circle.Top + diameter / 2 - S(22), diameter, S(28)), Ui.Ink, true, TextFormatFlags.HorizontalCenter);
        Draw(g, "đơn hàng", new Rectangle(circle.Left, circle.Top + diameter / 2 + S(6), diameter, S(24)), Ui.Muted, false, TextFormatFlags.HorizontalCenter);
        var left = circle.Right + S(24);
        for (var i = 0; i < points.Count; i++) { var y = S(90 + i * 40) + offset; var area = new Rectangle(left - S(8), y, Math.Max(1, Width - left - S(12)), S(36)); targets.Add((area,i)); if (i == selection) { using var bg = new SolidBrush(Ui.Soft); g.FillRectangle(bg,area); } using var b = new SolidBrush(colors[i % colors.Length]); g.FillEllipse(b,left,y+S(13),S(8),S(8)); Draw(g,points[i].Label,new Rectangle(left+S(18),y,Math.Max(1,Width-left-S(100)),S(36)),Ui.Ink); Draw(g,points[i].Value.ToString("N0"),new Rectangle(Width-S(78),y,S(52),S(36)),Ui.Ink,true,TextFormatFlags.Right); }
    }
    private static string Compact(decimal value) => Math.Abs(value) >= 1_000_000_000 ? (value / 1_000_000_000).ToString("0.#") + " tỷ" : Math.Abs(value) >= 1_000_000 ? (value / 1_000_000).ToString("0.#") + " triệu" : Math.Abs(value) >= 1000 ? (value / 1000).ToString("0.#") + " nghìn" : value.ToString("0.#");
}

internal static class InsightCards
{
    internal static TableLayoutPanel Metric(string caption, string value, string detail)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Ui.Surface, Padding = new Padding(18), ColumnCount = 1, RowCount = 3, Margin = new Padding(0,0,12,12) };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute,28));card.RowStyles.Add(new RowStyle(SizeType.Percent,100));card.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
        foreach (var label in new[] { Ui.Label(caption), Ui.Label(value,true), Ui.Label(detail) }) { label.Dock = DockStyle.Fill; label.AutoSize = false; label.AutoEllipsis=true;label.Margin=Padding.Empty;card.Controls.Add(label); }
        card.Controls[0].ForeColor = card.Controls[2].ForeColor = Ui.Muted; card.Controls[1].ForeColor = Ui.Primary; card.Controls[1].Font = new Font("Segoe UI Semibold",22);
        return card;
    }
    internal static decimal Number(System.Text.Json.JsonElement row,string key) => decimal.TryParse(Ui.Text(row,key),NumberStyles.Number,CultureInfo.InvariantCulture,out var value)?value:0;
}
