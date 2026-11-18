using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class InvoiceDetailForm : Form
{
    private readonly ApiClient api;
    private readonly Guid invoiceId;
    private readonly Label status=Ui.Label("Đang tải hóa đơn…"), issued=Ui.Label("—");
    private readonly TabControl tabs=Ui.Tabs();
    private readonly Button export, actions;
    private readonly TableLayoutPanel layout=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(24)};
    private Control heading;
    private CancellationTokenSource? cancellation;
    private Task? activeLoad;
    private long generation;
    private bool openingOrder;
    private string paymentSection="summary",informationSection="customer";
    public InvoiceDetailDto? CurrentInvoice{get;private set;}

    public InvoiceDetailForm(ApiClient api,Guid invoiceId)
    {
        this.api=api;this.invoiceId=invoiceId;Text="Hóa đơn";StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.Dpi;Size=new Size(1100,720);MinimumSize=new Size(720,480);
        export=Ui.Button("&Xuất hóa đơn",async(_,_)=>await ExportAsync(),true);export.Enabled=false;
        actions=Ui.MenuButton("Thao tác",("Xem đơn nguồn",async()=>await OpenOrderAsync()),("Tải lại",async()=>await LoadAsync()));
        heading=Ui.PageHeader("Hóa đơn","",export,actions);
        var caption=Ui.Label("Giá trị phát hành");caption.ForeColor=Ui.Muted;caption.Anchor=AnchorStyles.Left;caption.Margin=Padding.Empty;
        issued.Font=new Font("Segoe UI",22,FontStyle.Bold);issued.ForeColor=Ui.Primary;issued.Margin=Padding.Empty;
        var amount=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=2,RowCount=1,BackColor=Color.White,Padding=new Padding(16,12,16,12),Margin=new Padding(0,0,0,16)};amount.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));amount.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));amount.RowStyles.Add(new RowStyle(SizeType.AutoSize));amount.Controls.Add(caption,0,0);amount.Controls.Add(issued,1,0);
        tabs.SelectedIndexChanged+=(_,_)=>amount.Visible=tabs.SelectedIndex==0;
        var close=Ui.Button("Đóng",(_,_)=>Close());CancelButton=close;var footer=Ui.Bar(close);footer.FlowDirection=FlowDirection.RightToLeft;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.Controls.Add(heading,0,0);layout.Controls.Add(amount,0,1);layout.Controls.Add(tabs,0,2);layout.Controls.Add(status,0,3);layout.Controls.Add(footer,0,4);Controls.Add(layout);
        Shown+=async(_,_)=>await LoadAsync();FormClosed+=(_,_)=>cancellation?.Cancel();Ui.Prepare(this);
    }
    public Task LoadAsync()=>activeLoad is {IsCompleted:false}?activeLoad:activeLoad=LoadCoreAsync();
    private async Task LoadCoreAsync()
    {
        cancellation?.Cancel();cancellation?.Dispose();cancellation=new();var token=cancellation.Token;var current=++generation;
        using var busy=Ui.Busy(actions,"Đang tải…");status.ForeColor=Ui.Muted;
        try
        {
            var data=await api.GetAsync<InvoiceDetailDto>("api/invoices/"+invoiceId,token);if(IsDisposed||current!=generation)return;CurrentInvoice=data;
            var inv=data.Invoice;Text="Hóa đơn "+inv.InvoiceNo;issued.Text=Ui.Money(inv.GrandTotalVnd);
            var nextHeading=Ui.PageHeader(Text,$"{inv.CustomerName} · {Ui.VietDate(inv.IssuedAt)}",export,actions);layout.Controls.Remove(heading);heading.Dispose();heading=nextHeading;layout.Controls.Add(heading,0,0);
            var selected=tabs.SelectedIndex;foreach(var tab in tabs.TabPages.Cast<TabPage>().ToArray())tab.Dispose();tabs.TabPages.Clear();
            Add("Mặt hàng",ItemPanel(data));
            Add("Thanh toán",FinancePanels.Selector("Nội dung thanh toán",paymentSection,key=>paymentSection=key,
                ("summary","Tổng quan",()=>FinancePanels.Fields(("Giá trị phát hành",Ui.Money(inv.GrandTotalVnd)),("Điều chỉnh giảm lũy kế",Ui.Money(inv.CreditNoteTotalVnd)),("Giá trị ròng",Ui.Money(inv.NetVnd)),("Đã thu",Ui.Money(inv.ReceivedVnd)),("Đã hoàn",Ui.Money(inv.RefundedVnd)))),
                ("receipts","Khoản thu",()=>JsonPanel(data.Receipts,"receipts")),("notes","Chứng từ giảm",()=>JsonPanel(data.CreditNotes,"notes")),("refunds","Khoản hoàn",()=>JsonPanel(data.Refunds,"refunds")),("attempts","Lần thanh toán",()=>JsonPanel(data.Attempts,"attempts"))));
            Add("Thông tin",FinancePanels.Selector("Nội dung thông tin",informationSection,key=>informationSection=key,
                ("customer","Khách và giao hàng",()=>SnapshotPanel(inv)),
                ("document","Thông tin chứng từ",()=>FinancePanels.Fields(("Đơn nguồn",inv.OrderNo),("Nhân viên phụ trách",inv.StaffName),("Mã nhân viên",Ui.Text(inv.StaffSnapshot,"staffCode","staff_code")),("Người phát hành (mã)",inv.IssuedByUserId??""),("Trước giảm",Ui.Money(inv.SubtotalVnd)),("Giảm giá",Ui.Money(inv.DiscountTotalVnd)),("Tỷ giá snapshot (VND/USD)",inv.ExchangeRateVnd),("Dữ liệu được đọc lúc",Ui.VietDate(data.ReadAt)))),
                ("promotions","Ưu đãi và phiên bản",()=>JsonPanel(data.Promotions,"promotions"))));
            if(selected>=0&&selected<tabs.TabPages.Count)tabs.SelectedIndex=selected;export.Enabled=true;status.Visible=false;Ui.Pulse(tabs);
        }
        catch(OperationCanceledException){}
        catch(Exception ex){if(!IsDisposed&&current==generation){status.Visible=true;status.ForeColor=Ui.Danger;status.Text=Ui.Error(ex)+" Mở Thao tác → Tải lại để thử tiếp.";}}
    }
    private Control ItemPanel(InvoiceDetailDto data)
    {
        var grid=Ui.Grid();foreach(var col in new[]{("Name","Mặt hàng",280),("Quantity","SL",70),("UnitPriceVnd","Đơn giá",150),("NetAmountVnd","Thành tiền",170),("Sku","SKU",160),("LineNo","Dòng",70),("LineType","Loại",120),("VoucherDiscountVnd","Voucher",150),("PointsDiscountVnd","Điểm giảm",150),("Serials","Serial / IMEI",200),("Variant","Biến thể",200)})Ui.Column(grid,col.Item1,col.Item2,col.Item3);
        foreach(DataGridViewColumn column in grid.Columns){column.Visible=column.Index<4;column.FillWeight=column.Name=="Name"?50:column.Name=="Quantity"?10:20;}grid.Columns["Name"]!.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        grid.CellFormatting+=(_,e)=>{if(e.Value is string money&&grid.Columns[e.ColumnIndex].DataPropertyName.EndsWith("Vnd")){e.Value=Ui.Money(money);e.FormattingApplied=true;}else if(e.Value is List<string> serials){e.Value=string.Join(", ",serials);e.FormattingApplied=true;}else if(e.Value is JsonElement variant&&variant.ValueKind==JsonValueKind.Object){e.Value=string.Join(" · ",variant.EnumerateObject().Select(x=>x.Name+": "+x.Value));e.FormattingApplied=true;}else if(grid.Columns[e.ColumnIndex].DataPropertyName=="LineType"&&e.Value is string type){e.Value=type switch{"product"=>"Sản phẩm","warranty"=>"Bảo hành / Care",_=>type};e.FormattingApplied=true;}};
        var source=new BindingSource{DataSource=data.Items};grid.DataSource=source;grid.Disposed+=(_,_)=>source.Dispose();
        var more=Ui.MenuButton("Xem thêm",("Hiện / ẩn cột chi tiết",()=>{var visible=!grid.Columns["Sku"]!.Visible;foreach(DataGridViewColumn column in grid.Columns)if(column.Index>=4)column.Visible=visible;Ui.Pulse(grid);}),
            ("Phân bổ từng món",async()=>await FinancePanels.OpenAsync(this,"Phân bổ từng món",JsonPanel(data.Units,"units",data.Items.GroupBy(item=>item.OrderItemId.ToString()).ToDictionary(group=>group.Key,group=>group.First().Name)))));
        var panel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(0,8,0,0)};panel.Controls.Add(grid);panel.Controls.Add(Ui.Bar(more));return panel;
    }
    private async Task OpenOrderAsync()
    {
        if(CurrentInvoice is null||openingOrder)return;openingOrder=true;
        try{using var form=new OrderDetailForm(api,CurrentInvoice.Invoice.OrderId.ToString());await form.ShowDialogAsync(this);}catch(Exception ex){if(!IsDisposed)Ui.ShowError(this,ex);}finally{openingOrder=false;}
    }
    private async Task ExportAsync()
    {
        if(CurrentInvoice is null)return;using var busy=Ui.Busy(export,"Đang xuất…");await ExportFlow.RunAsync(this,api,new ExportRequest{InvoiceId=invoiceId,Kind="detail",Formats=["xlsx","pdf"]},$"Hóa đơn {CurrentInvoice.Invoice.InvoiceNo} · Giá trị phát hành {Ui.Money(CurrentInvoice.Invoice.GrandTotalVnd)}");
    }
    private void Add(string title,Control body){var tab=new TabPage(title);tab.Controls.Add(body);tabs.TabPages.Add(tab);}
    private static Control SnapshotPanel(InvoiceDetailHeaderDto invoice)=>FinancePanels.Fields(("Khách hàng",invoice.CustomerName),("Email snapshot",Ui.Text(invoice.CustomerSnapshot,"email")),("Người nhận",Ui.Text(invoice.AddressSnapshot,"recipient","name")),("Điện thoại",Ui.Text(invoice.AddressSnapshot,"phone")),("Tỉnh / thành",Ui.Text(invoice.AddressSnapshot,"province","city")),("Phường / xã",Ui.Text(invoice.AddressSnapshot,"ward")),("Địa chỉ",Ui.Text(invoice.AddressSnapshot,"addressLine","address_line","address")));
    internal static DataGridView JsonGrid(IEnumerable<JsonElement> rows)
    {
        var grid=Ui.Grid();grid.AutoGenerateColumns=true;var table=Ui.Table(rows);
        grid.DataBindingComplete+=(_,_)=>{foreach(DataGridViewColumn column in grid.Columns)if(DetailLabels.TryGetValue(column.DataPropertyName,out var label)){column.HeaderText=label;column.MinimumWidth=Math.Max(column.MinimumWidth,TextRenderer.MeasureText(label,grid.ColumnHeadersDefaultCellStyle.Font).Width+32);}};
        grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex<0||e.Value is not string value)return;var property=grid.Columns[e.ColumnIndex].DataPropertyName;if(property is "method" or "tender_kind"){e.Value=value switch{"cash_demo"=>"Tiền demo","store_credit"=>"Store credit","banking"=>"Chuyển khoản","momo"=>"MoMo demo","visa"=>"Visa demo","cod"=>"COD",_=>value};e.FormattingApplied=true;}};
        grid.DataSource=table;grid.Disposed+=(_,_)=>table.Dispose();return grid;
    }
    private static Control JsonPanel(IEnumerable<JsonElement> source,string kind,IReadOnlyDictionary<string,string>? itemNames=null)
    {
        var rows=source.ToList();var grid=JsonGrid(rows);var configured=false;
        string[] core=kind switch{"receipts"=>["received_at","tender_kind","amount_vnd"],"notes"=>["credit_note_no","issued_at","status","total_vnd"],"refunds"=>["completed_at","status","amount_vnd"],"attempts"=>["created_at","method","status","requested_amount_vnd"],"promotions"=>["code_normalized","version_no","status","amount_vnd"],"units"=>["order_item_id","unit_ordinal","net_amount_vnd","cash_planned_vnd","credit_planned_vnd"],_=>[]};
        void Configure(){if(configured||grid.Columns.Count==0)return;foreach(DataGridViewColumn column in grid.Columns){column.Visible=core.Contains(column.Name);column.FillWeight=column.Name=="order_item_id"?38:18;}for(var i=0;i<core.Length;i++)if(grid.Columns.Contains(core[i]))grid.Columns[core[i]]!.DisplayIndex=i;if(itemNames is not null&&grid.Columns.Contains("order_item_id")){grid.Columns["order_item_id"]!.HeaderText="Mặt hàng";grid.Columns["order_item_id"]!.MinimumWidth=240;}configured=true;}
        void SelectFirst(){if(!grid.IsDisposed&&grid.CurrentCell is null&&grid.Rows.Count>0&&grid.Columns.Cast<DataGridViewColumn>().Where(column=>column.Visible).OrderBy(column=>column.DisplayIndex).FirstOrDefault() is {} first)grid.CurrentCell=grid.Rows[0].Cells[first.Index];}
        grid.DataBindingComplete+=(_,_)=>{Configure();if(itemNames is not null&&grid.Columns.Contains("order_item_id"))grid.Columns["order_item_id"]!.HeaderText="Mặt hàng";SelectFirst();};grid.HandleCreated+=(_,_)=>grid.BeginInvoke((Action)SelectFirst);Configure();
        if(itemNames is not null)grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="order_item_id"&&e.Value is string id&&itemNames.TryGetValue(id,out var name)){e.Value=name;e.FormattingApplied=true;}};
        var columns=Ui.Button("Cột hiển thị",async(_,_)=>await FinancePanels.ChooseColumnsAsync(grid.FindForm()!,grid,"Cột chi tiết hóa đơn",[]));var toolbar=Ui.Bar(columns);
        var nested=rows.Any(row=>row.TryGetProperty("allocations",out _))?"allocations":rows.Any(row=>row.TryGetProperty("items",out _))?"items":null;
        if(nested is not null){var show=Ui.Button(nested=="allocations"?"Xem phân bổ":"Xem dòng chứng từ",async(_,_)=>{if(grid.CurrentRow?.DataBoundItem is not System.Data.DataRowView row||row[nested] is not string text||string.IsNullOrWhiteSpace(text))return;try{using var document=JsonDocument.Parse(text);if(document.RootElement.ValueKind==JsonValueKind.Array)await FinancePanels.OpenAsync(grid.FindForm()!,nested=="allocations"?"Chi tiết phân bổ":"Dòng chứng từ",JsonGrid(document.RootElement.EnumerateArray()));}catch(Exception ex){Ui.ShowError(grid.FindForm(),ex);}});toolbar.Controls.Add(show);}
        if(rows.Count==0)toolbar.Controls.Add(Ui.Label("Chưa có bản ghi."));var panel=new Panel{Dock=DockStyle.Fill};panel.Controls.Add(grid);panel.Controls.Add(toolbar);return panel;
    }
    private static readonly Dictionary<string,string> DetailLabels=new(StringComparer.OrdinalIgnoreCase){["id"]="Tham chiếu",["order_item_id"]="Dòng đơn",["unit_ordinal"]="Thứ tự món",["parent_unit_amount_id"]="Món gốc",["gross_vnd"]="Trước giảm",["net_amount_vnd"]="Sau giảm",["cash_planned_vnd"]="Tiền dự kiến",["credit_planned_vnd"]="Credit dự kiến",["points_discount_vnd"]="Điểm giảm",["voucher_discount_vnd"]="Voucher giảm",["redeemed_points"]="Điểm đã dùng",["tender_kind"]="Nguồn thu",["received_at"]="Thu lúc",["allocations"]="Chi tiết phân bổ",["credit_note_no"]="Mã chứng từ giảm",["issued_at"]="Phát hành lúc",["return_request_id"]="Hồ sơ trả",["warranty_claim_id"]="Hồ sơ Care",["shipment_id"]="Vận đơn",["items"]="Dòng chứng từ",["credit_note_id"]="Chứng từ giảm",["completed_at"]="Hoàn tất lúc",["method"]="Phương thức",["requested_amount_vnd"]="Số tiền yêu cầu",["code_normalized"]="Mã ưu đãi",["version_no"]="Phiên bản",["unitId"]="Món được phân bổ",["receiptId"]="Khoản thu",["amountVnd"]="Số tiền",["tenderKind"]="Nguồn thu",["invoiceItemId"]="Dòng hóa đơn"};
    protected override void Dispose(bool disposing){if(disposing){var pending=cancellation;cancellation=null;pending?.Cancel();pending?.Dispose();}base.Dispose(disposing);}
}

internal static class FinancePanels
{
    internal static Control Fields(params (string Label,string Value)[] fields)
    {
        var table=new TableLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,ColumnCount=2,RowCount=0,Padding=new Padding(16),BackColor=Color.White};table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,70));
        foreach(var(label,value)in fields){var row=table.RowCount++;table.RowStyles.Add(new RowStyle(SizeType.AutoSize));var caption=Ui.Label(label);caption.ForeColor=Ui.Muted;table.Controls.Add(caption,0,row);table.Controls.Add(Ui.FrameInput(new TextBox{ReadOnly=true,Text=value,AccessibleName=label,Multiline=value.Length>95,Height=value.Length>95?70:30,Dock=DockStyle.Top,Margin=new Padding(0,0,0,12)}),1,row);}return table;
    }
    internal static Control Selector(string label,string selected,Action<string> changed,params (string Key,string Label,Func<Control> Body)[] sections)
    {
        var selector=Ui.Combo(sections.Select(s=>new Choice(s.Key,s.Label)).ToArray());selector.Width=320;selector.AccessibleName=label;var host=new Panel{Dock=DockStyle.Fill};var panel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(8)};panel.Controls.Add(host);panel.Controls.Add(Ui.Bar(Ui.Field(label,selector)));
        void Render(){var key=Ui.Selected(selector);changed(key);foreach(Control control in host.Controls.Cast<Control>().ToArray())control.Dispose();host.Controls.Clear();var body=sections.First(s=>s.Key==key).Body();body.Dock=DockStyle.Fill;host.Controls.Add(body);Ui.Pulse(host);}
        var index=Array.FindIndex(sections,s=>s.Key==selected);selector.SelectedIndex=index>=0?index:0;selector.SelectedIndexChanged+=(_,_)=>Render();Render();return panel;
    }
    internal static async Task<bool> ChooseColumnsAsync(IWin32Window owner,DataGridView grid,string title,string[] required,string[]? excluded=null)
    {
        try
        {
            using var form=new Form{Text=title,Size=new Size(520,540),MinimumSize=new Size(420,380),StartPosition=FormStartPosition.CenterParent};var choices=new CheckedListBox{Dock=DockStyle.Fill,CheckOnClick=true,BorderStyle=BorderStyle.None,AccessibleName="Các cột hiển thị"};var columns=grid.Columns.Cast<DataGridViewColumn>().Where(c=>excluded is null||!excluded.Contains(c.Name)).ToArray();foreach(var column in columns)choices.Items.Add(column.HeaderText+(required.Contains(column.Name)?" *":""),column.Visible||required.Contains(column.Name));choices.ItemCheck+=(_,e)=>{if(required.Contains(columns[e.Index].Name))e.NewValue=CheckState.Checked;};
            var hint=Ui.Label(required.Length>0?"* Cột bắt buộc luôn được giữ.":"");hint.ForeColor=Ui.Muted;hint.Visible=required.Length>0;
            var apply=Ui.Button("&Áp dụng",(_,_)=>{if(columns.Length>0&&!columns.Where((column,index)=>required.Contains(column.Name)||choices.GetItemChecked(index)).Any()){hint.Text="Chọn ít nhất một cột để hiển thị.";hint.ForeColor=Ui.Danger;hint.Visible=true;choices.Focus();return;}for(var i=0;i<columns.Length;i++)columns[i].Visible=required.Contains(columns[i].Name)||choices.GetItemChecked(i);form.DialogResult=DialogResult.OK;form.Close();},true);var close=Ui.Button("Hủy",(_,_)=>form.Close());form.AcceptButton=apply;form.CancelButton=close;var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(24)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.Controls.Add(Ui.PageHeader(title,""),0,0);layout.Controls.Add(choices,0,1);layout.Controls.Add(hint,0,2);layout.Controls.Add(Ui.Bar(apply,close),0,3);form.Controls.Add(layout);Ui.Prepare(form);return await form.ShowDialogAsync(grid.FindForm()??owner)==DialogResult.OK;
        }
        catch(Exception ex){Ui.ShowError(owner,ex);return false;}
    }
    internal static async Task OpenAsync(IWin32Window owner,string title,Control body)
    {
        try{using var form=new Form{Text=title,Size=new Size(960,620),MinimumSize=new Size(620,440),StartPosition=FormStartPosition.CenterParent};var close=Ui.Button("Đóng",(_,_)=>form.Close());form.CancelButton=close;var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(24)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.Controls.Add(Ui.PageHeader(title,""),0,0);body.Dock=DockStyle.Fill;layout.Controls.Add(body,0,1);layout.Controls.Add(Ui.Bar(close),0,2);form.Controls.Add(layout);Ui.Prepare(form);await form.ShowDialogAsync(owner);}catch(Exception ex){Ui.ShowError(owner,ex);}
    }
}
