using System.Data;
using System.Globalization;
using System.Text.Json;

namespace BotZone.Admin.WinForms;

internal static partial class Ui
{
    public static readonly Color NavSurface=Color.White, NavMuted=ColorTranslator.FromHtml("#706C73");
    public static readonly Color Ink = ColorTranslator.FromHtml("#171719"), Paper = ColorTranslator.FromHtml("#F5F5F6"), Line = ColorTranslator.FromHtml("#E3E3E5"), Muted = ColorTranslator.FromHtml("#636367"), Danger = ColorTranslator.FromHtml("#A13035"), Primary = ColorTranslator.FromHtml("#6B263D"), Rail = ColorTranslator.FromHtml("#151517"), Soft = ColorTranslator.FromHtml("#F5EBEF"), Focus = ColorTranslator.FromHtml("#764355"), ControlBorder = ColorTranslator.FromHtml("#8A8C95"), Success = ColorTranslator.FromHtml("#246B4E"), Warning = ColorTranslator.FromHtml("#805714"), RailMuted = ColorTranslator.FromHtml("#B9BAC2"), Surface = Color.White;
    public static Button Button(string text, EventHandler? click = null, bool primary = false)
    {
        var b = new BzButton { Text = text, AccessibleName = text.Replace("&", ""), Primary = primary, AutoSize = true, MinimumSize = new Size(88, primary ? 40 : 36), Padding = new Padding(12, 4, 12, 4), Margin = new Padding(0, 0, 8, 6), Cursor = Cursors.Hand };
        if (click is not null) b.Click += click;
        return b;
    }
    public static Label Label(string text, bool heading = false) => new() { Text = text, UseMnemonic = false, AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", heading ? 20 : 10.5f, heading ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0, 0, 0, 8), MaximumSize = new Size(1500, 0) };
    public static FlowLayoutPanel Bar(params Control[] controls)
    {
        var p = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, WrapContents = true, Padding = new Padding(0, 4, 0, 2), Margin = Padding.Empty };
        foreach(var button in controls.OfType<Button>())button.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;
        p.Controls.AddRange(controls); return p;
    }
    public static DataGridView Grid() => CreateGrid();
    public static void Column(DataGridView grid, string property, string label, int width = 120)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = property, Name = property, HeaderText = label, MinimumWidth = width, SortMode = DataGridViewColumnSortMode.Programmatic });
    }
    public static ComboBox Combo(params Choice[] choices)
    {
        var c = new BzComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, DisplayMember = nameof(Choice.Label), ValueMember = nameof(Choice.Value), Margin = new Padding(0, 0, 8, 6) };
        c.Items.AddRange(choices.Cast<object>().ToArray()); if (c.Items.Count > 0) c.SelectedIndex = 0; return c;
    }
    public static string Selected(ComboBox c) => (c.SelectedItem as Choice)?.Value ?? "";
    public static string Money(string value) => decimal.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫" : value;
    public static string VietDate(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm") ?? "Chưa phát hành";
    public static string Error(Exception ex) => ex is OperationCanceledException ? "Đã hủy hoặc yêu cầu quá thời gian." : ex is HttpRequestException ? "Không kết nối được API. Kiểm tra dịch vụ, giữ nguyên dữ liệu và thử lại." : ex.Message;
    public static void ShowError(IWin32Window? owner, Exception ex) => MessageBox.Show(owner, Error(ex), "BotZone", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    public static string Text(JsonElement row, params string[] names)
    {
        foreach (var name in names) if (row.ValueKind == JsonValueKind.Object && row.TryGetProperty(name, out var value)) return value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? "" : value.ToString();
        return "";
    }
    public static List<JsonElement> Rows(JsonElement data, string property = "rows") => data.ValueKind == JsonValueKind.Array ? data.EnumerateArray().Select(x => x.Clone()).ToList() : data.TryGetProperty(property, out var rows) && rows.ValueKind == JsonValueKind.Array ? rows.EnumerateArray().Select(x => x.Clone()).ToList() : [];
    public static DataTable Table(IEnumerable<JsonElement> rows)
    {
        var items = rows.ToList(); var table = new DataTable();
        foreach (var row in items) foreach (var prop in row.EnumerateObject()) if (!table.Columns.Contains(prop.Name)) table.Columns.Add(prop.Name);
        foreach (var row in items) { var r = table.NewRow(); foreach (var prop in row.EnumerateObject()) r[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? "" : prop.Value.ToString(); table.Rows.Add(r); }
        return table;
    }
    public static TableLayoutPanel Vertical(params Control[] controls)
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = controls.Length, Padding = new Padding(24), BackColor = Paper, Margin = Padding.Empty };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < controls.Length; i++) { p.RowStyles.Add(new RowStyle(i == controls.Length - 1 ? SizeType.Percent : SizeType.AutoSize, 100)); controls[i].Dock = DockStyle.Fill; p.Controls.Add(controls[i], 0, i); }
        return p;
    }
}

internal sealed record Choice(string Value, string Label) { public override string ToString() => Label; }

internal abstract class PageView(ApiClient api) : UserControl
{
    protected readonly ApiClient Api = api;
    protected readonly Label Status = new BusyLabel { AutoSize = true, Text = "Sẵn sàng", ForeColor = Ui.Muted, Padding = new Padding(20, 5, 0, 8), AccessibleName = "Trạng thái dữ liệu", AccessibleRole = AccessibleRole.StatusBar };
    private CancellationTokenSource? pending;
    private long generation;
    internal bool HasLoaded { get; private set; }
    internal string StatusMessage => Status.Text;
    internal bool HasLoadError => Status.ForeColor == Ui.Danger;
    public virtual Task RefreshAsync() => Task.CompletedTask;
    internal virtual bool CanLeave()=>true;
    internal virtual bool HasPendingWrite=>false;
    internal void Deactivate() { if(IsDisposed)return; ++generation; pending?.Cancel(); pending?.Dispose(); pending = null; if (Status is BusyLabel busy) { if(busy.IsBusy)Status.Text=HasLoaded?"Đã dừng cập nhật. Dữ liệu trước đó được giữ; F5 để tải lại.":"Chưa tải xong dữ liệu."; busy.IsBusy = false; } }
    protected async Task LoadLatestAsync<T>(Func<CancellationToken, Task<T>> fetch, Action<T> apply)
    {
        if(IsDisposed || Disposing || (IsHandleCreated && !Visible))return;
        using var actionBusy = Ui.FocusedButton(this) is Control action ? Ui.Busy(action,"Đang tải…") : null;
        pending?.Cancel(); pending?.Dispose(); pending = new CancellationTokenSource();
        var token = pending.Token; var current = ++generation; Status.Text = HasLoaded ? "Đang cập nhật… Dữ liệu hiện tại được giữ lại." : "Đang tải dữ liệu…"; Status.ForeColor = Ui.Muted;
        if (Status is BusyLabel busy) busy.IsBusy = true;
        try { var data = await fetch(token); if (current != generation || IsDisposed || Disposing) return; apply(data); HasLoaded = true; Ui.Enhance(this); }
        catch (OperationCanceledException) { if (current == generation && !IsDisposed) Status.Text = "Đã hủy yêu cầu. Chọn Tải lại để thử tiếp."; }
        catch (Exception ex) { if (current == generation && !IsDisposed) { Status.Text = Ui.Error(ex); Status.ForeColor = Ui.Danger; } }
        finally { if (current == generation && !IsDisposed && Status is BusyLabel feedback) feedback.IsBusy = false; }
    }
    protected override void Dispose(bool disposing) { if (disposing) Deactivate(); base.Dispose(disposing); }
    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if(!Visible)Deactivate(); }
}
