using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class InvoicesView : PageView
{
    private readonly DataGridView grid = Ui.Grid();
    private readonly BindingSource binding = new();
    private readonly TextBox search = new() { Width = 270, PlaceholderText = "Mã hóa đơn hoặc tên khách hàng", AccessibleName = "Tìm hóa đơn" };
    private readonly ComboBox lifecycle = Ui.Combo(new("issued", "Đã phát hành"), new("draft", "Bản nháp"));
    private readonly ComboBox pageSize = Ui.Combo(new("20", "20 dòng"), new("50", "50 dòng"), new("100", "100 dòng"));
    private readonly Label totals = Ui.Label("Tổng HĐ của toàn bộ kết quả: —");
    private readonly Label count = Ui.Label("—");
    private readonly Label filterSummary = Ui.Label("Tất cả chứng từ trong phạm vi được phép.");
    private readonly Label pageLabel = Ui.Label("Chưa có dữ liệu");
    private readonly Button previous, next, export, filter;
    private readonly System.Windows.Forms.Timer debounce = new() { Interval = 350 };
    private InvoiceQuery query;
    private InvoiceQuery? loadedQuery;
    private InvoiceListResult? result;
    private bool opening, applyingView;
    public InvoiceListResult? CurrentResult => result;

    public InvoicesView(ApiClient api, InvoiceQuery? initial = null) : base(api)
    {
        query = initial ?? new InvoiceQuery { Lifecycle = "issued" }; Dock = DockStyle.Fill;
        search.Text = query.QuickSearch ?? ""; lifecycle.SelectedIndex = query.Lifecycle == "draft" ? 1 : 0;
        pageSize.Width = 160; pageSize.SelectedIndex = query.PageSize == 100 ? 2 : query.PageSize == 50 ? 1 : 0;
        foreach (var column in new[] { ("InvoiceNo", "Mã hóa đơn", 150), ("IssuedAt", "Phát hành (VN)", 160), ("CustomerName", "Khách hàng", 170), ("StaffName", "Phụ trách", 145), ("GrandTotalVnd", "Tổng HĐ", 145), ("NetVnd", "Ròng HĐ", 145), ("PaymentStatus", "Thanh toán", 150), ("CreditNoteTotalVnd", "Giảm lũy kế", 145), ("ItemCount", "Số món", 80), ("SubtotalVnd", "Trước giảm", 145), ("DiscountTotalVnd", "Giảm giá", 135) }) Ui.Column(grid, column.Item1, column.Item2, column.Item3);
        foreach(DataGridViewColumn column in grid.Columns){column.Visible=column.Name is "InvoiceNo" or "IssuedAt" or "CustomerName" or "GrandTotalVnd" or "PaymentStatus";column.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;column.FillWeight=column.Name=="CustomerName"?30:column.Name=="GrandTotalVnd"?20:16;}
        grid.Columns["InvoiceNo"]!.AutoSizeMode=DataGridViewAutoSizeColumnMode.None;grid.Columns["InvoiceNo"]!.Width=150;grid.Columns["InvoiceNo"]!.Frozen = true; grid.AccessibleName = "Danh sách hóa đơn. Enter mở chi tiết."; grid.DataSource = binding;
        grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0) return;
            if (e.Value is string s && grid.Columns[e.ColumnIndex].DataPropertyName.EndsWith("Vnd")) { e.Value = Ui.Money(s); e.FormattingApplied = true; }
            if (e.Value is DateTimeOffset d) { e.Value = Ui.VietDate(d); e.FormattingApplied = true; }
            if (grid.Columns[e.ColumnIndex].DataPropertyName == "PaymentStatus" && e.Value is string state)
            { e.Value = state switch { "paid" => "Đã thu đủ", "partially_paid" => "Thu một phần", "refunded" => "Đã hoàn", "partially_refunded" => "Hoàn một phần", "unpaid" => "Chưa thu", _ => state }; e.FormattingApplied = true; }
        };
        grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await DetailAsync(); };
        grid.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = e.SuppressKeyPress = true; await DetailAsync(); } };
        grid.ColumnHeaderMouseClick += async (_, e) =>
        {
            var name = grid.Columns[e.ColumnIndex].DataPropertyName switch { "IssuedAt" => "issuedAt", "InvoiceNo" => "invoiceNo", "GrandTotalVnd" => "grandTotalVnd", _ => null }; if (name is null) return;
            var direction = query.Sort.FirstOrDefault() is { } current && current.Field == name && current.Direction == "asc" ? "desc" : "asc";
            query = query with { Page = 1, Sort = [new(name, direction)] }; await RefreshAsync();
        };
        filter = Ui.Button("&Bộ lọc", async (_, _) => await FilterAsync());
        export = Ui.Button("&Xuất danh sách", async (_, _) => await ExportAsync(), true); export.Enabled = false;
        var actions=Ui.MenuButton("Thao tác",("Mở hóa đơn đã chọn",async()=>await DetailAsync()),("Cột hiển thị và chế độ xem",async()=>await SavedViewsAsync()),("Xóa bộ lọc",async()=>{debounce.Stop();search.Text="";debounce.Stop();query=new InvoiceQuery{Lifecycle=Ui.Selected(lifecycle),PageSize=query.PageSize};await RefreshAsync();}),("Làm mới",async()=>await RefreshAsync()),("Thông tin danh sách",()=>Ui.ShowInformation(FindForm(),"Thông tin danh sách",filterSummary.Text+(result is null?"":$"\n\nDữ liệu đọc lúc {Ui.VietDate(result.ReadAt)}.\nTổng tiền tính trên toàn bộ kết quả, không giới hạn ở trang hiện tại."))));
        previous = Ui.Button("‹ Trang trước", async (_, _) => { query = query with { Page = Math.Max(1, query.Page - 1) }; await RefreshAsync(); });
        next = Ui.Button("Trang sau ›", async (_, _) => { query = query with { Page = query.Page + 1 }; await RefreshAsync(); });
        totals.Font = new Font("Segoe UI", 18, FontStyle.Bold);totals.ForeColor=Ui.Primary;count.Font=new Font("Segoe UI",18,FontStyle.Bold);totals.Text="—";totals.Margin=count.Margin=Padding.Empty;
        var summary=new TableLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top,ColumnCount=4,RowCount=1,BackColor=Color.White,Padding=new Padding(16,8,16,8),Margin=new Padding(0,4,0,8)};
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));summary.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));summary.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));summary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var countCaption=Ui.Label("hóa đơn khớp bộ lọc");countCaption.ForeColor=Ui.Muted;var totalCaption=Ui.Label("Tổng khớp bộ lọc");totalCaption.ForeColor=Ui.Muted;
        count.Anchor=totals.Anchor=countCaption.Anchor=totalCaption.Anchor=AnchorStyles.Left;count.Margin=new Padding(0,0,8,0);countCaption.Margin=Padding.Empty;totalCaption.Margin=new Padding(12,0,12,0);summary.Controls.Add(count,0,0);summary.Controls.Add(countCaption,1,0);summary.Controls.Add(totalCaption,2,0);summary.Controls.Add(totals,3,0);
        Status.Font=new Font("Segoe UI",9.5f);Status.Padding=new Padding(12,0,0,0);Status.Margin=new Padding(0,3,0,4);
        filterSummary.ForeColor = Ui.Muted; filterSummary.MaximumSize = new Size(1000, 0);
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Margin = Padding.Empty }; body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 6; i++) body.RowStyles.Add(new RowStyle(i == 4 ? SizeType.Percent : SizeType.AutoSize, i == 4 ? 100 : 0));
        body.Controls.Add(Ui.PageHeader("Hóa đơn", api.Actor?.Role=="order_staff"?"Dữ liệu của tôi":"", export, actions), 0, 0);
        lifecycle.Width=190;search.Width=240;
        var filters=Ui.Bar(Ui.Field("Tìm chứng từ", search), Ui.Field("Vòng đời", lifecycle), filter);filters.BackColor=Color.White;filters.Padding=new Padding(12,8,12,4);body.Controls.Add(filters,0,1);
        body.Controls.Add(summary, 0, 2); body.Controls.Add(Status, 0, 3); body.Controls.Add(grid, 0, 4); body.Controls.Add(Ui.Bar(previous, pageLabel, next, Ui.FrameInput(pageSize)), 0, 5); Controls.Add(body);
        Status.TextChanged+=(_,_)=>Status.Visible=!string.IsNullOrWhiteSpace(Status.Text);
        search.TextChanged += (_, _) => { if(applyingView)return; Deactivate(); query = query with { Page = 1, QuickSearch = search.Text.Trim() }; debounce.Stop(); debounce.Start(); };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = e.SuppressKeyPress = true; debounce.Stop(); await RefreshAsync(); } };
        debounce.Tick += async (_, _) => { debounce.Stop(); await RefreshAsync(); };
        lifecycle.SelectedIndexChanged += async (_, _) => { if(applyingView)return; query = query with { Page = 1, Lifecycle = Ui.Selected(lifecycle) }; await RefreshAsync(); };
        pageSize.SelectedIndexChanged += async (_, _) => { if(applyingView)return; query = query with { Page = 1, PageSize = int.Parse(Ui.Selected(pageSize), CultureInfo.InvariantCulture) }; await RefreshAsync(); };
        VisibleChanged += (_, _) => { if (!Visible) debounce.Stop(); };
    }

    public override Task RefreshAsync()
    {
        var request = query;
        return LoadLatestAsync(token => Api.PostAsync<InvoiceListResult>("api/admin/invoices/search", request, token), data =>
        {
            var selectedId = (binding.Current as InvoiceListItemDto)?.Id; var scroll = grid.FirstDisplayedScrollingRowIndex; var horizontal = grid.HorizontalScrollingOffset; var samePage = loadedQuery?.Page == data.Page;
            result = data; loadedQuery = request; binding.DataSource = data.Items;
            if (samePage && selectedId is not null)
            {
                var row = data.Items.FindIndex(item => item.Id == selectedId);
                if (row >= 0) { binding.Position = row; grid.Rows[row].Selected = true; }
                if (scroll >= 0 && scroll < grid.Rows.Count) grid.FirstDisplayedScrollingRowIndex = scroll;
                grid.HorizontalScrollingOffset = horizontal;
            }
            count.Text=$"{data.MatchedCount:N0}";totals.Text=Ui.Money(data.MatchedTotalVnd);
            filterSummary.Text = FilterDialog.Describe(request.Filter) + (string.IsNullOrWhiteSpace(request.QuickSearch) ? "" : $" · Tìm: {request.QuickSearch}") + (request.FromInclusive is null ? "" : $" · Kỳ: {request.FromInclusive} → trước {request.ToExclusive}");
            var first = data.Items.Count == 0 ? 0 : (long)(data.Page - 1) * data.PageSize + 1; var last = data.Items.Count == 0 ? 0 : first + data.Items.Count - 1;
            pageLabel.Text = $"{first:N0}–{last:N0} / {data.MatchedCount:N0} · Trang {data.Page}"; previous.Enabled = data.Page > 1; next.Enabled = (long)data.Page * data.PageSize < data.MatchedCount;
            export.Enabled = true;filter.Text=request.Filter is null&&request.FromInclusive is null?"&Bộ lọc":"&Bộ lọc · Đang dùng";
            Status.Text = data.Items.Count == 0 ? "Không có hóa đơn phù hợp. Đổi điều kiện hoặc xóa bộ lọc." : "";
            foreach (DataGridViewColumn column in grid.Columns) column.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (request.Sort.FirstOrDefault() is { } sort) grid.Columns[sort.Field switch { "invoiceNo" => "InvoiceNo", "grandTotalVnd" => "GrandTotalVnd", _ => "IssuedAt" }]!.HeaderCell.SortGlyphDirection = sort.Direction == "asc" ? SortOrder.Ascending : SortOrder.Descending;
        });
    }
    private async Task FilterAsync()
    {
        using var busy = Ui.Busy(filter, "Đang tải lựa chọn…");
        try { var lookups = await Api.GetAsync<JsonElement>("api/admin/lookups"); if (IsDisposed) return; using var dialog = new FilterDialog(query.Filter, lookups); if (await dialog.ShowDialogAsync(FindForm() ?? (IWin32Window)this) == DialogResult.OK && !IsDisposed) { query = query with { Filter = dialog.Filter, Page = 1 }; await RefreshAsync(); } }
        catch (Exception ex) { if (!IsDisposed) { Status.Text = Ui.Error(ex); Status.ForeColor = Ui.Danger; } }
    }
    private async Task DetailAsync()
    {
        if (opening || binding.Current is not InvoiceListItemDto row) return; opening = true;
        try { using var form = new InvoiceDetailForm(Api, row.Id); await form.ShowDialogAsync(FindForm() ?? (IWin32Window)this); } finally { opening = false; }
    }
    private async Task ExportAsync()
    {
        if (loadedQuery is null || result is null) return;
        using var busy = Ui.Busy(export, "Đang xuất file…");
        await ExportFlow.RunAsync(FindForm()!, Api, new ExportRequest { Query = loadedQuery, Formats = ["xlsx", "pdf"], Kind = "list" }, $"Toàn bộ kết quả đang hiển thị: {result.MatchedCount:N0} hóa đơn · {Ui.Money(result.MatchedTotalVnd)}. Không giới hạn ở trang hiện tại.");
    }
    private async Task SavedViewsAsync()
    {
        using var dialog=new InvoiceViewsDialog(Api.Actor?.Id??"anonymous",query,grid);
        if(await dialog.ShowDialogAsync(FindForm()??(IWin32Window)this)!=DialogResult.OK||IsDisposed)return;
        foreach(DataGridViewColumn column in grid.Columns)column.Visible=column.Name=="InvoiceNo"||!dialog.HiddenColumns.Contains(column.Name);
        if(dialog.SelectedQuery is null)return;
        applyingView=true;
        try{query=dialog.SelectedQuery;search.Text=query.QuickSearch??"";lifecycle.SelectedIndex=query.Lifecycle=="draft"?1:0;pageSize.SelectedIndex=query.PageSize==100?2:query.PageSize==50?1:0;debounce.Stop();}
        finally{applyingView=false;}
        await RefreshAsync();
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { if (keyData == (Keys.Control | Keys.F)) { search.Focus(); search.SelectAll(); return true; } return base.ProcessCmdKey(ref msg, keyData); }
    protected override void Dispose(bool disposing) { if (disposing) { filterSummary.Dispose(); debounce.Dispose(); binding.Dispose(); } base.Dispose(disposing); }
}

internal sealed class InvoiceViewsDialog : Form
{
    private readonly ListBox views=new(){Dock=DockStyle.Fill,DisplayMember=nameof(SavedView.Name),AccessibleName="Chế độ xem đã lưu",BorderStyle=BorderStyle.None};
    private readonly TextBox name=new(){Dock=DockStyle.Top,MaxLength=64,AccessibleName="Tên chế độ xem"};
    private readonly CheckedListBox columns=new(){Dock=DockStyle.Fill,CheckOnClick=true,AccessibleName="Các cột hiển thị",BorderStyle=BorderStyle.None};
    private readonly Label status=Ui.Label("Lưu bộ lọc, sắp xếp, kích thước trang và cột trên máy này, riêng cho tài khoản của bạn.");
    private readonly List<SavedView> saved=[];
    private readonly string filename;
    private readonly InvoiceQuery current;
    private readonly List<string> columnNames=[];
    internal InvoiceQuery? SelectedQuery{get;private set;}
    internal List<string> HiddenColumns{get;private set;}=[];
    private sealed record SavedView(string Name,InvoiceQuery Query,List<string> HiddenColumns);
    internal InvoiceViewsDialog(string actorId,InvoiceQuery current,DataGridView grid)
    {
        this.current=current;
        var account=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(actorId)))[..24];
        filename=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BotZone","views","invoices-"+account+".json");
        Text="Chế độ xem hóa đơn";Size=new Size(780,650);MinimumSize=new Size(620,460);StartPosition=FormStartPosition.CenterParent;
        foreach(DataGridViewColumn column in grid.Columns){columnNames.Add(column.Name);columns.Items.Add(column.HeaderText,column.Visible);}
        columns.ItemCheck+=(_,e)=>{if(columnNames[e.Index]=="InvoiceNo")e.NewValue=CheckState.Checked;};
        var apply=Ui.Button("Áp dụng",(_,_)=>{SelectedQuery=(views.SelectedItem as SavedView)?.Query;HiddenColumns=GetHidden();DialogResult=DialogResult.OK;Close();},true);
        Button? save=null;save=Ui.Button("Lưu chế độ hiện tại",async(_,_)=>{if(string.IsNullOrWhiteSpace(name.Text)){status.Text="Nhập tên cho chế độ xem.";status.ForeColor=Ui.Danger;name.Focus();return;}using var busy=Ui.Busy(save!,"Đang lưu…");try{var label=name.Text.Trim();var existing=saved.FindIndex(v=>v.Name.Equals(label,StringComparison.CurrentCultureIgnoreCase));var item=new SavedView(label,current with { Page=1 },GetHidden());if(existing>=0)saved[existing]=item;else saved.Add(item);await WriteAsync();if(IsDisposed)return;Bind();status.ForeColor=Ui.Muted;status.Text="Đã lưu chế độ xem trên máy này.";}catch(Exception ex){if(!IsDisposed){status.Text=Ui.Error(ex);status.ForeColor=Ui.Danger;}}});
        var remove=Ui.Button("Xóa chế độ đã chọn",async(_,_)=>{if(views.SelectedItem is not SavedView selected)return;try{saved.Remove(selected);await WriteAsync();if(!IsDisposed)Bind();}catch(Exception ex){if(!IsDisposed){status.Text=Ui.Error(ex);status.ForeColor=Ui.Danger;}}});
        var close=Ui.Button("Hủy",(_,_)=>Close());AcceptButton=apply;CancelButton=close;
        var choices=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2};choices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));choices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));choices.RowStyles.Add(new RowStyle(SizeType.AutoSize));choices.RowStyles.Add(new RowStyle(SizeType.Percent,100));choices.Controls.Add(Ui.Label("Chế độ đã lưu"),0,0);choices.Controls.Add(Ui.Label("Cột hiển thị"),1,0);choices.Controls.Add(views,0,1);choices.Controls.Add(columns,1,1);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(20)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));for(var i=2;i<5;i++)layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.Controls.Add(Ui.PageHeader("Chế độ xem","Quyền dữ liệu luôn được API kiểm tra lại."),0,0);layout.Controls.Add(choices,0,1);layout.Controls.Add(Ui.Field("Tên chế độ mới / thay thế",name),0,2);layout.Controls.Add(status,0,3);layout.Controls.Add(Ui.Bar(apply,save,remove,close),0,4);Controls.Add(layout);Ui.Prepare(this);
        views.SelectedIndexChanged+=(_,_)=>{if(views.SelectedItem is not SavedView selected)return;name.Text=selected.Name;for(var i=0;i<columnNames.Count;i++)columns.SetItemChecked(i,!selected.HiddenColumns.Contains(columnNames[i]));};
        Shown+=async(_,_)=>{try{if(File.Exists(filename)){var items=JsonSerializer.Deserialize<List<SavedView>>(await File.ReadAllTextAsync(filename),ApiClient.Json);if(IsDisposed)return;if(items is not null)saved.AddRange(items);Bind();}}catch(Exception ex){if(!IsDisposed){status.Text="Chưa đọc được chế độ đã lưu: "+Ui.Error(ex);status.ForeColor=Ui.Danger;}}};
    }
    private List<string> GetHidden()=>columnNames.Where((_,index)=>!columns.GetItemChecked(index)).ToList();
    private void Bind(){views.DataSource=null;views.DataSource=saved.ToList();views.DisplayMember=nameof(SavedView.Name);views.SelectedIndex=-1;}
    private async Task WriteAsync(){Directory.CreateDirectory(Path.GetDirectoryName(filename)!);var temporary=filename+"."+Guid.NewGuid().ToString("N")+".tmp";try{await File.WriteAllTextAsync(temporary,JsonSerializer.Serialize(saved,ApiClient.Json));File.Move(temporary,filename,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}}
}

internal static class ExportFlow
{
    public static async Task RunAsync(IWin32Window owner, ApiClient api, ExportRequest request, string? scope = null)
    { using var progress = new ExportProgressForm(api, request, scope); await progress.ShowDialogAsync(owner); }
}

internal sealed class ExportProgressForm : Form
{
    private readonly ApiClient api;
    private readonly ExportRequest request;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Label status = Ui.Label("Excel và PDF dùng cùng dữ liệu và quyền. Chọn Tạo file để bắt đầu.");
    private readonly FlowLayoutPanel files = Ui.Bar();
    private readonly Button create, close;
    private bool working, resourcesDisposed;

    public ExportProgressForm(ApiClient api, ExportRequest request, string? scope = null)
    {
        this.api = api; this.request = request;
        Text = "Xuất chứng từ BotZone"; StartPosition = FormStartPosition.CenterParent; Size = new Size(840, 560); MinimumSize = new Size(600, 460); AutoScaleMode = AutoScaleMode.Dpi;
        create = Ui.Button("&Tạo Excel / PDF", async (_, _) => await GenerateAsync(), true); close = Ui.Button("Đóng (Esc)", (_, _) => Close()); AcceptButton=create;CancelButton = close;
        var description = Ui.Label(scope ?? (request.Kind == "detail" ? "Chỉ hóa đơn đang xem và các dòng chứng từ liên quan." : "Báo cáo đang hiển thị: cùng kỳ, chỉ tiêu và bộ lọc đã áp dụng.")); description.MaximumSize = new Size(680, 0);
        var note = Ui.Label("Dữ liệu được chốt lại tại thời điểm tạo file. Số lượng, tổng và thời điểm chính thức sẽ hiển thị sau khi API hoàn tất."); note.ForeColor = Ui.Muted; note.MaximumSize = new Size(680, 0);
        var content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var list = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 }; list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var control in new Control[] { description, note }) { list.RowStyles.Add(new RowStyle(SizeType.AutoSize)); list.Controls.Add(control, 0, list.RowCount++); } content.Controls.Add(list);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(20) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));for(var i=2;i<5;i++)layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        status.BackColor=Ui.Soft;status.Padding=new Padding(12);status.Dock=DockStyle.Top;status.MaximumSize=new Size(740,0);
        layout.Controls.Add(Ui.PageHeader("Xuất chứng từ", "Chọn nơi lưu sau khi file được tạo thành công."), 0, 0); layout.Controls.Add(content, 0, 1);layout.Controls.Add(status,0,2);layout.Controls.Add(files,0,3); layout.Controls.Add(Ui.Bar(create, close), 0, 4); Controls.Add(layout);
        FormClosed += (_, _) => { if(!resourcesDisposed)cancellation.Cancel(); }; Ui.Prepare(this);
    }
    private async Task GenerateAsync()
    {
        if (working) return; working = true; using var busy = Ui.Busy(create, "Đang tạo file…"); status.Text = "Đang đọc dữ liệu và tạo Excel / PDF…"; status.ForeColor = Ui.Muted;
        try
        {
            var result = await api.PostAsync<InvoiceExportResult>("api/invoices/exports", request, cancellation.Token); if (IsDisposed) return;
            status.Text = $"{result.RowCount:N0} hóa đơn · {Ui.Money(result.TotalVnd)}\nDữ liệu lúc {Ui.VietDate(result.ReadAt)}. Chọn từng định dạng để lưu.";
            foreach (var (format, url) in result.Links)
            { Button? save = null; save = Ui.Button("Lưu " + format.ToUpperInvariant(), async (_, _) => await SaveAsync(save!, format, url, result.ExportId)); files.Controls.Add(save); }
            AcceptButton=null;create.Visible = false;files.Controls.OfType<Button>().FirstOrDefault()?.Focus();
        }
        catch (OperationCanceledException) { if (!IsDisposed) status.Text = "Đã ngừng chờ. File chưa được lưu trên máy."; }
        catch (Exception ex) { if (!IsDisposed) { status.Text = Ui.Error(ex); status.ForeColor = Ui.Danger; } }
        finally { working = false; }
    }
    private async Task SaveAsync(Button button, string format, string url, Guid exportId)
    {
        using var save = new SaveFileDialog { Title = "Lưu " + format.ToUpperInvariant(), Filter = format == "pdf" ? "PDF (*.pdf)|*.pdf" : "Excel (*.xlsx)|*.xlsx", DefaultExt = format, AddExtension = true, FileName = $"BotZone-{exportId}.{format}", OverwritePrompt = true };
        if (save.ShowDialog(this) != DialogResult.OK) return; using var busy = Ui.Busy(button, "Đang lưu file…");
        try
        {
            await api.DownloadAsync(url, save.FileName, cancellation.Token); if (IsDisposed) return; status.Text = "Đã lưu " + save.FileName; var filename = save.FileName;
            files.Controls.Add(Ui.Button("Mở " + format.ToUpperInvariant(), (_, _) => Open(filename))); files.Controls.Add(Ui.Button("Mở thư mục", (_, _) => Open(Path.GetDirectoryName(filename)!)));
        }
        catch (Exception ex) { if (!IsDisposed) { status.Text = Ui.Error(ex); status.ForeColor = Ui.Danger; } }
    }
    private void Open(string filename) { try { Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true }); } catch (Exception ex) { status.Text = Ui.Error(ex); } }
    protected override void Dispose(bool disposing) { if (disposing&&!resourcesDisposed) { resourcesDisposed=true;cancellation.Cancel(); cancellation.Dispose(); } base.Dispose(disposing); }
}

