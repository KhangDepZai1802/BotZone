using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class OverviewView : PageView
{
    private readonly Actor actor;private readonly Action<string> navigate;
    private readonly FlowLayoutPanel cards=new(){Dock=DockStyle.Top,AutoSize=true,WrapContents=true};
    private readonly DataGridView grid=Ui.Grid(), stock=Ui.Grid();
    private readonly DateTimePicker from=new BzDateTimePicker(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Width=180},to=new BzDateTimePicker(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Width=180};
    private string metricDetails="Chưa tải dữ liệu.";
    private TableLayoutPanel? metricCard;
    private Label? metricValue;
    private Button? metricInspect;
    public OverviewView(ApiClient api,Actor actor,Action<string> navigate):base(api)
    {
        this.actor=actor;this.navigate=navigate;Dock=DockStyle.Fill;grid.AutoGenerateColumns=true;stock.AutoGenerateColumns=true;
        var today=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;from.Value=new DateTime(today.Year,today.Month,1);to.Value=today.AddDays(1);
        var owner=actor.Role=="owner";var title=owner?"Tổng quan cửa hàng":"Công việc kho";
        var links=owner?Ui.Bar(Ui.Button("Mở đơn hàng",(_,_)=>navigate("orders")),Ui.MenuButton("Công việc khác",("Hoàn tiền",()=>navigate("refunds")),("Kiểm kê",()=>navigate("stock_adjustments")),("Thu cũ / báo giá",()=>navigate("trade_ins")))):Ui.Bar(Ui.Button("Xuất hàng",(_,_)=>navigate("orders"),true),Ui.Button("Nhận hàng",(_,_)=>navigate("purchases")),Ui.MenuButton("Công việc khác",("Kiểm kê",()=>navigate("stock_adjustments")),("Kiểm định hậu mãi",()=>navigate("returns"))));
        var tabs=Ui.Tabs();var work=new TabPage(owner?"Đơn theo trạng thái":"Phiếu nhập gần đây"){BackColor=Ui.Surface};work.Controls.Add(grid);tabs.TabPages.Add(work);var low=new TabPage("Tồn cần chú ý"){BackColor=Ui.Surface};low.Controls.Add(stock);tabs.TabPages.Add(low);
        var menu=owner?Ui.MenuButton("Tùy chọn",("Đổi kỳ tổng quan",async()=>await ChoosePeriodAsync()),("Thông tin chỉ số",()=>Ui.ShowInformation(FindForm(),"Chỉ số tổng quan",metricDetails)),("Làm mới",async()=>await RefreshAsync())):Ui.MenuButton("Tùy chọn",("Làm mới",async()=>await RefreshAsync()),("Phạm vi dữ liệu",()=>Ui.ShowInformation(FindForm(),"Công việc kho","Trang này hiển thị tối đa 20 phiếu nhập gần đây. Mở Nhận hàng để tra cứu toàn bộ phiếu theo quyền hiện tại. Tồn cần chú ý lấy từ dữ liệu tồn kho của cửa hàng.")));
        var header=Ui.PageHeader(title,"",Ui.Button(owner?"Xem báo cáo":"Mở tồn và serial",(_,_)=>navigate(owner?"reports":"inventory"),true),menu);
        Status.Padding=new Padding(20,0,0,0);Status.Margin=new Padding(12,0,0,8);Status.Font=new Font("Segoe UI",9.5f);Status.Anchor=AnchorStyles.Left;
        links.Controls.Add(Status);Controls.Add(Ui.Vertical(header,cards,links,tabs));
        links.SizeChanged+=(_,_)=>Status.MaximumSize=new Size(Math.Max(160,links.ClientSize.Width-Status.Margin.Horizontal-links.Padding.Horizontal),0);
        SizeChanged+=(_,_)=>AdaptCard();
        grid.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;navigate(owner?"orders":"purchases");}};grid.CellDoubleClick+=(_,_)=>navigate(owner?"orders":"purchases");
    }
    public override Task RefreshAsync()=>LoadLatestAsync(async token=>
    {
        var overview=await Api.GetAsync<JsonElement>("api/admin/overview",token);var metrics=new List<InvoiceStatisticsResult>();JsonElement purchases=default;
        if(actor.Role=="owner")foreach(var metric in new[]{"issued","adjustments","net"})metrics.Add(await Api.PostAsync<InvoiceStatisticsResult>("api/admin/reports/invoices",new ReportRequest{Metric=metric,Query=new(){FromInclusive=from.Value.Date.ToString("yyyy-MM-dd"),ToExclusive=to.Value.Date.ToString("yyyy-MM-dd")}},token));
        else purchases=await Api.GetAsync<JsonElement>("api/admin/data/purchases?page=1&pageSize=20",token);
        return(overview,metrics,purchases);
    },data=>
    {
        foreach(Control c in cards.Controls.Cast<Control>().ToArray())c.Dispose();cards.Controls.Clear();
        var titles=new[]{"Tổng HĐ phát hành","Điều chỉnh giảm","Ròng phát sinh kỳ"};var definitions=new[]{"Theo ngày phát hành hóa đơn","Theo ngày chứng từ giảm","Phát hành trừ điều chỉnh trong kỳ"};
        metricCard=null;metricValue=null;metricInspect=null;
        if(data.metrics.Count>0){var panel=new TableLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=2,RowCount=2,Padding=new Padding(20),BackColor=Ui.Surface,Margin=new Padding(0,0,0,16)};var caption=Ui.Label($"Hóa đơn phát hành · {from.Value:dd/MM} – {to.Value.AddDays(-1):dd/MM/yyyy}");caption.ForeColor=Ui.Muted;panel.Controls.Add(caption,0,0);var value=Ui.Label(Ui.Money(data.metrics[0].TotalVnd),true);panel.Controls.Add(value,0,1);var inspect=Ui.Button("Xem cơ cấu",(_,_)=>Ui.ShowInformation(FindForm(),"Chỉ số tổng quan",metricDetails));inspect.Margin=new Padding(48,0,0,0);panel.Controls.Add(inspect,1,1);cards.Controls.Add(panel);metricCard=panel;metricValue=value;metricInspect=inspect;AdaptCard();}
        metricDetails=string.Join("\r\n\r\n",data.metrics.Select((metric,index)=>$"{titles[index]}: {Ui.Money(metric.TotalVnd)}\r\n{definitions[index]}\r\nĐọc lúc {Ui.VietDate(metric.ReadAt)}"));
        grid.DataSource=Ui.Table(actor.Role=="owner"?Ui.Rows(data.overview,"orders"):Ui.Rows(data.purchases));stock.DataSource=Ui.Table(Ui.Rows(data.overview,"lowStock"));
        Status.Text=actor.Role=="owner"?$"Cập nhật {Ui.VietDate(data.metrics.Last().ReadAt)}":"Tối đa 20 phiếu gần đây · Mở Nhận hàng để xem thêm.";
    });
    private void AdaptCard()
    {
        if(metricCard is null||metricCard.IsDisposed||metricValue is null||metricInspect is null)return;
        var compact=ClientSize.Height*96/Math.Max(96,DeviceDpi)<600;
        metricCard.Padding=new Padding(compact?12:20);metricCard.Margin=new Padding(0,0,0,compact?8:16);metricInspect.Margin=new Padding(compact?24:48,0,0,0);
        var size=compact?22f:26f;if(Math.Abs(metricValue.Font.SizeInPoints-size)>.1f){var previous=metricValue.Font;metricValue.Font=new Font("Segoe UI",size,FontStyle.Bold);previous.Dispose();}
    }
    private async Task ChoosePeriodAsync()
    {
        using var dialog=new Form{Text="Kỳ tổng quan",Size=new Size(560,320),StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false};Ui.Prepare(dialog);
        var start=new BzDateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Value=from.Value,Width=190};var end=new BzDateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Value=to.Value,Width=190};
        var apply=Ui.Button("Áp dụng",(_,_)=>{if(start.Value.Date>=end.Value.Date){Ui.ShowError(dialog,new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu."));end.Focus();return;}dialog.DialogResult=DialogResult.OK;},true);var cancel=Ui.Button("Hủy",(_,_)=>dialog.Close());dialog.AcceptButton=apply;dialog.CancelButton=cancel;
        dialog.Controls.Add(Ui.Vertical(Ui.PageHeader("Kỳ tổng quan",""),Ui.Bar(Ui.Field("Từ ngày",start),Ui.Field("Trước ngày",end)),Ui.Bar(apply,cancel)));
        if(await dialog.ShowDialogAsync(FindForm()??(IWin32Window)this)==DialogResult.OK){from.Value=start.Value.Date;to.Value=end.Value.Date;await RefreshAsync();}
    }
}
