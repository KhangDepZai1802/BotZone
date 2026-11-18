using System.Globalization;
using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class OperationalReportsView:PageView
{
    private readonly ComboBox kind=Ui.Combo();
    private readonly DateTimePicker from=new BzDateTimePicker(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Value=new DateTime(2024,1,1),Width=180};
    private readonly DateTimePicker to=new BzDateTimePicker(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Value=new DateTime(2026,12,31),Width=180};
    private readonly TextBox query=new(){Width=230,PlaceholderText="Mã hàng / tên sản phẩm…",AccessibleName="Tìm trong báo cáo"};
    private readonly Label definition=Ui.Label("Định nghĩa chỉ tiêu sẽ được API trả theo báo cáo.");
    private readonly DataGridView grid=Ui.Grid();
    private readonly BindingSource binding=new();
    private readonly InsightChart chart=new(){Dock=DockStyle.Fill,Name="operationChart"};
    private readonly Label summary=Ui.Label("");
    private readonly Panel dataHost=new(){Dock=DockStyle.Fill};
    private readonly System.Windows.Forms.Timer debounce=new(){Interval=350};
    private string reportInfo="Chưa tải báo cáo.";
    private readonly Dictionary<string,HashSet<string>> columnsByReport=[];
    public OperationalReportsView(ApiClient api,Actor actor):base(api)
    {
        Dock=DockStyle.Fill;var all=new[]{new Choice("inventory","Tồn kho"),new Choice("margin","Lãi gộp hàng bán"),new Choice("top_products","Sản phẩm bán chạy"),new Choice("promotions","Hiệu quả ưu đãi"),new Choice("trade_in","Thu cũ / cấp số dư"),new Choice("warranty","Bảo hành mở rộng")};
        kind.Width=300;foreach(var item in all)if(actor.Role=="owner"||actor.Role=="warehouse_staff"&&item.Value=="inventory"||actor.Role=="order_staff"&&item.Value=="promotions")kind.Items.Add(item);if(kind.Items.Count>0)kind.SelectedIndex=0;
        grid.AutoGenerateColumns=true;grid.DataSource=binding;grid.CellFormatting+=(_,e)=>{if(e.Value is string text&&grid.Columns[e.ColumnIndex].DataPropertyName.EndsWith("vnd",StringComparison.OrdinalIgnoreCase)){e.Value=Ui.Money(text);e.FormattingApplied=true;}};
        var refresh=Ui.Button("&Xem báo cáo",async(_,_)=>await RefreshAsync(),true);
        var more=Ui.MenuButton("Thao tác",("Cột hiển thị",async()=>{if(await FinancePanels.ChooseColumnsAsync(FindForm()!,grid,"Cột báo cáo",[]))columnsByReport[Ui.Selected(kind)]=grid.Columns.Cast<DataGridViewColumn>().Where(c=>c.Visible).Select(c=>c.Name).ToHashSet();}),("Thông tin báo cáo",()=>Ui.ShowInformation(FindForm(),"Thông tin báo cáo",reportInfo)));
        var controls=Ui.Bar(Ui.Field("Báo cáo",kind),Ui.Field("Từ ngày",from),Ui.Field("Đến ngày",to),Ui.Field("Tìm trong dữ liệu",query));
        controls.BackColor=Color.White;controls.Padding=new Padding(12,8,12,4);controls.Margin=new Padding(0,0,0,12);
        definition.ForeColor=Ui.Muted;definition.BackColor=Color.White;definition.Padding=new Padding(12);definition.MaximumSize=new Size(1100,0);
        grid.Visible=false;dataHost.Controls.Add(grid);dataHost.Controls.Add(chart);
        var display=Ui.Bar(Ui.Button("Biểu đồ",(_,_)=>{chart.Visible=true;grid.Visible=false;chart.BringToFront();}),Ui.Button("Bảng chi tiết",(_,_)=>{chart.Visible=false;grid.Visible=true;grid.BringToFront();}),summary);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(20)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(var i=0;i<4;i++)layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.Controls.Add(Ui.PageHeader("Báo cáo hoạt động","",refresh,more),0,0);layout.Controls.Add(controls,0,1);layout.Controls.Add(display,0,2);layout.Controls.Add(Status,0,3);layout.Controls.Add(dataHost,0,4);Controls.Add(layout);Status.TextChanged+=(_,_)=>Status.Visible=!string.IsNullOrWhiteSpace(Status.Text);
        kind.SelectedIndexChanged+=async(_,_)=>await RefreshAsync();query.TextChanged+=(_,_)=>{Deactivate();debounce.Stop();debounce.Start();};debounce.Tick+=async(_,_)=>{debounce.Stop();await RefreshAsync();};VisibleChanged+=(_,_)=>{if(!Visible)debounce.Stop();};query.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.Handled=e.SuppressKeyPress=true;debounce.Stop();await RefreshAsync();}};
    }
    public override Task RefreshAsync()
    {
        Deactivate();
        if(from.Value.Date>to.Value.Date){Status.Text="Ngày kết thúc phải sau ngày bắt đầu.";Status.ForeColor=Ui.Danger;to.Focus();return Task.CompletedTask;}
        if(kind.SelectedItem is null){Status.Text="Vai trò hiện tại không có báo cáo nghiệp vụ này.";return Task.CompletedTask;}
        var url=$"api/admin/reports/operations?kind={Uri.EscapeDataString(Ui.Selected(kind))}&from={from.Value:yyyy-MM-dd}&to={to.Value.Date.AddDays(1):yyyy-MM-dd}&q={Uri.EscapeDataString(query.Text.Trim())}";
        return LoadLatestAsync(token=>Api.GetAsync<JsonElement>(url,token),data=>
        {
            var position=binding.Position;var rows=Ui.Rows(data);var previous=binding.DataSource as System.Data.DataTable;binding.DataSource=Ui.Table(rows);previous?.Dispose();if(position>=0&&position<rows.Count)binding.Position=position;var visible=columnsByReport.GetValueOrDefault(Ui.Selected(kind));var candidates=CoreColumns(Ui.Selected(kind)).ToHashSet();foreach(DataGridViewColumn column in grid.Columns){column.HeaderText=Labels.GetValueOrDefault(column.Name,RecordPresentation.Label(column.Name));column.MinimumWidth=125;column.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;column.FillWeight=column.Name is "name" or "product_name"?35:18;column.Visible=visible?.Contains(column.Name)??candidates.Contains(column.Name);if(column.Name is "available" or "reserved" or "quarantine" or "net_quantity" or "consumed" or "released" or "item_count")column.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;}
            definition.Text=Definition(Ui.Selected(kind));var at=DateTimeOffset.TryParse(Ui.Text(data,"readAt"),CultureInfo.InvariantCulture,DateTimeStyles.None,out var readAt)?Ui.VietDate(readAt):Ui.Text(data,"readAt");reportInfo=$"{definition.Text}\n\n{rows.Count:N0} dòng · Dữ liệu đọc lúc {at}.\nPhạm vi theo quyền tài khoản. Tối đa 10.000 dòng trong một lần tải.";SetChart(Ui.Selected(kind),rows);Status.Text=rows.Count==0?"Không có dữ liệu phù hợp. Đổi kỳ hoặc xóa nội dung tìm kiếm.":"";
        });
    }
    private static string[] CoreColumns(string kind)=>kind switch{"inventory"=>["sku","available","reserved","quarantine","stock_value_vnd"],"margin" or "top_products"=>["name","net_quantity","product_net_vnd","cost_vnd","gross_margin_vnd"],"promotions"=>["code_normalized","consumed","reserved","released","consumed_discount_vnd"],"trade_in"=>["created_at","reception_method","state","item_count","issued_credit_vnd"],"warranty"=>["sku","state","created_at","replacement_cost_vnd","credit_note_vnd"],_=>[]};
    private void SetChart(string report,List<JsonElement> rows)
    {
        string label=report switch{"inventory"=>"sku","promotions"=>"code_normalized","trade_in" or "warranty"=>"state",_=>"name"};
        string amount=report switch{"inventory"=>"available","margin"=>"gross_margin_vnd","top_products"=>"net_quantity","promotions"=>"consumed",_=>""};
        var points=rows.GroupBy(x=>Ui.Text(x,label)).Select(g=>new InsightPoint(label=="state"?ModuleView.ValueLabel("state",g.Key):g.Key,amount.Length==0?g.Count():g.Sum(x=>InsightCards.Number(x,amount)))).OrderByDescending(x=>x.Value).ToList();
        var title=report switch{"inventory"=>"Số hàng có thể bán","margin"=>"Lãi gộp theo sản phẩm","top_products"=>"Số sản phẩm bán sau khi trừ hàng trả","promotions"=>"Lượt sử dụng mã ưu đãi",_=>"Hồ sơ theo trạng thái"};
        chart.Kind=InsightKind.Bars;chart.SetPoints(points.Take(12),title+(points.Count>12?$" · 12 / {points.Count} mục":""),report=="margin"?"₫":report=="promotions"?"lượt":amount.Length==0?"hồ sơ":"sản phẩm");
        summary.Text=$"{rows.Count:N0} dòng đã tải · "+(report=="inventory"?"Tồn hiện tại, không lọc ngày":$"Tổng: {points.Sum(x=>x.Value):N0}"+(report=="margin"?" ₫":""));
        chart.AccessibleDescription=title+". Biểu đồ hiển thị tối đa 12 mục lớn nhất; Bảng chi tiết có toàn bộ dòng đã tải.";
    }
    private static string Definition(string report)=>report switch
    {
        "inventory"=>"Số hàng có thể bán, hàng đang giữ cho đơn và hàng chờ kiểm tra ở hiện tại. Đổi ngày không thay đổi báo cáo tồn. Nhân viên kho chỉ xem số lượng.",
        "margin"=>"Lãi gộp = giá trị hàng bán sau điều chỉnh trừ giá vốn. Chưa trừ mọi chi phí vận hành; không phải lợi nhuận cuối cùng. Chi phí đổi bảo hành nằm trong bảng chi tiết.",
        "top_products"=>"So sánh số sản phẩm bán, đã trừ số lượng trả lại, theo ngày phát sinh trong kỳ.",
        "promotions"=>"Lượt mã ưu đãi đã sử dụng của đơn tạo trong kỳ. Lượt đang giữ và đã trả lại xem trong Bảng chi tiết.",
        "trade_in"=>"Hồ sơ thu cũ tạo trong kỳ. Số dư mua hàng đã cấp cho khách và phí giao nhận xem trong Bảng chi tiết.",
        _=>"Hồ sơ bảo hành mở rộng tạo trong kỳ. Chi phí thay thế và số tiền điều chỉnh xem trong Bảng chi tiết."
    };
    internal IReadOnlyList<string> ReportKinds=>kind.Items.Cast<Choice>().Select(x=>x.Value).ToList();
    internal async Task SelectReportAsync(string value){for(var i=0;i<kind.Items.Count;i++)if(((Choice)kind.Items[i]!).Value==value){kind.SelectedIndex=i;await RefreshAsync();return;}throw new InvalidOperationException("Vai trò không có báo cáo đã chọn.");}
    private static readonly Dictionary<string,string> Labels=new(StringComparer.OrdinalIgnoreCase){["model_code"]="Mẫu sản phẩm",["received_at"]="Nhập lúc",["unit_cost_vnd"]="Giá vốn đơn vị",["stock_value_vnd"]="Giá trị tồn",["net_quantity"]="Số lượng ròng",["product_net_vnd"]="Doanh thu ròng",["care_replacement_cost_vnd"]="Chi phí đổi Care",["code_normalized"]="Mã ưu đãi",["consumed"]="Đã dùng",["released"]="Đã trả",["consumed_discount_vnd"]="Giảm giá đã dùng",["reception_method"]="Tiếp nhận",["created_at"]="Tạo lúc",["item_count"]="Số món",["issued_credit_vnd"]="Số dư đã cấp",["freight_vnd"]="Phí vận đơn",["replacement_cost_vnd"]="Chi phí thay thế",["credit_note_vnd"]="Chứng từ giảm",["damage_kind"]="Loại hư hỏng",["sku"]="Mã phiên bản",["name"]="Sản phẩm",["product_name"]="Sản phẩm",["available"]="Có thể bán",["reserved"]="Đang giữ",["quarantine"]="Chờ kiểm tra",["qty_on_hand"]="Số lượng trong lô",["qty_reserved"]="Đang giữ",["quantity"]="Số lượng",["quantity_sold"]="Số đã bán",["revenue_vnd"]="Doanh thu VND",["net_revenue_vnd"]="Doanh thu ròng VND",["cost_vnd"]="Giá vốn VND",["margin_vnd"]="Lãi gộp VND",["gross_margin_vnd"]="Lãi gộp VND",["inventory_value_vnd"]="Giá trị tồn VND",["inventory_cost_vnd"]="Giá vốn tồn VND",["total_vnd"]="Tổng VND",["code"]="Mã",["promotion_code"]="Mã ưu đãi",["status"]="Trạng thái",["state"]="Trạng thái",["count"]="Số hồ sơ",["discount_vnd"]="Giảm giá VND",["credit_vnd"]="Số dư đã cấp (₫)",["issued_vnd"]="Giá trị phát hành VND",["refund_vnd"]="Hoàn VND",["period"]="Kỳ"};
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==(Keys.Control|Keys.F)){query.Focus();query.SelectAll();return true;}return base.ProcessCmdKey(ref msg,keyData);}
    protected override void Dispose(bool disposing){if(disposing){definition.Dispose();debounce.Dispose();(binding.DataSource as System.Data.DataTable)?.Dispose();binding.Dispose();}base.Dispose(disposing);}
}
