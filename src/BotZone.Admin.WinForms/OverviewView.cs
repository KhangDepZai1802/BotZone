using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class OverviewView : PageView
{
    private readonly Actor actor;
    private readonly Action<string> navigate;
    private readonly Action<string>? openOrders;
    private readonly TableLayoutPanel metrics = new() { Dock=DockStyle.Top,ColumnCount=3,RowCount=1 };
    private readonly TableLayoutPanel visuals = new() { Dock=DockStyle.Top,ColumnCount=2,RowCount=1 };
    private readonly InsightChart distribution = new() { Dock=DockStyle.Fill,Kind=InsightKind.Donut,Margin=new Padding(0,0,12,12),Name="orderDistribution" };
    private readonly InsightChart stock = new() { Dock=DockStyle.Fill,Margin=new Padding(0,0,0,12),Name="lowStockChart" };
    private readonly FlowLayoutPanel nextWork = Ui.Bar();
    private readonly TableLayoutPanel body = new() { Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=5 };
    private readonly DateTimePicker from=new BzDateTimePicker(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Width=180},to=new BzDateTimePicker(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Width=180};
    private readonly Label period = Ui.Label("");
    private OverviewMetric[] metricDetails=[];
    private string metricPeriod="";
    private int selectedOrder;
    public OverviewView(ApiClient api,Actor actor,Action<string> navigate,Action<string>? openOrders=null):base(api)
    {
        this.actor=actor;this.navigate=navigate;this.openOrders=openOrders;Dock=DockStyle.Fill;
        var today=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;from.Value=new DateTime(today.Year,today.Month,1);to.Value=today.AddDays(1);
        var owner=actor.Role=="owner";
        for(var i=0;i<3;i++)metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));metrics.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        visuals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,55));visuals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,45));visuals.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        visuals.Controls.Add(distribution,0,0);visuals.Controls.Add(stock,1,0);
        var dateActions=owner?Ui.Bar(period,Ui.Button("Đổi khoảng ngày",async(_,_)=>await ChoosePeriodAsync()),Ui.Button("Giải thích số liệu",async(_,_)=>{using var dialog=new OverviewMetricsDialog(metricDetails,metricPeriod);await dialog.ShowDialogAsync(FindForm()??(IWin32Window)this);})):Ui.Bar(period);
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(var c in new Control[]{dateActions,metrics,nextWork,visuals,Status}){body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.Controls.Add(c);}
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};scroll.Controls.Add(body);
        Controls.Add(Ui.Vertical(Ui.PageHeader(owner?"Tình hình cửa hàng":"Công việc kho","Nhìn nhanh tình hình và mở việc cần làm.",Ui.Button(owner?"Xem báo cáo":"Mở tồn kho",(_,_)=>navigate(owner?"reports":"inventory"),true),Ui.Button("Làm mới",async(_,_)=>await RefreshAsync())),scroll));
        distribution.RowSelected+=i=>selectedOrder=i;distribution.RowActivated+=()=>{if(owner&&distribution.Points.Count>selectedOrder)OpenOrders(distribution.Points[selectedOrder].Key??"");else navigate("purchases");};
        stock.RowActivated+=()=>navigate("inventory");
        SizeChanged+=(_,_)=>Adapt();Adapt();
    }
    private void OpenOrders(string state){if(openOrders is not null)openOrders(state);else navigate("orders");}
    private void Adapt()
    {
        var scale=DeviceDpi/96f;var compact=ClientSize.Width/scale<930;
        metrics.Height=(int)((compact?112:144)*scale);visuals.Height=(int)((compact?630:330)*scale);
        foreach(TableLayoutPanel card in metrics.Controls){card.Padding=new Padding((int)((compact?12:18)*scale));var value=card.Controls[1];var desired=compact?17f:22f;if(Math.Abs(value.Font.SizeInPoints-desired)>.1f){var old=value.Font;value.Font=new Font("Segoe UI Semibold",desired);old.Dispose();}card.Controls[2].Visible=!compact;card.RowStyles[0].Height=28*scale;card.RowStyles[2].Height=compact?0:36*scale;}
        if(Controls[0] is TableLayoutPanel outer&&outer.Controls[0] is PageHeading heading)heading.CompactDescription(compact);
        visuals.SuspendLayout();visuals.Controls.Remove(stock);visuals.RowStyles.Clear();visuals.ColumnStyles[0].Width=compact?100:55;visuals.ColumnStyles[1].Width=compact?0:45;
        visuals.RowCount=compact?2:1;visuals.RowStyles.Add(new RowStyle(SizeType.Percent,compact?50:100));if(compact)visuals.RowStyles.Add(new RowStyle(SizeType.Percent,50));visuals.Controls.Add(stock,compact?0:1,compact?1:0);visuals.ResumeLayout();
    }
    public override Task RefreshAsync()=>LoadLatestAsync(async token=>
    {
        var overview=await Api.GetAsync<JsonElement>("api/admin/overview",token);var values=new List<InvoiceStatisticsResult>();JsonElement purchases=default;
        if(actor.Role=="owner")foreach(var metric in new[]{"issued","adjustments","net"})values.Add(await Api.PostAsync<InvoiceStatisticsResult>("api/admin/reports/invoices",new ReportRequest{Metric=metric,Query=new(){FromInclusive=from.Value.Date.ToString("yyyy-MM-dd"),ToExclusive=to.Value.Date.ToString("yyyy-MM-dd")}},token));
        else purchases=await Api.GetAsync<JsonElement>("api/admin/data/purchases?page=1&pageSize=20",token);
        return(overview,values,purchases);
    },data=>
    {
        foreach(Control c in metrics.Controls.Cast<Control>().ToArray())c.Dispose();foreach(Control c in nextWork.Controls.Cast<Control>().ToArray())c.Dispose();
        var low=Ui.Rows(data.overview,"lowStock");var owner=actor.Role=="owner";
        period.Text=owner?$"Số tiền trong kỳ {from.Value:dd/MM} – {to.Value.AddDays(-1):dd/MM/yyyy}":"Tình hình nhận hàng và tồn kho hiện tại";
        var states=owner?Ui.Rows(data.overview,"orders").Select(x=>new InsightPoint(OrdersView.StateLabel(Ui.Text(x,"order_status")),InsightCards.Number(x,"count"),Ui.Text(x,"order_status"))).ToList():Ui.Rows(data.purchases).GroupBy(x=>Ui.Text(x,"status")).Select(g=>new InsightPoint(ModuleView.ValueLabel("status",g.Key),g.Count(),g.Key)).ToList();
        distribution.Kind=owner?InsightKind.Donut:InsightKind.Bars;
        distribution.SetPoints(states,owner?"Đơn hàng hiện tại · mọi thời điểm":"20 phiếu nhập gần đây",owner?"đơn":"phiếu");
        stock.SetPoints(low.Select(x=>new InsightPoint(Ui.Text(x,"sku"),InsightCards.Number(x,"available"))),$"Hàng sắp hết · {low.Count} mã dưới 5 sản phẩm","sản phẩm");
        if(owner)
        {
            string[] titles=["Giá trị hóa đơn","Đã điều chỉnh giảm","Còn lại sau điều chỉnh"],notes=["Hóa đơn phát hành trong kỳ","Chứng từ giảm phát sinh trong kỳ","Có thể âm nếu giảm của kỳ trước"];
            for(var i=0;i<3;i++)metrics.Controls.Add(InsightCards.Metric(titles[i],Ui.Money(data.values[i].TotalVnd),notes[i]),i,0);
            string[] explanations=["Giá trị các hóa đơn được phát hành trong kỳ đã chọn.","Số tiền giảm theo chứng từ điều chỉnh phát sinh trong kỳ.","Giá trị hóa đơn trừ khoản điều chỉnh giảm của kỳ. Có thể âm nếu điều chỉnh hóa đơn từ kỳ trước."];
            metricDetails=data.values.Select((v,i)=>new OverviewMetric(titles[i],Ui.Money(v.TotalVnd),explanations[i])).ToArray();metricPeriod=period.Text;
            nextWork.Controls.Add(Ui.Button("Tất cả đơn hàng",(_,_)=>OpenOrders("")));
            foreach(var state in new[]{"pending","confirmed","reconciliation"}) {var count=states.FirstOrDefault(x=>x.Key==state)?.Value??0;if(count>0)nextWork.Controls.Add(Ui.Button($"{OrdersView.StateLabel(state)} · {count:N0}",(_,_)=>OpenOrders(state),state=="pending"));}
            nextWork.Controls.Add(Ui.Button("Bổ sung hàng",(_,_)=>navigate("purchases")));
        }
        else
        {
            metrics.Controls.Add(InsightCards.Metric("Phiếu đã tải",Ui.Rows(data.purchases).Count.ToString(),"20 phiếu gần đây nhất"),0,0);
            metrics.Controls.Add(InsightCards.Metric("Cần bổ sung",low.Count.ToString(),"Mã hàng còn dưới 5 sản phẩm"),1,0);
            metrics.Controls.Add(InsightCards.Metric("Đang nhận dở",states.Where(x=>x.Key=="part_received").Sum(x=>x.Value).ToString(),"Trong các phiếu đã tải"),2,0);
            nextWork.Controls.AddRange([Ui.Button("Nhận hàng",(_,_)=>navigate("purchases"),true),Ui.Button("Bàn giao đơn",(_,_)=>navigate("orders")),Ui.Button("Kiểm kê",(_,_)=>navigate("stock_adjustments"))]);
        }
        Status.Text="Mũi tên chọn mục trong biểu đồ · Enter mở danh sách liên quan · Cập nhật "+DateTime.Now.ToString("HH:mm");Adapt();
    });
    private async Task ChoosePeriodAsync()
    {
        using var dialog=new OverviewPeriodDialog(from.Value,to.Value.AddDays(-1));
        if(await dialog.ShowDialogAsync(FindForm()??(IWin32Window)this)==DialogResult.OK){from.Value=dialog.StartDate;to.Value=dialog.EndDate.AddDays(1);await RefreshAsync();}
    }
}
