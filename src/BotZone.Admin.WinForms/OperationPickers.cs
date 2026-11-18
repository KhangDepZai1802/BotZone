using System.Globalization;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace BotZone.Admin.WinForms;

// Choices remain native controls. Filtering changes the visible subset, never
// the IDs already chosen for the command.
internal sealed class MultiLookupPicker : UserControl
{
    private readonly List<Choice> all;
    private readonly HashSet<string> selected = new(StringComparer.Ordinal);
    private readonly TextBox search = new() { Dock = DockStyle.Fill, PlaceholderText = "Tìm theo tên hoặc mã…", AccessibleName = "Tìm trong danh sách lựa chọn" };
    private readonly CheckedListBox list = new OperationCheckedList { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.None, IntegralHeight = false, DisplayMember = nameof(Choice.Label), AccessibleName = "Các mục có thể chọn" };
    private readonly Label summary = Ui.Label("");
    private readonly CheckBox selectedOnly = new() { Text = "Chỉ mục đã chọn", AutoSize = true };
    private readonly Button clear;
    private bool rebuilding;
    internal event EventHandler? Changed;
    internal string[] SelectedValues => all.Where(x => selected.Contains(x.Value)).Select(x => x.Value).ToArray();

    internal MultiLookupPicker(IEnumerable<Choice> choices, IEnumerable<string>? initial = null)
    {
        all = choices.ToList(); Height = 270; MinimumSize = new Size(240, 220); BackColor = Ui.Surface;
        foreach (var id in initial ?? []) if (all.Any(x => x.Value == id)) selected.Add(id);
        clear = Ui.Button("Bỏ chọn", (_, _) => { selected.Clear(); Rebuild(); Changed?.Invoke(this, EventArgs.Empty); });
        clear.Name = "clearSelection";
        var options = Ui.Bar(selectedOnly, clear); options.BackColor = Ui.Surface;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var searchFrame = Ui.FrameInput(search); searchFrame.Dock = DockStyle.Fill;
        layout.Controls.Add(searchFrame, 0, 0); layout.Controls.Add(options, 0, 1); layout.Controls.Add(list, 0, 2); layout.Controls.Add(summary, 0, 3); Controls.Add(layout);
        search.TextChanged += (_, _) => Rebuild(); selectedOnly.CheckedChanged += (_, _) => Rebuild();
        list.ItemCheck += (_, e) =>
        {
            if (rebuilding) return;
            var item = (Choice)list.Items[e.Index]; if (e.NewValue == CheckState.Checked) selected.Add(item.Value); else selected.Remove(item.Value);
            UpdateSummary(); Changed?.Invoke(this, EventArgs.Empty);
            if (selectedOnly.Checked && IsHandleCreated) BeginInvoke((Action)(() => { if (!IsDisposed) Rebuild(); }));
        };
        Rebuild();
    }
    private void Rebuild()
    {
        var current = (list.SelectedItem as Choice)?.Value;
        rebuilding = true; list.BeginUpdate(); list.Items.Clear();
        foreach (var choice in all.Where(x => (!selectedOnly.Checked || selected.Contains(x.Value)) && OperationChoiceSearch.Matches(x.Label, search.Text)))
        {
            var index = list.Items.Add(choice, selected.Contains(choice.Value)); if (choice.Value == current) list.SelectedIndex = index;
        }
        if (list.SelectedIndex < 0 && list.Items.Count > 0) list.SelectedIndex = 0;
        list.EndUpdate(); rebuilding = false; UpdateSummary();
    }
    private void UpdateSummary()
    {
        summary.ForeColor = Ui.Muted;
        summary.Text = list.Items.Count == 0 ? $"Đã chọn {selected.Count} · Không khớp. Thử tên hoặc mã khác." : $"Đã chọn {selected.Count} · Hiện {list.Items.Count}/{all.Count}";
        clear.Enabled = selected.Count > 0;
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F)) { search.Focus(); search.SelectAll(); return true; }
        if (keyData == Keys.Enter) { if (list.Focused && list.SelectedIndex >= 0) list.SetItemChecked(list.SelectedIndex, !list.GetItemChecked(list.SelectedIndex)); else list.Focus(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}

internal static class OperationChoiceSearch
{
    internal static bool Matches(string label, string query) => CultureInfo.GetCultureInfo("vi-VN").CompareInfo.IndexOf(label, query.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
}

internal sealed class OperationCheckedList : CheckedListBox
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override int ItemHeight { get => Math.Max(base.ItemHeight, 32 * DeviceDpi / 96); set { } }
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        base.OnDrawItem(e); if(e.Index < 0 || e.Index >= Items.Count) return;
        var scale = DeviceDpi / 96f; var active = (e.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(Ui.Surface); e.Graphics.FillRectangle(background,e.Bounds);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var rowPath = FieldPaint.Shape(Rectangle.Inflate(e.Bounds,-2,-1), (int)(6*scale));
        if(active){using var highlight = new SolidBrush(Ui.Soft); e.Graphics.FillPath(highlight,rowPath);}
        var size=(int)(16*scale); var box=new Rectangle(e.Bounds.Left+(int)(8*scale),e.Bounds.Top+(e.Bounds.Height-size)/2,size,size);
        using var boxPath=FieldPaint.Shape(box,(int)(3*scale)); var state=GetItemCheckState(e.Index);
        using var boxFill=new SolidBrush(state==CheckState.Unchecked?Ui.Surface:Ui.Primary);e.Graphics.FillPath(boxFill,boxPath);
        using var outline=new Pen(state==CheckState.Unchecked?Ui.ControlBorder:Ui.Primary,1);e.Graphics.DrawPath(outline,boxPath);
        if(state!=CheckState.Unchecked){using var tick=new Pen(Color.White,1.6f*scale);if(state==CheckState.Indeterminate)e.Graphics.DrawLine(tick,box.Left+4*scale,box.Top+8*scale,box.Right-4*scale,box.Top+8*scale);else e.Graphics.DrawLines(tick,[new PointF(box.Left+3*scale,box.Top+8*scale),new PointF(box.Left+7*scale,box.Top+12*scale),new PointF(box.Left+13*scale,box.Top+4*scale)]);}
        var text=new Rectangle(box.Right+(int)(10*scale),e.Bounds.Top,Math.Max(1,e.Bounds.Right-box.Right-(int)(18*scale)),e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics,GetItemText(Items[e.Index]),Font,text,Enabled?Ui.Ink:Ui.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        if((e.State&DrawItemState.Focus)!=0){using var focus=new Pen(Ui.Focus);e.Graphics.DrawPath(focus,rowPath);}
    }
}

internal sealed class OperationChoiceForm : Form
{
    private readonly List<Choice> all;
    private readonly TextBox search = new() { Dock = DockStyle.Fill, PlaceholderText = "Tìm theo tên hoặc mã…", AccessibleName = "Tìm món hàng" };
    private readonly ListBox list = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, IntegralHeight = false, DisplayMember = nameof(Choice.Label), AccessibleName = "Kết quả tìm món hàng", DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 40 };
    private readonly Label count = Ui.Label("");
    private readonly Button choose;
    internal Choice? Selected => list.SelectedItem as Choice;
    internal OperationChoiceForm(string title, IEnumerable<Choice> choices)
    {
        all = choices.ToList(); Text = title; Size = new Size(680, 540); MinimumSize = new Size(440, 380); StartPosition = FormStartPosition.CenterParent;
        choose = Ui.Button("Chọn món", (_, _) => Confirm(), true); var cancel = Ui.Button("Hủy", (_, _) => Close()); AcceptButton = choose; CancelButton = cancel;
        var actions=Ui.Bar(choose,cancel);actions.WrapContents=false;actions.FlowDirection=FlowDirection.RightToLeft;actions.Dock=DockStyle.Fill;
        var footer = new TableLayoutPanel { Dock=DockStyle.Fill, AutoSize=true, ColumnCount=2, RowCount=1, Margin=Padding.Empty };footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));count.Anchor=AnchorStyles.Left;footer.Controls.Add(count,0,0);footer.Controls.Add(actions,1,0);
        footer.Layout+=(_,_)=>{var width=Math.Max(40,footer.ClientSize.Width-actions.GetPreferredSize(Size.Empty).Width-count.Margin.Horizontal-footer.Padding.Horizontal);if(count.MaximumSize.Width!=width)count.MaximumSize=new Size(width,0);};
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(20), BackColor = Ui.Paper };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var searchFrame = Ui.FrameInput(search); searchFrame.Dock = DockStyle.Fill;
        layout.Controls.Add(Ui.PageHeader(title, ""), 0, 0); layout.Controls.Add(searchFrame, 0, 1); layout.Controls.Add(list, 0, 2); layout.Controls.Add(footer, 0, 3); Controls.Add(layout);
        list.DrawItem += (_, e) => { if (e.Index < 0) return; var active = (e.State & DrawItemState.Selected) != 0; using var brush = new SolidBrush(active ? Ui.Soft : Ui.Surface); e.Graphics.FillRectangle(brush, e.Bounds); var bounds = Rectangle.Inflate(e.Bounds, -12, 0); TextRenderer.DrawText(e.Graphics, ((Choice)list.Items[e.Index]).Label, Font, bounds, active ? Ui.Primary : Ui.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); if ((e.State & DrawItemState.Focus) != 0) e.DrawFocusRectangle(); };
        list.DoubleClick += (_, _) => Confirm(); search.TextChanged += (_, _) => Rebuild(); search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Down && list.Items.Count > 0) { e.SuppressKeyPress = true; list.Focus(); } }; Shown += (_, _) => search.Focus(); Ui.Prepare(this); Rebuild();
    }
    private void Rebuild()
    {
        var previous = Selected?.Value; list.BeginUpdate(); list.Items.Clear(); foreach (var item in all.Where(x => OperationChoiceSearch.Matches(x.Label, search.Text))) { var index = list.Items.Add(item); if (item.Value == previous) list.SelectedIndex = index; } if (list.SelectedIndex < 0 && list.Items.Count > 0) list.SelectedIndex = 0; list.EndUpdate();
        choose.Enabled = list.Items.Count > 0; count.ForeColor = Ui.Muted; count.Text = list.Items.Count > 0 ? $"{list.Items.Count} mục" : "Không khớp. Thử tên hoặc mã khác.";
    }
    private void Confirm() { if (Selected is null) return; DialogResult = DialogResult.OK; Close(); }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { if (keyData == (Keys.Control | Keys.F)) { search.Focus(); search.SelectAll(); return true; } return base.ProcessCmdKey(ref msg, keyData); }
}
