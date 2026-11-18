using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace BotZone.Admin.WinForms;

// Keep the native DataGridView combo contract: DisplayMember is a label, ValueMember
// is the raw ID, and the grid still owns commit/cancel and editing-control reuse.
internal sealed class BzGridComboBoxColumn : DataGridViewComboBoxColumn
{
    public BzGridComboBoxColumn() { CellTemplate = new BzGridComboBoxCell(); FlatStyle = FlatStyle.Flat; DefaultCellStyle.Padding = new Padding(1); }
}

internal sealed class BzGridComboBoxCell : DataGridViewComboBoxCell
{
    public override Type EditType => typeof(BzGridComboBoxEditingControl);

    protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds,
        int rowIndex, DataGridViewElementStates cellState, object? value, object? formattedValue,
        string? errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
        DataGridViewPaintParts paintParts)
    {
        var chrome = paintParts & (DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground | DataGridViewPaintParts.Border);
        base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, chrome);
        var editing = DataGridView is { IsCurrentCellInEditMode: true } activeGrid && activeGrid.CurrentCellAddress == new Point(ColumnIndex, rowIndex);
        if (!editing && (paintParts & DataGridViewPaintParts.ContentForeground) != 0 && DataGridView is { } grid)
        {
            var state = graphics.Save();
            try
            {
                graphics.SetClip(Rectangle.Intersect(clipBounds, cellBounds), CombineMode.Intersect);
                var selected = (cellState & DataGridViewElementStates.Selected) != 0;
                var focused = grid.ContainsFocus && grid.CurrentCellAddress == new Point(ColumnIndex, rowIndex);
                var bounds = cellBounds;
                if (!string.IsNullOrEmpty(errorText)) bounds.Width -= 20 * grid.DeviceDpi / 96;
                GridComboPaint.Draw(graphics, bounds, cellStyle.Font ?? grid.Font, Convert.ToString(formattedValue) ?? "",
                    selected ? cellStyle.SelectionBackColor : cellStyle.BackColor, grid.Enabled && !ReadOnly, focused, grid.DeviceDpi);
            }
            finally { graphics.Restore(state); }
        }
        if ((paintParts & DataGridViewPaintParts.ErrorIcon) != 0)
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, DataGridViewPaintParts.ErrorIcon);
    }
}

internal sealed class BzGridComboBoxEditingControl : DataGridViewComboBoxEditingControl
{
    private readonly ComboPopupChrome popup = new();
    private Color surrounding = Ui.Surface;

    public BzGridComboBoxEditingControl()
    {
        DrawMode = DrawMode.OwnerDrawFixed; FlatStyle = FlatStyle.Flat; IntegralHeight = false;
        BackColor = Ui.Surface; ForeColor = Ui.Ink;
        UpdateItemHeight();
    }

    public override void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.ApplyCellStyleToEditingControl(dataGridViewCellStyle);
        surrounding = dataGridViewCellStyle.SelectionBackColor;
        BackColor = Ui.Surface; ForeColor = Ui.Ink;
        DrawMode = DrawMode.OwnerDrawFixed; FlatStyle = FlatStyle.Flat;
        UpdateItemHeight();
    }

    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); UpdateItemHeight(); }
    private void UpdateItemHeight() => ItemHeight = Math.Max(28 * DeviceDpi / 96, Font.Height + 12 * DeviceDpi / 96);
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count) return;
        ComboPopupChrome.DrawItem(e, GetItemText(Items[e.Index]) ?? "", Font, DeviceDpi, Enabled);
    }

    protected override void OnDropDown(EventArgs e)
    {
        base.OnDropDown(e);
        // DataGridView's native DropDown handler also assigns width. Apply our
        // DPI-aware measurement after it, before the list window is shown.
        DropDownWidth = ComboPopupChrome.MeasureDropDownWidth(this);
        BeginInvoke((Action)(() => { if (!IsDisposed && IsHandleCreated && DroppedDown) popup.Apply(this); }));
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is 0xF or 0x317 or 0x318 && IsHandleCreated && DropDownStyle == ComboBoxStyle.DropDownList)
        {
            using var graphics = m.Msg != 0xF && m.WParam != IntPtr.Zero ? Graphics.FromHdc(m.WParam) : Graphics.FromHwnd(Handle);
            GridComboPaint.Draw(graphics, ClientRectangle, Font, Text, surrounding, Enabled, ContainsFocus, DeviceDpi);
        }
    }

    protected override void Dispose(bool disposing) { if (disposing) popup.Dispose(); base.Dispose(disposing); }
}

internal static class GridComboPaint
{
    internal static void Draw(Graphics graphics, Rectangle bounds, Font font, string text, Color surrounding, bool enabled, bool focused, int dpi)
    {
        if (bounds.Width < 8 || bounds.Height < 8) return;
        var scale = dpi / 96f;
        using var background = new SolidBrush(surrounding.IsEmpty ? Ui.Surface : surrounding);
        graphics.FillRectangle(background, bounds);
        var inset = Math.Max(2, (int)(3 * scale));
        var frame = Rectangle.Inflate(bounds, -inset, -inset);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var shape = FieldPaint.Shape(frame, (int)(8 * scale));
        using var fill = new SolidBrush(enabled ? Ui.Surface : Ui.Paper);
        graphics.FillPath(fill, shape);
        using var edge = new Pen(focused ? Ui.Focus : Ui.Line, focused ? 2 * scale : Math.Max(1, scale));
        graphics.DrawPath(edge, shape);
        var textBounds = new Rectangle(frame.Left + (int)(10 * scale), frame.Top, Math.Max(1, frame.Width - (int)(38 * scale)), frame.Height);
        TextRenderer.DrawText(graphics, text, font, textBounds, enabled ? Ui.Ink : Ui.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping);
        var x = frame.Right - 14 * scale; var y = frame.Top + frame.Height / 2f;
        using var glyph = new Pen(Ui.Muted, 1.3f * scale);
        graphics.DrawLines(glyph, [new PointF(x - 4 * scale, y - 2 * scale), new PointF(x, y + 2 * scale), new PointF(x + 4 * scale, y - 2 * scale)]);
    }
}

// Subclass only the native popup's paint. Selection, scrolling, keyboard input and
// cancellation remain owned by ComboBox; no per-edit event handlers are attached.
internal sealed class ComboPopupChrome : NativeWindow, IDisposable
{
    [StructLayout(LayoutKind.Sequential)] private struct Rect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct ComboInfo { internal int Size; internal Rect Item, Button; internal int State; internal IntPtr Combo, Edit, List; }
    [DllImport("user32.dll")] private static extern bool GetComboBoxInfo(IntPtr window, ref ComboInfo info);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int ellipseWidth, int ellipseHeight);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr region);
    [DllImport("user32.dll")] private static extern int SetWindowRgn(IntPtr window, IntPtr region, bool redraw);
    private int dpi = 96;
    private bool disposed;

    internal static int MeasureDropDownWidth(ComboBox combo)
    {
        using var graphics = combo.CreateGraphics();
        var content = combo.Items.Cast<object>().Select(item => TextRenderer.MeasureText(graphics,
            combo.GetItemText(item) ?? "", combo.Font, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width + 36 * combo.DeviceDpi / 96).DefaultIfEmpty(combo.Width).Max();
        return Math.Min(Screen.FromControl(combo).WorkingArea.Width, Math.Max(combo.Width, content));
    }

    internal void Apply(ComboBox combo)
    {
        if (disposed || combo.IsDisposed || !combo.IsHandleCreated || !combo.DroppedDown) return;
        var info = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() };
        if (!GetComboBoxInfo(combo.Handle, ref info) || info.List == IntPtr.Zero || !GetWindowRect(info.List, out var rect)) return;
        if (Handle != info.List) { ReleaseHandle(); AssignHandle(info.List); }
        dpi = combo.DeviceDpi;
        var diameter = 20 * dpi / 96;
        var region = CreateRoundRectRgn(0, 0, rect.Right - rect.Left + 1, rect.Bottom - rect.Top + 1, diameter, diameter);
        if (region != IntPtr.Zero && SetWindowRgn(info.List, region, true) == 0) DeleteObject(region);
        PaintBorder();
    }

    internal static void DrawItem(DrawItemEventArgs e, string text, Font font, int dpi, bool enabled)
    {
        var state = e.Graphics.Save();
        try
        {
        var scale = dpi / 96f;
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(Ui.Surface); e.Graphics.FillRectangle(background, e.Bounds);
        if (selected)
        {
            var bounds = Rectangle.Inflate(e.Bounds, -(int)(4 * scale), -(int)(2 * scale));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var shape = FieldPaint.Shape(bounds, (int)(8 * scale));
            using var fill = new SolidBrush(Ui.Soft); e.Graphics.FillPath(fill, shape);
            if ((e.State & DrawItemState.Focus) != 0) { using var edge = new Pen(Ui.Focus); e.Graphics.DrawPath(edge, shape); }
        }
        TextRenderer.DrawText(e.Graphics, text, font,
            new Rectangle(e.Bounds.X + (int)(12 * scale), e.Bounds.Y, Math.Max(1, e.Bounds.Width - (int)(24 * scale)), e.Bounds.Height),
            !enabled ? Ui.Muted : selected ? Ui.Primary : Ui.Ink,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping);
        }
        finally { e.Graphics.Restore(state); }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is 0x85 or 0xF) PaintBorder();
    }

    private void PaintBorder()
    {
        if (Handle == IntPtr.Zero || !GetWindowRect(Handle, out var rect)) return;
        var dc = GetWindowDC(Handle); if (dc == IntPtr.Zero) return;
        try
        {
            using var graphics = Graphics.FromHdc(dc); graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, rect.Right - rect.Left - 1, rect.Bottom - rect.Top - 1);
            if (bounds.Width < 2 || bounds.Height < 2) return;
            using var erase = new SolidBrush(Ui.Surface);
            graphics.FillRectangle(erase, 0, 0, bounds.Width + 1, 2);
            graphics.FillRectangle(erase, 0, bounds.Height - 1, bounds.Width + 1, 2);
            graphics.FillRectangle(erase, 0, 0, 2, bounds.Height + 1);
            graphics.FillRectangle(erase, bounds.Width - 1, 0, 2, bounds.Height + 1);
            using var shape = FieldPaint.Shape(bounds, 10 * dpi / 96);
            using var edge = new Pen(Ui.Line, Math.Max(1, dpi / 96f)); graphics.DrawPath(edge, shape);
        }
        finally { ReleaseDC(Handle, dc); }
    }

    public void Dispose() { if (disposed) return; disposed = true; ReleaseHandle(); }
}
