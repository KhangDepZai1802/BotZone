using System.Data;
using System.Text.Json;

namespace BotZone.Admin.WinForms;

internal sealed class CustomerDetailForm:Form
{
    private readonly ApiClient api;private readonly string userId;
    private readonly Label status=Ui.Label("Đang tải hồ sơ…");
    private readonly TabControl tabs=Ui.Tabs();
    private readonly Button refresh;
    private readonly TableLayoutPanel layout=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(24)};
    private Control heading;
    private CancellationTokenSource? pending;private Task? activeLoad;private long generation;
    private string historySection="creditEntries";private string dataInfo="";
    internal bool Loaded{get;private set;}
    internal bool HasLoadError{get;private set;}
    internal string StatusMessage=>status.Text;
    internal Dictionary<string,int> RowCounts{get;}=[];

    public CustomerDetailForm(ApiClient api,string userId)
    {
        this.api=api;this.userId=userId;Text="Khách hàng";StartPosition=FormStartPosition.CenterParent;Size=new Size(1100,720);MinimumSize=new Size(720,480);AutoScaleMode=AutoScaleMode.Dpi;
        refresh=Ui.MenuButton("Thao tác",("Tải lại",async()=>await LoadAsync()),("Thông tin dữ liệu",()=>Ui.ShowInformation(this,"Thông tin dữ liệu",dataInfo)));
        heading=Ui.PageHeader("Khách hàng","",refresh);var close=Ui.Button("Đóng",(_,_)=>Close());CancelButton=close;var footer=Ui.Bar(close);footer.FlowDirection=FlowDirection.RightToLeft;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.Controls.Add(heading,0,0);layout.Controls.Add(tabs,0,1);layout.Controls.Add(status,0,2);layout.Controls.Add(footer,0,3);Controls.Add(layout);
        Shown+=async(_,_)=>await LoadAsync();FormClosed+=(_,_)=>pending?.Cancel();Ui.Prepare(this);
    }
    internal Task LoadAsync()=>activeLoad is {IsCompleted:false}?activeLoad:activeLoad=LoadCoreAsync();
    private async Task LoadCoreAsync()
    {
        pending?.Cancel();pending?.Dispose();pending=new();var token=pending.Token;var current=++generation;Loaded=false;HasLoadError=false;status.Text="Đang tải hồ sơ…";status.ForeColor=Ui.Muted;status.Visible=true;
        using var busy=Ui.Busy(refresh,"Đang tải…");var selected=tabs.SelectedIndex;
        try
        {
            var data=await api.GetAsync<JsonElement>("api/admin/customers/"+Uri.EscapeDataString(userId),token);if(IsDisposed||current!=generation)return;
            foreach(var tab in tabs.TabPages.Cast<TabPage>().ToArray())tab.Dispose();tabs.TabPages.Clear();RowCounts.Clear();foreach(var key in new[]{"orders","addresses","creditEntries","loyaltyEntries","returns","tradeIns","claims"})RowCounts[key]=Ui.Rows(data,key).Count;
            var customer=data.GetProperty("customer");var balances=data.GetProperty("balances");var name=Ui.Text(customer,"display_name","displayName","name");Text="Khách hàng · "+name;
            var next=Ui.PageHeader(name,"Khách hàng",refresh);layout.Controls.Remove(heading);heading.Dispose();heading=next;layout.Controls.Add(heading,0,0);
            var readAt=DateTimeOffset.TryParse(Ui.Text(data,"readAt"),out var timestamp)?Ui.VietDate(timestamp):"chưa có";dataInfo=$"Dữ liệu đọc lúc {readAt}.\nTối đa {Ui.Text(data,"historyLimit")} bản ghi mới nhất mỗi lịch sử.\nSố dư và hạng do API tính.";
            Add("Hồ sơ",Summary(customer,balances,data));
            Add("Đơn hàng",BuildGrid(data,"orders",[("order_no","Mã đơn",170),("order_status","Trạng thái",150),("total_vnd","Tổng",170),("created_at","Đặt lúc",180),("payment_method","Thanh toán",140),("delivered_at","Giao lúc",180)],true));
            Add("Lịch sử",FinancePanels.Selector("Loại lịch sử",historySection,key=>historySection=key,
                ("creditEntries","Store credit",()=>BuildGrid(data,"creditEntries",[("created_at","Ghi nhận lúc",180),("kind","Loại giao dịch",160),("delta_vnd","Thay đổi",170),("amount_vnd","Số tiền",170),("order_no","Mã đơn",185),("order_id","Tham chiếu đơn",240),("trade_in_item_id","Tham chiếu thu cũ",240),("event_key","Sự kiện",280),("reason","Lý do",240)])),
                ("loyaltyEntries","Điểm thưởng",()=>BuildGrid(data,"loyaltyEntries",[("created_at","Ghi nhận lúc",180),("kind","Loại giao dịch",160),("delta","Thay đổi điểm",140),("delta_points","Thay đổi điểm",140),("order_no","Mã đơn",185),("order_id","Tham chiếu đơn",240),("event_key","Sự kiện",280)])),
                ("returns","Trả hàng",()=>BuildGrid(data,"returns",[("id","Mã hồ sơ",240),("state","Trạng thái",150),("status","Trạng thái",150),("reason","Lý do",270),("created_at","Yêu cầu lúc",180),("order_id","Tham chiếu đơn",240)])),
                ("tradeIns","Thu cũ",()=>BuildGrid(data,"tradeIns",[("id","Mã hồ sơ",240),("reception_method","Tiếp nhận",160),("state","Trạng thái",150),("status","Trạng thái",150),("created_at","Yêu cầu lúc",180),("note","Ghi chú",280)])),
                ("claims","Bảo hành / Care",()=>BuildGrid(data,"claims",[("id","Mã hồ sơ",240),("state","Trạng thái",150),("status","Trạng thái",150),("description","Mô tả",280),("damage_kind","Loại hư hỏng",160),("created_at","Yêu cầu lúc",180),("starts_on","Care bắt đầu",140),("ends_on","Care kết thúc",140),("order_id","Tham chiếu đơn",240)]))));
            if(selected>=0&&selected<tabs.TabPages.Count)tabs.SelectedIndex=selected;Loaded=true;status.Visible=false;status.Text="";Ui.Pulse(tabs);
        }
        catch(OperationCanceledException){}catch(Exception ex){if(!IsDisposed&&current==generation){HasLoadError=true;status.Visible=true;status.ForeColor=Ui.Danger;status.Text=Ui.Error(ex);}}
    }
    private void Add(string title,Control body){var tab=new TabPage(title);tab.Controls.Add(body);tabs.TabPages.Add(tab);}
    private Control Summary(JsonElement customer,JsonElement balances,JsonElement data)
    {
        var fields=FinancePanels.Fields(("Email",Ui.Text(customer,"email")),("Điện thoại",Ui.Text(customer,"phone")),("Store credit",Ui.Money(Ui.Text(balances,"credit_vnd","creditVnd","credit"))),("Điểm thưởng",Ui.Text(balances,"balance_points","balancePoints","points")));
        var addresses=Ui.Button("Địa chỉ",async(_,_)=>await FinancePanels.OpenAsync(this,"Địa chỉ",BuildGrid(data,"addresses",[("recipient","Người nhận",180),("phone","Điện thoại",140),("province","Tỉnh / thành",170),("ward","Phường / xã",160),("address_line","Địa chỉ",280),("is_default","Mặc định",100)])));
        var more=Ui.Button("Thông tin khác",async(_,_)=>await FinancePanels.OpenAsync(this,"Thông tin khác",FinancePanels.Fields(("Email đã xác minh",Ui.Text(customer,"email_verified","emailVerified") is "True" or "true"?"Đã xác minh":"Chưa xác minh"),("Hạng hiện tại",Ui.Text(balances,"tier")),("Điểm đang giữ",Ui.Text(balances,"held_points")),("Credit đang giữ",Ui.Money(Ui.Text(balances,"held_credit_vnd"))),("Chi tiêu ròng tích lũy",Ui.Money(Ui.Text(balances,"net_spend_vnd","netSpendVnd","spend"))),("Tổng số đơn",Ui.Text(balances,"order_count","orderCount")))));
        var panel=new Panel{Dock=DockStyle.Fill};panel.Controls.Add(fields);panel.Controls.Add(Ui.Bar(addresses,more));return panel;
    }
    private Control BuildGrid(JsonElement data,string key,(string Key,string Label,int Width)[] columns,bool openOrder=false)
    {
        var rows=Ui.Rows(data,key);var grid=Ui.Grid();var table=Ui.Table(rows);var source=new BindingSource{DataSource=table};grid.DataSource=source;grid.Disposed+=(_,_)=>{source.Dispose();table.Dispose();};
        foreach(var col in columns)if(table.Columns.Contains(col.Key))Ui.Column(grid,col.Key,col.Label,col.Width);foreach(DataGridViewColumn column in grid.Columns){column.Visible=key=="addresses"?column.Name is "recipient" or "phone" or "address_line" or "is_default":column.Index<4;column.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;column.FillWeight=column.Name is "description" or "reason" or "address_line"?40:column.Name=="is_default"?12:20;}
        if(key=="addresses"&&grid.Columns.Contains("address_line")){grid.Columns["address_line"]!.DefaultCellStyle.WrapMode=DataGridViewTriState.True;grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells;}
        grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex<0||e.Value is not string value)return;var property=grid.Columns[e.ColumnIndex].DataPropertyName;if(key=="addresses"&&property=="address_line"&&grid.Rows[e.RowIndex].DataBoundItem is DataRowView row){e.Value=string.Join(", ",new[]{"address_line","ward","province"}.Where(table.Columns.Contains).Select(name=>Convert.ToString(row[name])).Where(part=>!string.IsNullOrWhiteSpace(part)));e.FormattingApplied=true;}else if(property=="is_default"){e.Value=value.Equals("true",StringComparison.OrdinalIgnoreCase)?"Mặc định":"—";e.FormattingApplied=true;}else if(property.EndsWith("_vnd")){e.Value=Ui.Money(value);e.FormattingApplied=true;}else if(property.EndsWith("_at")&&DateTimeOffset.TryParse(value,out var date)){e.Value=Ui.VietDate(date);e.FormattingApplied=true;}else if(property is "kind" or "state" or "order_status" or "payment_method" or "reception_method" or "damage_kind"){e.Value=Values.GetValueOrDefault(value,value);e.FormattingApplied=true;}};
        var opening=false;if(openOrder){grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await OpenOrder();};grid.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.Handled=e.SuppressKeyPress=true;await OpenOrder();}};}
        var columnsButton=Ui.Button("Cột hiển thị",async(_,_)=>await FinancePanels.ChooseColumnsAsync(this,grid,"Cột lịch sử",[]));var toolbar=Ui.Bar(columnsButton);
        if(openOrder){var open=Ui.Button("Mở đơn",async(_,_)=>await OpenOrder());open.Enabled=rows.Count>0;toolbar.Controls.Add(open);}if(rows.Count==0)toolbar.Controls.Add(Ui.Label("Chưa có bản ghi."));
        var panel=new Panel{Dock=DockStyle.Fill};panel.Controls.Add(grid);panel.Controls.Add(toolbar);return panel;
        async Task OpenOrder(){if(opening||grid.CurrentRow?.DataBoundItem is not DataRowView row||!table.Columns.Contains("id"))return;opening=true;try{using var detail=new OrderDetailForm(api,Convert.ToString(row["id"])!);await detail.ShowDialogAsync(this);}catch(Exception ex){if(!IsDisposed)Ui.ShowError(this,ex);}finally{opening=false;}}
    }
    private static readonly Dictionary<string,string> Values=new(StringComparer.OrdinalIgnoreCase){["trade_in"]="Thu cũ",["spend"]="Sử dụng credit",["refund"]="Hoàn",["earn"]="Tích điểm",["redeem"]="Đổi điểm",["restore"]="Khôi phục điểm",["reverse"]="Thu hồi điểm",["pending"]="Chờ xử lý",["confirmed"]="Đã xác nhận",["handed_off"]="Đã bàn giao",["delivered"]="Đã giao",["cancelled"]="Đã hủy",["expired"]="Hết hạn",["requested"]="Đã yêu cầu",["received"]="Đã tiếp nhận",["inspected"]="Đã kiểm tra",["approved"]="Đã duyệt",["rejected"]="Từ chối",["credited"]="Đã ghi giảm",["closed"]="Đã đóng",["quoted"]="Đã báo giá",["completed"]="Hoàn tất",["returned"]="Đã trả lại",["repairing"]="Đang sửa",["repaired"]="Đã sửa",["replace"]="Duyệt đổi",["replaced"]="Đã đổi",["refunded"]="Đã hoàn",["contact_pending"]="Chờ liên hệ",["exception"]="Cần xử lý ngoại lệ",["store"]="Tại cửa hàng",["remote"]="Từ xa",["hardware"]="Phần cứng",["accidental"]="Va chạm",["liquid"]="Chất lỏng",["cod"]="COD",["banking"]="Chuyển khoản",["momo"]="MoMo demo",["visa"]="Visa demo"};
    protected override void Dispose(bool disposing){if(disposing){var active=pending;pending=null;active?.Cancel();active?.Dispose();}base.Dispose(disposing);}
}
