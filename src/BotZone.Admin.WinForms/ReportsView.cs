using System.Globalization;
using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class ReportsView : PageView
{
    private readonly ComboBox grouping=Ui.Combo(new("period","Theo thời gian"),new("staff","Theo nhân viên"),new("product","Theo sản phẩm"));
    private readonly ComboBox granularity=Ui.Combo(new("month","Tháng"),new("quarter","Quý"),new("year","Năm"));
    private readonly ComboBox metric=Ui.Combo(new("issued","Giá trị phát hành"),new("adjustments","Điều chỉnh giảm"),new("net","Ròng phát sinh"),new("receipts","Tiền đã thu"),new("refunds","Tiền đã hoàn"));
    private readonly ComboBox productLevel=Ui.Combo(new("model","Model"),new("sku","SKU"));
    private readonly ComboBox dimension=Ui.Combo(new Choice("","Tất cả đối tượng"));
    private readonly DateTimePicker from=new BzDateTimePicker(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Value=new DateTime(2024,1,1),Width=180};
    private readonly DateTimePicker to=new BzDateTimePicker(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Value=new DateTime(2027,1,1),Width=180};
    private readonly DataGridView grid=Ui.Grid();
    private readonly BindingSource binding=new();
    private readonly Label total=Ui.Label("—");
    private readonly TabControl views=Ui.Tabs();
    private readonly Button export,advanced;
    private readonly CheckBox showChart=new(){Text="Biểu đồ",AutoSize=true};
    private readonly ReportBars chart=new(){Dock=DockStyle.Fill,Visible=false};
    private readonly Action<InvoiceQuery> drillDown;
    private InvoiceStatisticsResult? result;
    private FilterNode? filter;
    private ReportRequest? loadedRequest;
    private long dimensionsGeneration;
    private string loadedProductLevel="model";
    private CancellationTokenSource? dimensionsCancellation;
    private bool dimensionsLoaded;
    private HashSet<string>? selectedColumns;

    public ReportsView(ApiClient api,Action<InvoiceQuery> drillDown):base(api)
    {
        this.drillDown=drillDown;Dock=DockStyle.Fill;
        foreach(var col in new[]{("Period","Kỳ"),("DimensionName","Đối tượng"),("InvoiceCount","Số hóa đơn"),("Quantity","Số lượng hàng"),("TotalVnd","Giá trị"),("CashDemoVnd","Tiền mô phỏng"),("StoreCreditVnd","Store credit")})Ui.Column(grid,col.Item1,col.Item2,col.Item1=="DimensionName"?220:150);
        grid.DataSource=binding;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
        grid.CellFormatting+=(_,e)=>
        {
            if(e.ColumnIndex<0)return;var property=grid.Columns[e.ColumnIndex].DataPropertyName;
            if(e.Value is string s&&property.EndsWith("Vnd")){e.Value=Ui.Money(s);e.FormattingApplied=true;}
            else if(property=="Period"&&e.Value is string period&&DateTime.TryParse(period,CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)){e.Value=result?.Granularity=="year"?date.ToString("yyyy"):result?.Granularity=="quarter"?$"Quý {(date.Month-1)/3+1}/{date.Year}":date.ToString("MM/yyyy");e.FormattingApplied=true;}
        };
        var apply=Ui.Button("&Áp dụng",async(_,_)=>await RefreshAsync());export=Ui.Button("&Xuất báo cáo",async(_,_)=>await ExportAsync(),true);export.Enabled=false;
        advanced=Ui.Button("Lọc tập hóa đơn",async(_,_)=>await AdvancedAsync());
        granularity.Width=140;metric.Width=240;productLevel.Width=150;dimension.Width=260;
        var productField=Ui.Field("Cấp sản phẩm",productLevel);var dimensionField=Ui.Field("Đối tượng",dimension);productField.Visible=dimensionField.Visible=false;
        var extra=Ui.Bar(Ui.Field("Nhóm kỳ",granularity),productField,dimensionField,advanced);extra.Visible=false;extra.BackColor=Color.White;extra.Padding=new Padding(12,4,12,8);
        Button? more=null;more=Ui.Button("Thêm bộ lọc",(_,_)=>{extra.Visible=!extra.Visible;more!.Text=extra.Visible?"Thu gọn bộ lọc":"Thêm bộ lọc";Ui.Pulse(extra);});
        apply.Anchor=more.Anchor=advanced.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;
        var filters=Ui.Bar(Ui.Field("Từ ngày",from),Ui.Field("Trước ngày",to),Ui.Field("Chỉ tiêu",metric),apply,more);filters.BackColor=Color.White;filters.Padding=new Padding(12,8,12,4);
        total.Font=new Font("Segoe UI",16,FontStyle.Bold);total.ForeColor=Ui.Primary;total.Padding=new Padding(12);total.Margin=new Padding(0,8,0,8);
        var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(12)};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(var i=0;i<3;i++)panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.Controls.Add(filters,0,0);panel.Controls.Add(extra,0,1);panel.Controls.Add(total,0,2);
        var dataHost=new Panel{Dock=DockStyle.Fill};dataHost.Controls.Add(grid);dataHost.Controls.Add(chart);panel.Controls.Add(dataHost,0,3);panel.Controls.Add(Status,0,4);
        var actions=Ui.MenuButton("Thao tác",("Mở hóa đơn nguồn",Drill),("Cột hiển thị",async()=>{if(await FinancePanels.ChooseColumnsAsync(FindForm()!,grid,"Cột báo cáo",result?.GroupBy=="period"?["Period","TotalVnd"]:["Period","TotalVnd","DimensionName"],result?.GroupBy=="period"?["DimensionName"]:[])){selectedColumns=grid.Columns.Cast<DataGridViewColumn>().Where(c=>c.Visible).Select(c=>c.Name).ToHashSet();ConfigureColumns();}}),("Bảng / biểu đồ",()=>showChart.Checked=!showChart.Checked),("Thông tin báo cáo",ShowReportInfo));
        foreach(var title in new[]{"Theo thời gian","Theo nhân viên","Theo sản phẩm"})views.TabPages.Add(new TabPage(title));views.TabPages[0].Controls.Add(panel);
        var layout=new Panel{Dock=DockStyle.Fill};layout.Controls.Add(views);layout.Controls.Add(Ui.PageHeader("Thống kê hóa đơn",api.Actor?.Role=="order_staff"?"Dữ liệu của tôi":"",export,actions));Controls.Add(layout);
        views.SelectedIndexChanged+=async(_,_)=>{if(views.SelectedTab is null)return;views.SelectedTab.Controls.Add(panel);grouping.SelectedIndex=views.SelectedIndex;productField.Visible=views.SelectedIndex==2;dimensionField.Visible=views.SelectedIndex!=0;await LoadDimensionsAsync();if(!IsDisposed)await RefreshAsync();};
        productLevel.SelectedIndexChanged+=async(_,_)=>{await LoadDimensionsAsync();if(!IsDisposed)Status.Text="Đã đổi cấp sản phẩm. Chọn Áp dụng để cập nhật.";};
        showChart.CheckedChanged+=(_,_)=>{chart.Visible=showChart.Checked;grid.Visible=!showChart.Checked;if(showChart.Checked){chart.SelectRow(binding.Position);chart.BringToFront();}Ui.Pulse(dataHost);};
        chart.RowSelected+=index=>{if(index>=0&&index<binding.Count)binding.Position=index;};chart.RowActivated+=Drill;
        grid.CellDoubleClick+=(_,e)=>{if(e.RowIndex>=0)Drill();};grid.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.Handled=e.SuppressKeyPress=true;Drill();}};
        foreach(var input in new Control[]{metric,granularity,dimension,from,to}){if(input is ComboBox combo)combo.SelectedIndexChanged+=(_,_)=>{if(result is not null)Status.Text="Có thay đổi chưa áp dụng.";};else if(input is DateTimePicker date)date.ValueChanged+=(_,_)=>{if(result is not null)Status.Text="Có thay đổi chưa áp dụng.";};}
        Status.TextChanged+=(_,_)=>Status.Visible=!string.IsNullOrWhiteSpace(Status.Text);ConfigureColumns();
    }
    private void ConfigureColumns()
    {
        var group=result?.GroupBy??Ui.Selected(grouping);
        foreach(DataGridViewColumn column in grid.Columns)
        {
            var core=column.Name is "Period" or "InvoiceCount" or "TotalVnd"||column.Name=="DimensionName"&&group!="period";
            column.Visible=column.Name=="DimensionName"?group!="period":selectedColumns?.Contains(column.Name)??core;column.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
            column.FillWeight=column.Name switch{"DimensionName"=>40,"Period"=>25,"TotalVnd"=>30,_=>15};column.DefaultCellStyle.Alignment=column.Name.EndsWith("Vnd")?DataGridViewContentAlignment.MiddleRight:column.Name is "InvoiceCount" or "Quantity"?DataGridViewContentAlignment.MiddleCenter:DataGridViewContentAlignment.MiddleLeft;
        }
    }
    private ReportRequest Request()=>new(){Query=new InvoiceQuery{FromInclusive=from.Value.Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),ToExclusive=to.Value.Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),Filter=filter},GroupBy=Ui.Selected(grouping),Granularity=Ui.Selected(granularity),Metric=Ui.Selected(metric),DimensionId=string.IsNullOrEmpty(Ui.Selected(dimension))?null:Ui.Selected(dimension),ProductLevel=Ui.Selected(productLevel)};
    public override async Task RefreshAsync()
    {
        Deactivate();if(!dimensionsLoaded){await LoadDimensionsAsync();if(IsDisposed||Parent is null)return;}
        if(from.Value.Date>=to.Value.Date){Status.Text="Ngày kết thúc phải sau ngày bắt đầu.";Status.ForeColor=Ui.Danger;to.Focus();return;}
        var request=Request();var label=(metric.SelectedItem as Choice)?.Label??request.Metric;
        await LoadLatestAsync(token=>Api.PostAsync<InvoiceStatisticsResult>("api/admin/reports/invoices",request,token),data=>{var position=binding.Position;result=data;loadedRequest=request;loadedProductLevel=request.ProductLevel;binding.DataSource=data.Rows;if(position>=0&&position<data.Rows.Count)binding.Position=position;ConfigureColumns();total.Text=$"{label} · {Ui.Money(data.TotalVnd)}";chart.SetData(data,label);if(showChart.Checked)chart.SelectRow(binding.Position);export.Enabled=true;Status.Text=data.Rows.Count==0?"Không có dữ liệu trong kỳ.":"";});
    }
    private async Task AdvancedAsync()
    {
        using var busy=Ui.Busy(advanced,"Đang tải…");try{var lookups=await Api.GetAsync<JsonElement>("api/admin/lookups");if(IsDisposed)return;using var dialog=new FilterDialog(filter,lookups);if(await dialog.ShowDialogAsync(FindForm()??(IWin32Window)this)==DialogResult.OK&&!IsDisposed){filter=dialog.Filter;advanced.Text=filter is null?"Lọc tập hóa đơn":"Lọc tập hóa đơn · Đang dùng";await RefreshAsync();}}catch(Exception ex){if(!IsDisposed){Status.Text=Ui.Error(ex);Status.ForeColor=Ui.Danger;}}
    }
    private async Task LoadDimensionsAsync()
    {
        dimensionsCancellation?.Cancel();dimensionsCancellation?.Dispose();dimensionsCancellation=new();var token=dimensionsCancellation.Token;var current=++dimensionsGeneration;
        dimensionsLoaded=true;dimension.Items.Clear();dimension.Items.Add(new Choice("","Tất cả đối tượng"));dimension.SelectedIndex=0;dimension.Enabled=Ui.Selected(grouping)!="period";productLevel.Enabled=Ui.Selected(grouping)=="product";
        if(!dimension.Enabled)return;if(Ui.Selected(grouping)=="staff"&&Api.Actor is {Role:"order_staff"} actor){dimension.Items.Clear();dimension.Items.Add(new Choice(actor.Id,actor.Name+" · Dữ liệu của tôi"));dimension.SelectedIndex=0;dimension.Enabled=false;return;}
        var key=Ui.Selected(grouping)=="staff"?"staff":Ui.Selected(productLevel)=="sku"?"variants":"products";
        try{var lookups=await Api.GetAsync<JsonElement>("api/admin/lookups",token);if(current!=dimensionsGeneration||IsDisposed)return;foreach(var item in Ui.Rows(lookups,key))dimension.Items.Add(new Choice(Ui.Text(item,"id"),Ui.Text(item,"name","sku")));}catch(OperationCanceledException){}catch(Exception ex){if(current==dimensionsGeneration&&!IsDisposed)Status.Text=Ui.Error(ex);}
    }
    private void ShowReportInfo()
    {
        if(loadedRequest is not {} request||result is null)return;
        Ui.ShowInformation(FindForm(),"Thông tin báo cáo",$"{MetricDefinition(request.Metric)}.\n\nKỳ: {request.Query.FromInclusive} → trước {request.Query.ToExclusive} (giờ Việt Nam).\n{FilterDialog.Describe(request.Query.Filter)}.\n\n{result.Rows.Count} dòng · Dữ liệu đọc lúc {Ui.VietDate(result.ReadAt)}.\nStore credit là nguồn thanh toán; các cột đối soát có trong Cột hiển thị.");
    }
    private async Task ExportAsync()
    {
        if(loadedRequest is not {} r||result is null)return;using var busy=Ui.Busy(export,"Đang xuất…");await ExportFlow.RunAsync(FindForm()!,Api,new ExportRequest{Kind="report",Formats=["xlsx","pdf"],Query=r.Query,GroupBy=r.GroupBy,Granularity=r.Granularity,Metric=r.Metric,DimensionId=r.DimensionId,ProductLevel=r.ProductLevel},$"{r.Query.FromInclusive} → trước {r.Query.ToExclusive} · Tổng đang xem {Ui.Money(result.TotalVnd)}.");
    }
    private void Drill()
    {
        if(binding.Current is not InvoiceStatisticDto row||result is null)return;
        if(result.Metric!="issued"){Status.Text="Mở hóa đơn nguồn áp dụng cho chỉ tiêu giá trị phát hành.";return;}
        if(result.GroupBy=="product"&&loadedProductLevel=="sku"){Status.Text="Chọn cấp Model trong Thêm bộ lọc để mở tập hóa đơn nguồn.";return;}
        var start=DateTime.TryParse(row.Period,CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)?date:from.Value.Date;var end=result.Granularity=="year"?start.AddYears(1):start.AddMonths(result.Granularity=="quarter"?3:1);
        FilterNode? rowFilter=result.GroupBy=="staff"&&row.DimensionId is not null?new InvoiceRuleFilter("responsibleStaffId","=",JsonSerializer.SerializeToElement(row.DimensionId)):result.GroupBy=="product"&&Guid.TryParse(row.DimensionId,out var product)?new InvoiceProductFilter(product,"contains"):null;
        var currentFilter=loadedRequest?.Query.Filter;if(currentFilter is not null&&rowFilter is not null)rowFilter=new InvoiceGroupFilter("AND",[currentFilter,rowFilter]);else rowFilter??=currentFilter;
        var lower=new DateTimeOffset(DateTime.SpecifyKind(start,DateTimeKind.Unspecified),TimeSpan.FromHours(7));var upper=new DateTimeOffset(DateTime.SpecifyKind(end,DateTimeKind.Unspecified),TimeSpan.FromHours(7));var reportFrom=DateTimeOffset.Parse(result.FromInclusive,CultureInfo.InvariantCulture);var reportTo=DateTimeOffset.Parse(result.ToExclusive,CultureInfo.InvariantCulture);if(lower<reportFrom)lower=reportFrom;if(upper>reportTo)upper=reportTo;
        drillDown(new InvoiceQuery{Filter=rowFilter,FromInclusive=lower.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz",CultureInfo.InvariantCulture),ToExclusive=upper.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz",CultureInfo.InvariantCulture)});
    }
    protected override void Dispose(bool disposing){if(disposing){var active=dimensionsCancellation;dimensionsCancellation=null;active?.Cancel();active?.Dispose();grouping.Dispose();showChart.Dispose();binding.Dispose();}base.Dispose(disposing);}
    private static string MetricDefinition(string metric)=>metric switch{"adjustments"=>"Điều chỉnh giảm theo ngày chứng từ giảm, không viết lại kỳ phát hành gốc","net"=>"Ròng phát sinh kỳ = phát hành trong kỳ trừ điều chỉnh trong kỳ; có thể âm","receipts"=>"Tiền đã thu theo giao dịch hoàn tất; tách tiền demo và credit","refunds"=>"Tiền đã hoàn theo giao dịch hoàn tất, khác chứng từ điều chỉnh",_=>"Giá trị hóa đơn phát hành theo ngày hóa đơn; sản phẩm tính riêng giá trị dòng"};
}

internal sealed class ReportBars : ScrollableControl
{
    private List<(string Label,decimal Amount)> rows=[];
    private string caption="Chưa có dữ liệu";
    private int selectedIndex;
    internal event Action<int>? RowSelected;
    internal event Action? RowActivated;
    private int RowHeight=>Math.Max(Font.Height+20,(int)(42*DeviceDpi/96f));
    private int HeaderHeight=>(int)(54*DeviceDpi/96f);
    public ReportBars(){DoubleBuffered=true;AutoScroll=true;TabStop=true;BackColor=Color.White;AccessibleRole=AccessibleRole.List;AccessibleName="Biểu đồ báo cáo";AccessibleDescription="Dùng mũi tên để chọn dòng, Home/End để đến đầu/cuối, Enter để mở hóa đơn nguồn. Bảng có đầy đủ dữ liệu.";}
    internal void SetData(InvoiceStatisticsResult result,string metric)
    {
        string Period(string period)=>DateTime.TryParse(period,CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)?result.Granularity=="year"?date.ToString("yyyy"):result.Granularity=="quarter"?$"Quý {(date.Month-1)/3+1}/{date.Year}":date.ToString("MM/yyyy"):period;
        rows=result.Rows.Select(row=>(result.GroupBy=="period"?Period(row.Period):row.DimensionName+" · "+Period(row.Period),decimal.TryParse(row.TotalVnd,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)?amount:0)).ToList();
        caption=$"{metric} · {rows.Count:N0} dòng · Đơn vị VND";selectedIndex=0;AutoScrollPosition=Point.Empty;MeasureContent();Invalidate();
    }
    private void MeasureContent()=>AutoScrollMinSize=new Size(0,rows.Count==0?0:HeaderHeight+rows.Count*RowHeight+(int)(12*DeviceDpi/96f));
    protected override void OnResize(EventArgs e){base.OnResize(e);MeasureContent();Invalidate();}
    protected override void OnScroll(ScrollEventArgs se){base.OnScroll(se);Invalidate();}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
    internal void SelectRow(int index)
    {
        if(rows.Count==0)return;selectedIndex=Math.Clamp(index,0,rows.Count-1);var top=HeaderHeight+selectedIndex*RowHeight;var bottom=top+RowHeight;var offset=-AutoScrollPosition.Y;
        if(selectedIndex==0)AutoScrollPosition=Point.Empty;else if(top<offset)AutoScrollPosition=new Point(0,top);else if(bottom>offset+ClientSize.Height)AutoScrollPosition=new Point(0,bottom-ClientSize.Height);
        AccessibleDescription=$"{rows[selectedIndex].Label}: {Ui.Money(rows[selectedIndex].Amount.ToString(CultureInfo.InvariantCulture))}. Dùng mũi tên để đổi dòng; Enter mở hóa đơn nguồn.";RowSelected?.Invoke(selectedIndex);Invalidate();
    }
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();var y=e.Y-AutoScrollPosition.Y-HeaderHeight;if(y>=0&&y/RowHeight<rows.Count)SelectRow(y/RowHeight);}
    protected override bool ProcessCmdKey(ref Message message,Keys keyData)
    {
        if(rows.Count>0){var page=Math.Max(1,ClientSize.Height/RowHeight);var next=keyData switch{Keys.Up=>selectedIndex-1,Keys.Down=>selectedIndex+1,Keys.PageUp=>selectedIndex-page,Keys.PageDown=>selectedIndex+page,Keys.Home=>0,Keys.End=>rows.Count-1,_=>-1};if(keyData is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End){SelectRow(next);return true;}if(keyData==Keys.Enter){RowActivated?.Invoke();return true;}}
        return base.ProcessCmdKey(ref message,keyData);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var scale=DeviceDpi/96f;var margin=(int)(16*scale);var scroll=AutoScrollPosition.Y;TextRenderer.DrawText(e.Graphics,caption,Font,new Rectangle(margin,margin+scroll,Math.Max(0,ClientSize.Width-margin*2),HeaderHeight-margin),Ui.Muted,TextFormatFlags.EndEllipsis);
        if(rows.Count==0){TextRenderer.DrawText(e.Graphics,"Không có giao dịch trong kỳ.",Font,ClientRectangle,Ui.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
        var max=rows.Max(row=>Math.Abs(row.Amount));var height=RowHeight;var labelWidth=Math.Min((int)(280*scale),ClientSize.Width/3);var numberWidth=(int)(170*scale);var available=Math.Max(1,ClientSize.Width-labelWidth-numberWidth-margin*4);var signed=rows.Any(row=>row.Amount<0);var origin=margin*2+labelWidth+(signed?available/2:0);var extent=signed?available/2:available;
        using var fill=new SolidBrush(Ui.Primary);using var track=new SolidBrush(Ui.Paper);
        for(var i=0;i<rows.Count;i++){var y=HeaderHeight+i*height+scroll;if(y+height<0||y>ClientSize.Height)continue;var row=rows[i];if(i==selectedIndex&&Focused){using var outline=new Pen(Ui.Focus);e.Graphics.DrawRectangle(outline,new Rectangle(2,y+2,Math.Max(1,ClientSize.Width-5),height-4));}TextRenderer.DrawText(e.Graphics,row.Label,Font,new Rectangle(margin,y,labelWidth,height),Ui.Ink,TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter);e.Graphics.FillRectangle(track,margin*2+labelWidth,y+height/3,available,Math.Max(8,height/3));if(max>0){var width=(float)(Math.Abs(row.Amount)/max*extent);e.Graphics.FillRectangle(fill,row.Amount<0?origin-width:origin,y+height/3,width,Math.Max(8,height/3));}if(signed){using var axis=new Pen(Ui.ControlBorder);e.Graphics.DrawLine(axis,origin,y+height/4,origin,y+height*3/4);}TextRenderer.DrawText(e.Graphics,Ui.Money(row.Amount.ToString(CultureInfo.InvariantCulture)),Font,new Rectangle(ClientSize.Width-numberWidth-margin,y,numberWidth,height),Ui.Ink,TextFormatFlags.Right|TextFormatFlags.VerticalCenter);}
    }
}
