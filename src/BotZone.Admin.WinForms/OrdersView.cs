using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class OrdersView : PageView
{
    private readonly DataGridView grid=Ui.Grid();private readonly BindingSource binding=new();private List<JsonElement> rows=[];private readonly Button detail;
    private readonly TextBox search=new(){Width=280,PlaceholderText="Tìm mã đơn trong danh sách đã tải…",AccessibleName="Tìm mã đơn"};
    private readonly ComboBox view=Ui.Combo(new("","Tất cả"),new("pending","Chờ xác nhận"),new("confirmed","Chờ kho"),new("handed_off","Đã bàn giao"),new("completed","Hoàn tất"),new("reconciliation","Cần đối soát"),new("expired","Hết hạn"),new("cancelled","Đã hủy"));
    private readonly Panel listHost=new(){Dock=DockStyle.Fill};
    private readonly Label preview=new BusyLabel{Text="Chọn một đơn để xem thanh toán, giao nhận và việc tiếp theo.",AutoSize=true,Padding=new Padding(18,0,0,0),ForeColor=Ui.Muted};
    private readonly Panel previewPanel=new(){Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(20)};
    private readonly TableLayoutPanel work=new(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
    private CancellationTokenSource? previewCancellation;private long previewGeneration;private bool bindingRows,disposingView;
    private string? selectionId;
    internal OrderDetailForm? ActiveDetail{get;private set;}
    public OrdersView(ApiClient api):base(api)
    {
        Dock=DockStyle.Fill;grid.AutoGenerateColumns=true;grid.DataSource=binding;
        var refresh=Ui.Button("Làm mới",async(_,_)=>await RefreshAsync());detail=Ui.Button("Mở đơn đã chọn",async(_,_)=>await Detail(),true);
        work.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));work.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,330));work.RowStyles.Add(new RowStyle(SizeType.Percent,100));work.Controls.Add(grid,0,0);
        preview.Dock=DockStyle.Top;preview.MaximumSize=new Size(300,0);var previewBody=Ui.Vertical(preview,Ui.Bar(Ui.Button("Xem chi tiết đơn",async(_,_)=>await Detail())),new Panel());previewBody.Padding=Padding.Empty;previewPanel.Controls.Add(previewBody);work.Controls.Add(previewPanel,1,0);
        var toolbar=Ui.Bar(Ui.Field("Tìm kiếm",search),Ui.Field("Chế độ xem",view),detail);
        listHost.Controls.Add(Ui.Vertical(Ui.PageHeader(api.Actor?.Role=="warehouse_staff"?"Phiếu xuất hàng":"Hàng chờ xử lý đơn","Mở hồ sơ, kiểm tra thanh toán và thực hiện bước tiếp theo.",refresh),toolbar,Status,work));Controls.Add(listHost);
        grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await Detail();};grid.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Detail();}};
        grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="order_status"){e.Value=StateLabel(Convert.ToString(e.Value)??"");e.FormattingApplied=true;}else if(e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="payment_method"){e.Value=ModuleView.ValueLabel("payment_method",Convert.ToString(e.Value)??"");e.FormattingApplied=true;}};
        grid.SelectionChanged+=async(_,_)=>{if(!bindingRows&&!disposingView&&!Disposing&&!IsDisposed)await PreviewAsync();};search.TextChanged+=(_,_)=>BindRows();view.SelectedIndexChanged+=(_,_)=>{Ui.Pulse(work);BindRows();};
        SizeChanged+=(_,_)=>{var wide=ClientSize.Width*96/Math.Max(96,DeviceDpi)>=1120;previewPanel.Visible=wide;work.ColumnStyles[1].Width=wide?330*DeviceDpi/96:0;};
        VisibleChanged+=(_,_)=>{if(!Visible){previewCancellation?.Cancel();++previewGeneration;((BusyLabel)preview).IsBusy=false;}};
    }
    public override Task RefreshAsync()=>LoadLatestAsync(t=>Api.GetAsync<JsonElement>("api/orders",t),data=>{rows=Ui.Rows(data,"orders");BindRows();});
    private void BindRows()
    {
        if(IsDisposed||disposingView)return;var id=SelectedId()??selectionId;var scroll=grid.FirstDisplayedScrollingRowIndex;var sortColumn=grid.SortedColumn?.Name;var sortOrder=grid.SortOrder;bindingRows=true;
        var q=search.Text.Trim();var state=Ui.Selected(view);var visible=rows.Where(x=>(state.Length==0||Ui.Text(x,"order_status")==state)&&(q.Length==0||Ui.Text(x,"order_no").Contains(q,StringComparison.CurrentCultureIgnoreCase))).ToList();
        binding.DataSource=Ui.Table(visible);
        foreach(DataGridViewColumn column in grid.Columns){column.MinimumWidth=column.Name=="order_no"?160:130;if(column.Name=="payment_method")column.HeaderText="Phương thức";}
        RecordPresentation.ApplyColumns(grid,RecordPresentation.Columns("orders",grid));
        if(sortColumn is not null&&grid.Columns[sortColumn] is {} sorted&&sortOrder!=SortOrder.None)grid.Sort(sorted,sortOrder==SortOrder.Ascending?System.ComponentModel.ListSortDirection.Ascending:System.ComponentModel.ListSortDirection.Descending);
        foreach(DataGridViewRow item in grid.Rows)if(item.DataBoundItem is System.Data.DataRowView row&&Convert.ToString(row["id"])==id){grid.CurrentCell=item.Cells.Cast<DataGridViewCell>().First(c=>c.Visible);item.Selected=true;break;}
        if(grid.CurrentCell is null&&grid.Rows.Count>0)grid.CurrentCell=grid.Rows[0].Cells.Cast<DataGridViewCell>().FirstOrDefault(c=>c.Visible);
        if(scroll>=0&&scroll<grid.Rows.Count)grid.FirstDisplayedScrollingRowIndex=scroll;bindingRows=false;detail.Enabled=visible.Count>0;
        Status.Text=$"{visible.Count} / {rows.Count} đơn đã tải · Phạm vi: tối đa 200 đơn mới nhất. Lọc trong danh sách này.";
        if(visible.Count==0)preview.Text="Không có đơn phù hợp. Xóa từ khóa hoặc chọn Tất cả.";else _=PreviewAsync();
    }
    private string? SelectedId(){if(disposingView||IsDisposed||Disposing)return null;var index=grid.CurrentRow?.Index??-1;return index>=0&&index<binding.Count&&binding[index] is System.Data.DataRowView row&&row.Row.Table.Columns.Contains("id")?Convert.ToString(row["id"]):null;}
    private async Task PreviewAsync()
    {
        var id=SelectedId();selectionId=id;previewCancellation?.Cancel();previewCancellation?.Dispose();previewCancellation=new();var token=previewCancellation.Token;var generation=++previewGeneration;if(id is null)return;
        preview.Text="Đang mở thông tin đơn…";preview.ForeColor=Ui.Muted;((BusyLabel)preview).IsBusy=true;
        try{var data=await Api.GetAsync<JsonElement>("api/orders/"+id,token);if(IsDisposed||generation!=previewGeneration)return;var order=data.GetProperty("order");var state=Ui.Text(order,"order_status");var shipment=Ui.Rows(data,"shipments").LastOrDefault();
            var lines=new List<string>{Ui.Text(order,"order_no"),StateLabel(state)};
            if(Api.Actor?.Role!="warehouse_staff")lines.Add(Ui.Money(Ui.Text(order,"total_vnd"))+" · "+ModuleView.ValueLabel("payment_method",Ui.Text(order,"payment_method")));
            if(Ui.Text(shipment,"status").Length>0)lines.Add("Giao nhận: "+StateLabel(Ui.Text(shipment,"status")));
            lines.AddRange(["",state=="pending"?"Tiếp theo: đối chiếu và nhận xử lý.":state=="confirmed"?"Tiếp theo: kiểm tra hàng để bàn giao.":state=="completed"?"Đơn đã hoàn tất. Mở hồ sơ khi cần đối chiếu.":"Mở hồ sơ để xem việc có thể thực hiện."]);preview.Text=string.Join(Environment.NewLine,lines);preview.ForeColor=Ui.Ink;
        }catch(OperationCanceledException){}catch(Exception ex){if(!IsDisposed&&generation==previewGeneration){preview.Text=Ui.Error(ex)+"\nChọn lại đơn để thử lại.";preview.ForeColor=Ui.Danger;}}finally{if(!IsDisposed&&generation==previewGeneration)((BusyLabel)preview).IsBusy=false;}
    }
    internal static string StateLabel(string state)=>state switch{"pending"=>"Chờ xử lý","confirmed"=>"Đã xác nhận","handed_off"=>"Đã bàn giao","in_transit"=>"Đang giao","delivered"=>"Đã giao","completed"=>"Hoàn tất","reconciliation"=>"Cần đối soát","cancelled"=>"Đã hủy","failed"=>"Thất bại","returned"=>"Đã thu hồi","success"=>"Thành công","expired"=>"Hết hạn",""=>"Chưa ghi nhận",_=>state};
    internal void SelectAndOpen(string orderId){foreach(DataGridViewRow item in grid.Rows)if(item.DataBoundItem is System.Data.DataRowView row&&Convert.ToString(row["id"])==orderId){grid.CurrentCell=item.Cells.Cast<DataGridViewCell>().First(c=>c.Visible);item.Selected=true;detail.PerformClick();return;}throw new InvalidOperationException("Order is absent from the native list or outside staff scope: "+orderId);}
    private Task Detail()
    {
        var id=SelectedId();if(id is null||ActiveDetail is not null)return Task.CompletedTask;
        previewCancellation?.Cancel();var form=new OrderDetailForm(Api,id){TopLevel=false,FormBorderStyle=FormBorderStyle.None,Dock=DockStyle.Fill};ActiveDetail=form;
        listHost.Visible=false;Controls.Add(form);form.BringToFront();form.FormClosed+=async(_,_)=>{ActiveDetail=null;Controls.Remove(form);form.Dispose();if(IsDisposed)return;listHost.Visible=true;Ui.Pulse(listHost);await RefreshAsync();grid.Focus();};form.Show();Ui.Pulse(form);return Task.CompletedTask;
    }
    internal override bool CanLeave()=>ActiveDetail?.ActiveCommand is null;
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==(Keys.Control|Keys.F)&&ActiveDetail is null){search.Focus();return true;}if(keyData==Keys.Escape&&ActiveDetail is not null){ActiveDetail.Close();return true;}return base.ProcessCmdKey(ref msg,keyData);}
    protected override void Dispose(bool disposing){if(disposing&&!disposingView){disposingView=true;++previewGeneration;previewCancellation?.Cancel();previewCancellation?.Dispose();previewCancellation=null;grid.DataSource=null;binding.Dispose();}base.Dispose(disposing);}
}

internal sealed class OrderDetailForm:Form
{
    private readonly ApiClient api;private readonly string orderId;private readonly FlowLayoutPanel actions=Ui.Bar();private readonly Label header=new BusyLabel{Text="Đang tải đơn…",AutoSize=true,Padding=new Padding(20,6,0,8),ForeColor=Ui.Ink};private readonly TabControl tabs=Ui.Tabs();private readonly CommandKeys keys;private readonly CancellationTokenSource cancel=new();private bool busy;
    private CancellationTokenSource? pendingRead;private long generation;private JsonElement currentOrder;private bool cleaned;
    internal NativeCommandForm? ActiveCommand{get;private set;}
    internal string? CurrentState{get;private set;}
    internal string StatusText=>header.Text;
    public OrderDetailForm(ApiClient api,string orderId){this.api=api;keys=api.PendingCommands;this.orderId=orderId;Text="Chi tiết / xử lý đơn";Size=new Size(1100,730);MinimumSize=new Size(680,430);AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterParent;actions.Controls.Add(Ui.Button("← Quay lại",(_,_)=>Close()));actions.Controls.Add(Ui.Button("Làm mới",async(_,_)=>await LoadAsync()));Controls.Add(Ui.Vertical(Ui.PageHeader("Chi tiết đơn hàng",""),header,actions,tabs));Shown+=async(_,_)=>await LoadAsync();Ui.Prepare(this);}
    protected override void Dispose(bool disposing){if(disposing&&!cleaned){cleaned=true;++generation;cancel.Cancel();pendingRead?.Cancel();pendingRead?.Dispose();pendingRead=null;cancel.Dispose();}base.Dispose(disposing);}
    private IWin32Window DialogOwner => TopLevel ? this : TopLevelControl as Form ?? this;
    private async Task LoadAsync()
    {
        pendingRead?.Cancel();pendingRead?.Dispose();pendingRead=CancellationTokenSource.CreateLinkedTokenSource(cancel.Token);var token=pendingRead.Token;var current=++generation;((BusyLabel)header).IsBusy=true;
        try
        {
            var data=await api.GetAsync<JsonElement>("api/orders/"+orderId,token);if(IsDisposed||current!=generation)return;var order=data.GetProperty("order");currentOrder=order.Clone();var state=Ui.Text(order,"orderStatus","order_status");var role=api.Actor?.Role;
            CurrentState=state;
            header.ForeColor=Ui.Ink;header.Text=$"{Ui.Text(order,"orderNo","order_no")} · {OrdersView.StateLabel(state)}"+(role=="warehouse_staff"?"":" · "+Ui.Money(Ui.Text(order,"totalVnd","total_vnd")));
            foreach(Control control in actions.Controls.Cast<Control>().ToArray())control.Dispose();actions.Controls.Clear();var back=Ui.Button("← Quay lại",(_,_)=>Close());actions.Controls.Add(back);CancelButton=back;actions.Controls.Add(Ui.Button("Làm mới",async(_,_)=>await LoadAsync()));
            if(role is "owner" or "order_staff")
            {
                if(state=="pending")Action("Xác nhận / nhận xử lý","confirm_order",new{orderId});
                if(state is "pending" or "confirmed")Action("Hủy trước bàn giao","cancel_order",new{orderId});
                var shipment=Ui.Rows(data,"shipments").LastOrDefault();var shipping=Ui.Text(shipment,"status");
                if(shipping is "handed_off" or "failed")Action("Đang giao demo","shipment_event",new{orderId,status="in_transit"});
                if(shipping=="in_transit")Action("Giao thành công demo","shipment_event",new{orderId,status="delivered"});
                if(shipping is "handed_off" or "in_transit")Action("Giao thất bại demo","shipment_event",new{orderId,status="failed"});
                if(shipping=="failed")Action("Hàng giao thất bại đã thu hồi","shipment_event",new{orderId,status="returned"});
                if(Ui.Text(order,"paymentMethod","payment_method") is "cod" or "banking"&&state is not("cancelled" or "completed")&&(Ui.Text(order,"paymentMethod","payment_method")!="cod"||!string.IsNullOrEmpty(Ui.Text(order,"delivered_at"))))actions.Controls.Add(Ui.Button("Đối soát khoản thu",async(sender,_)=>await PaymentAsync(data,(Control)sender!)));
            }
            if(role=="warehouse_staff"&&state=="confirmed")Action("Bàn giao hàng","handoff",new{orderId});
            if(role=="owner"&&state=="cancelled")Action("Hoàn tiền đơn đã hủy","execute_refund",new{orderId,outcome="success"});
            if(data.TryGetProperty("invoice",out var inv)&&inv.ValueKind==JsonValueKind.Object&&Guid.TryParse(Ui.Text(inv,"id"),out var invoiceId))actions.Controls.Add(Ui.Button("Mở hóa đơn",async(_,_)=>{using var f=new InvoiceDetailForm(api,invoiceId);await f.ShowDialogAsync(DialogOwner);}));
            BuildSections(data,order,role);
        }
        catch(OperationCanceledException){}catch(Exception ex){if(!IsDisposed&&current==generation){header.Text=Ui.Error(ex)+" Chọn Làm mới để thử lại.";header.ForeColor=Ui.Danger;}}finally{if(!IsDisposed&&current==generation)((BusyLabel)header).IsBusy=false;}
    }
    private void BuildSections(JsonElement data,JsonElement order,string? role)
    {
        var selected=tabs.SelectedTab?.Text;foreach(TabPage page in tabs.TabPages.Cast<TabPage>().ToArray())page.Dispose();tabs.TabPages.Clear();
        void Add(string title,params Control[] controls){var page=new TabPage(title);var content=Ui.Vertical(controls);content.Padding=new Padding(12);page.Controls.Add(content);tabs.TabPages.Add(page);}
        var units=Ui.Rows(data,"units");var allocations=Ui.Button($"Serial / phân bổ ({units.Count})",async(_,_)=>await OpenRecords("Serial / phân bổ từng món","units",units));allocations.Enabled=units.Count>0;
        var information=Ui.Button("Thông tin hồ sơ",async(_,_)=>{using var detail=new RecordDetailForm(order,"Thông tin đơn hàng");await detail.ShowDialogAsync(DialogOwner);});
        Add("Hàng hóa",Ui.Bar(allocations,information),OrderGrid("items",Ui.Rows(data,"items")));
        if(role!="warehouse_staff")
        {
            var receipts=Ui.Rows(data,"receipts");var attempts=Ui.Rows(data,"attempts");
            var amount=receipts.Sum(x=>decimal.TryParse(Ui.Text(x,"amount_vnd"),out var value)?value:0);
            var summary=Ui.Label(ModuleView.ValueLabel("payment_method",Ui.Text(order,"payment_method","paymentMethod"))+" · "+(receipts.Count==0?"Chưa ghi nhận khoản thu":$"{receipts.Count} khoản thu · {Ui.Money(amount.ToString(System.Globalization.CultureInfo.InvariantCulture))}"));
            var history=Ui.Button($"Lần thanh toán ({attempts.Count})",async(_,_)=>await OpenRecords("Lần thanh toán","attempts",attempts));history.Enabled=attempts.Count>0;
            var help=Ui.Button("Cách đối soát",(_,_)=>Ui.ShowInformation(DialogOwner,"Đối soát khoản thu","Khoản thu được ghi nhận riêng với trạng thái giao hàng. Với COD, chỉ đối soát sau khi đã giao; trạng thái bàn giao không xác nhận đã thu tiền."));
            Add("Thanh toán",summary,Ui.Bar(history,help),receipts.Count>0?OrderGrid("receipts",receipts):new Panel{BackColor=Ui.Paper});
        }
        var shipments=Ui.Rows(data,"shipments");var events=shipments.SelectMany(x=>x.TryGetProperty("events",out var value)&&value.ValueKind==JsonValueKind.Array?value.EnumerateArray().Select(e=>e.Clone()):Enumerable.Empty<JsonElement>()).ToList();
        var deliveryActions=Ui.Bar();
        var delivery=Ui.Label("Chưa có địa chỉ trong hồ sơ.");
        if(order.TryGetProperty("address_snapshot",out var address)&&address.ValueKind==JsonValueKind.Object)
        {
            delivery.Text=string.Join(" · ",new[]{Ui.Text(address,"recipient","name"),Ui.Text(address,"phone")}.Where(x=>x.Length>0))+Environment.NewLine+string.Join(", ",new[]{Ui.Text(address,"addressLine","address_line","address"),Ui.Text(address,"ward"),Ui.Text(address,"province","city")}.Where(x=>x.Length>0));
            deliveryActions.Controls.Add(Ui.Button("Địa chỉ đầy đủ",async(_,_)=>{using var detail=new RecordDetailForm(address,"Địa chỉ giao hàng");await detail.ShowDialogAsync(DialogOwner);}));
        }
        if(events.Count>0)deliveryActions.Controls.Add(Ui.Button($"Lịch sử giao nhận ({events.Count})",async(_,_)=>await OpenRecords("Lịch sử giao nhận","events",events)));
        Add("Giao nhận",delivery,deliveryActions,shipments.Count>0?OrderGrid("shipments",shipments):Ui.Label("Chưa có vận đơn."));
        if(selected is not null)foreach(TabPage page in tabs.TabPages)if(page.Text==selected){tabs.SelectedTab=page;break;}
    }
    private async Task OpenRecords(string title,string kind,List<JsonElement> records)
    {
        using var dialog=new Form{Text=title,Size=new Size(940,620),MinimumSize=new Size(620,420),StartPosition=FormStartPosition.CenterParent};var close=Ui.Button("Đóng",(_,_)=>dialog.Close());dialog.CancelButton=close;var grid=OrderGrid(kind,records);
        dialog.Controls.Add(Ui.Vertical(Ui.PageHeader(title,$"{records.Count} dòng · Enter mở chi tiết",close),grid));Ui.Prepare(dialog);await dialog.ShowDialogAsync(DialogOwner);
    }
    private DataGridView OrderGrid(string kind,List<JsonElement> records)
    {
        var grid=Ui.Grid();grid.AutoGenerateColumns=true;var table=Ui.Table(records);
        if(kind=="items")
        {
            foreach(var name in new[]{"product_name","sku","options"})if(!table.Columns.Contains(name))table.Columns.Add(name);
            for(var i=0;i<records.Count;i++)if(records[i].TryGetProperty("snapshot",out var snapshot)&&snapshot.ValueKind==JsonValueKind.Object){table.Rows[i]["product_name"]=Ui.Text(snapshot,"name");table.Rows[i]["sku"]=Ui.Text(snapshot,"sku");if(snapshot.TryGetProperty("options",out var options)&&options.ValueKind==JsonValueKind.Object)table.Rows[i]["options"]=string.Join(" · ",options.EnumerateObject().Select(p=>p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False?(p.Name=="demo"?"Dữ liệu mẫu":Ui.DisplayName(p.Name))+": "+(p.Value.GetBoolean()?"Có":"Không"):p.Value.ToString()));}
        }
        if(kind=="units"&&table.Columns.Contains("identifiers"))for(var i=0;i<records.Count;i++)if(records[i].TryGetProperty("identifiers",out var identifiers)&&identifiers.ValueKind==JsonValueKind.Array)table.Rows[i]["identifiers"]=string.Join(", ",identifiers.EnumerateArray().Select(x=>Ui.Text(x,"value")));
        grid.DataBindingComplete+=(_,_)=>
        {
            foreach(DataGridViewColumn column in grid.Columns)
            {
                column.HeaderText=column.Name switch{"product_name"=>"Sản phẩm","line_no"=>"Dòng","line_type"=>"Loại dòng","unit_ordinal"=>"Món số","identifiers"=>"Serial / IMEI","options"=>"Biến thể","unit_price_vnd"=>"Đơn giá VND","voucher_discount_vnd"=>"Giảm voucher VND","points_discount_vnd"=>"Giảm điểm VND","net_amount_vnd"=>"Thành tiền VND","cash_planned_vnd"=>"Tiền cần thu VND","credit_planned_vnd"=>"Credit dự kiến VND","requested_amount_vnd"=>"Số tiền đối soát VND","tracking_no"=>"Vận đơn",_=>Ui.DisplayName(column.Name)};
                column.MinimumWidth=column.Name=="product_name"?220:100;
            }
            var priority=kind=="items"?new[]{"product_name","sku","quantity","unit_price_vnd","net_amount_vnd"}:kind=="units"?new[]{"unit_ordinal","identifiers","net_amount_vnd","cash_planned_vnd","credit_planned_vnd"}:kind=="shipments"?new[]{"tracking_no","status","created_at"}:kind=="events"?new[]{"status","occurred_at","note","created_at"}:new[]{"method","status","requested_amount_vnd","amount_vnd","received_at","created_at"};
            var shown=priority.Where(name=>grid.Columns.Contains(name)&&(api.Actor?.Role!="warehouse_staff"||!name.EndsWith("_vnd"))).ToHashSet();RecordPresentation.ApplyColumns(grid,shown);
            var display=0;foreach(var name in priority)if(grid.Columns[name] is {} column)column.DisplayIndex=display++;
        };
        grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="status"){e.Value=OrdersView.StateLabel(Convert.ToString(e.Value)??"");e.FormattingApplied=true;}else if(e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="method"){e.Value=ModuleView.ValueLabel("method",Convert.ToString(e.Value)??"");e.FormattingApplied=true;}};
        async Task OpenRow(int index){if(index<0||index>=grid.Rows.Count||grid.Rows[index].DataBoundItem is not System.Data.DataRowView row)return;var sourceIndex=table.Rows.IndexOf(row.Row);if(sourceIndex<0||sourceIndex>=records.Count)return;using var detail=new RecordDetailForm(records[sourceIndex],"Thông tin dòng");await detail.ShowDialogAsync(grid.FindForm()??DialogOwner);}
        grid.CellDoubleClick+=async(_,e)=>await OpenRow(e.RowIndex);grid.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await OpenRow(grid.CurrentRow?.Index??-1);}};
        grid.DataSource=table;grid.Disposed+=(_,_)=>table.Dispose();
        return grid;
    }
    private void Action(string label,string command,object input){var button=Ui.Button(label,async(_,_)=>await SendAsync(label,command,input),actions.Controls.Count==2);button.Name=command;actions.Controls.Add(button);}
    private async Task SendAsync(string label,string command,object input)
    {
        if(busy)return;busy=true;actions.Enabled=false;
        try{var context=JsonSerializer.Deserialize<Dictionary<string,object?>>(JsonSerializer.Serialize(input))!;var spec=new ActionSpec(label,command,[api.Actor!.Role],_=>context,[]);using var form=new NativeCommandForm(api,spec,currentOrder,default);ActiveCommand=form;if(await form.ShowDialogAsync(DialogOwner)==DialogResult.OK&&!IsDisposed)await LoadAsync();}catch(Exception ex){if(!IsDisposed)Ui.ShowError(this,ex);}finally{ActiveCommand=null;busy=false;if(!IsDisposed)actions.Enabled=true;}
    }
    private async Task PaymentAsync(JsonElement data,Control sourceButton)
    {
        if(busy)return;busy=true;using var indicator=Ui.Busy(sourceButton,"Đang đối soát…");
        try
        {
            var pending=Ui.Rows(data,"attempts").FirstOrDefault(r=>Ui.Text(r,"status")=="pending");
            if(pending.ValueKind!=JsonValueKind.Object){await api.CommandAsync("start_payment",new{orderId},keys.For("start_payment",new{orderId}));keys.Succeeded("start_payment",new{orderId});if(IsDisposed)return;await LoadAsync();if(!IsDisposed)header.Text+="\nĐã tạo lần thanh toán. Chọn đối soát lại để xác nhận số tiền.";return;}
            var lookups=await api.GetAsync<JsonElement>("api/admin/lookups",cancel.Token);using var form=new NativeCommandForm(api,ModuleActions.For("payments")[0],pending,lookups);if(await form.ShowDialogAsync(DialogOwner)==DialogResult.OK&&!IsDisposed)await LoadAsync();
        }catch(Exception ex){if(!IsDisposed)Ui.ShowError(this,ex);}finally{busy=false;}
    }
}
