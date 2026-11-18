using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class ModuleView : PageView
{
    private readonly string module;
    private readonly Actor actor;
    private readonly DataGridView grid = Ui.Grid();
    private readonly Panel gridHost = new(){Dock=DockStyle.Fill,BackColor=Ui.Surface};
    private readonly TableLayoutPanel emptyState = new(){Name="moduleEmptyState",Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Ui.Surface,Visible=false};
    private readonly Label emptyTitle=Ui.Label("Chưa có hồ sơ"),emptyGuidance=Ui.Label("");
    private readonly BindingSource binding = new();
    private readonly Button detailButton;
    private readonly Button previous, next;
    private readonly Dictionary<Button,ActionSpec> actionButtons=[];
    private readonly Label selectionInfo=Ui.Label("Chọn một hồ sơ để sửa hoặc duyệt."),workflow=Ui.Label("");
    private readonly List<ActionSpec> allowedActions;
    private readonly TextBox search = new() { Width = 260, PlaceholderText = "Tìm kiếm…" };
    private readonly Label pagination = Ui.Label("Trang 1");
    private readonly System.Windows.Forms.Timer debounce = new() { Interval = 350 };
    private List<JsonElement> rows = [];
    private int page = 1;
    private int total;
    private bool opening,disposingView;
    private CancellationTokenSource? actionRead;
    private HashSet<string>? visibleColumns;
    public int LoadedRowCount => rows.Count;
    internal CustomerDetailForm? ActiveCustomerDetail{get;private set;}

    public ModuleView(ApiClient api, Actor actor, string module) : base(api)
    {
        this.module = module; this.actor = actor; Dock = DockStyle.Fill; grid.AutoGenerateColumns = true; grid.DataSource = binding;
        grid.CellFormatting+=(_,e)=>
        {
            if(e.ColumnIndex<0||e.Value is null)return;var key=grid.Columns[e.ColumnIndex].Name;var raw=Convert.ToString(e.Value)??"";
            var text=RecordPresentation.CellText(key,raw);if(text!=raw){e.Value=text;e.FormattingApplied=true;}
            if(key is "status" or "state" or "business_status" or "validation_status")e.CellStyle.ForeColor=raw is "failed" or "rejected" or "cancelled"?Ui.Danger:raw is "draft" or "pending" or "requested"?Ui.Warning:Ui.Ink;
        };
        grid.CellToolTipTextNeeded+=(_,e)=>{if(e.RowIndex<0||e.ColumnIndex<0)return;var key=grid.Columns[e.ColumnIndex].Name;var raw=Convert.ToString(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value)??"";if(RecordPresentation.Technical(key)&&Guid.TryParse(raw,out var reference))e.ToolTipText=raw;};
        emptyTitle.Font=new Font("Segoe UI Semibold",14);emptyTitle.TextAlign=ContentAlignment.MiddleCenter;emptyTitle.Anchor=AnchorStyles.None;
        emptyGuidance.ForeColor=Ui.Muted;emptyGuidance.TextAlign=ContentAlignment.MiddleCenter;emptyGuidance.Anchor=AnchorStyles.None;
        var emptyMessage=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Anchor=AnchorStyles.None,BackColor=Ui.Surface};emptyMessage.Controls.AddRange([emptyTitle,emptyGuidance]);
        emptyState.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));emptyState.RowStyles.Add(new RowStyle(SizeType.Percent,50));emptyState.RowStyles.Add(new RowStyle(SizeType.AutoSize));emptyState.RowStyles.Add(new RowStyle(SizeType.Percent,50));emptyState.Controls.Add(emptyMessage,0,1);
        emptyState.SizeChanged+=(_,_)=>{emptyTitle.MaximumSize=emptyGuidance.MaximumSize=new Size(Math.Max(160,emptyState.ClientSize.Width-64),0);};
        gridHost.Controls.Add(grid);gridHost.Controls.Add(emptyState);
        allowedActions = ModuleActions.For(module).Where(x => x.Roles.Contains(actor.Role)).ToList();
        detailButton=Ui.Button("Mở chi tiết", async (_, _) => await DetailAsync());
        var createActions=Ui.Bar();var rowActions=Ui.Bar(detailButton);
        foreach(var action in allowedActions)
        {
            var button=Ui.Button(action.Label,async(sender,_)=>await ExecuteAsync(action,(Control)sender!),!ModuleActions.RequiresSelection(action));button.Name="action_"+action.Command;actionButtons.Add(button,action);
            (ModuleActions.RequiresSelection(action)?rowActions:createActions).Controls.Add(button);
        }
        selectionInfo.ForeColor=Ui.Muted;selectionInfo.Font=new Font("Segoe UI",9.5f);workflow.ForeColor=Ui.Muted;workflow.Font=new Font("Segoe UI",9.5f);
        var actions=new ActionShelf(selectionInfo,rowActions,workflow);
        var headerActions=new List<Control>{Ui.Button("Cột hiển thị",async(_,_)=>{using var picker=new OperationColumnsForm(grid);if(await picker.ShowDialogAsync(FindForm()??(IWin32Window)this)==DialogResult.OK){visibleColumns=picker.Selected;RecordPresentation.ApplyColumns(grid,visibleColumns);}}),Ui.Button("Làm mới",async(_,_)=>await RefreshAsync())};
        if (module == "media" && actor.Role == "content_staff") headerActions.Add(Ui.Button("Thêm ảnh / tài nguyên", async (sender, _) => await UploadAsync((Control)sender!),true));
        previous = Ui.Button("← Trước", async (_, _) => { if (page > 1) { page--; await RefreshAsync(); } }); next = Ui.Button("Tiếp →", async (_, _) => { if (page*20<total) { page++; await RefreshAsync(); } });
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6,Padding=new Padding(20),BackColor=Ui.Paper }; panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for(var i=0;i<6;i++)panel.RowStyles.Add(new RowStyle(i==4?SizeType.Percent:SizeType.AutoSize,i==4?100:0));
        panel.Controls.Add(Ui.PageHeader(Title(module),Description(module,actor.Role),headerActions.ToArray()),0,0);
        var filters=Ui.Bar(Ui.Field("Tìm kiếm",search),Ui.Button("Xóa lọc",(_,_)=>{search.Clear();search.Focus();}));foreach(Control button in createActions.Controls.Cast<Control>().ToArray()){button.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;filters.Controls.Add(button);}createActions.Dispose();panel.Controls.Add(filters,0,1);
        panel.Controls.Add(actions,0,2);panel.Controls.Add(Status,0,3);panel.Controls.Add(gridHost,0,4);panel.Controls.Add(Ui.Bar(previous,pagination,next),0,5);
        foreach(Control c in panel.Controls)c.Dock=DockStyle.Fill;Controls.Add(panel);
        void Adapt(){var scale=DeviceDpi/96f;var compact=ClientSize.Height/scale<600;var field=filters.Controls[0];field.MinimumSize=new Size((int)((compact?190:260)*scale),0);field.Width=field.MinimumSize.Width;field.Controls[0].Visible=!compact;workflow.Visible=!compact&&workflow.Text.Length>0;((PageHeading)panel.GetControlFromPosition(0,0)!).CompactDescription(compact);panel.Padding=new Padding((int)((compact?12:20)*scale));}
        SizeChanged+=(_,_)=>Adapt();workflow.TextChanged+=(_,_)=>Adapt();Adapt();
        search.TextChanged += (_, _) => { debounce.Stop(); debounce.Start(); }; debounce.Tick += async (_, _) => { debounce.Stop(); page = 1; await RefreshAsync(); };
        search.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;debounce.Stop();page=1;await RefreshAsync();}};
        grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await DetailAsync(); }; grid.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await DetailAsync(); } };
        grid.SelectionChanged+=(_,_)=>UpdateActions();UpdateActions();
        VisibleChanged+=(_,_)=>{if(!Visible){debounce.Stop();actionRead?.Cancel();}};
    }
    public override Task RefreshAsync()
    {
        emptyState.Visible=false;Status.Visible=true;
        var selectedId=Ui.Text(Selected(),"variant_id","id");var scroll=grid.FirstDisplayedScrollingRowIndex;var requestedPage=page;var sortColumn=grid.SortedColumn?.Name;var sortOrder=grid.SortOrder;
        return LoadLatestAsync(token => Api.GetAsync<JsonElement>($"api/admin/data/{Uri.EscapeDataString(module)}?q={Uri.EscapeDataString(search.Text.Trim())}&page={requestedPage}&pageSize=20", token), data =>
    {
        rows = Ui.Rows(data);var table=Ui.Table(rows);if(module=="settings"){table.Columns.Add("summary");for(var i=0;i<rows.Count;i++){var key=Ui.Text(rows[i],"id");var value=rows[i].GetProperty("value");table.Rows[i]["summary"]=key=="exchangeRateVnd"?Ui.Money(value.ToString())+" / USD":key=="commerce"?"Phí giao "+Ui.Money(Ui.Text(value,"shippingVnd"))+" · Trả hàng "+Ui.Text(value,"returnDays")+" ngày":"Mở hồ sơ để xem chính sách";}}binding.DataSource = table;total=int.TryParse(Ui.Text(data,"totalCount"),out var count)?count:rows.Count;
        foreach (DataGridViewColumn column in grid.Columns) { column.MinimumWidth = 110; column.HeaderText = column.Name=="id"?module=="settings"?"Thiết lập":module=="inventory"?"Lô hàng":"Mã hồ sơ":Labels.GetValueOrDefault(column.Name, RecordPresentation.Label(column.Name)); }
        RecordPresentation.ApplyColumns(grid,visibleColumns??RecordPresentation.Columns(module,grid));
        if(sortColumn is not null&&grid.Columns[sortColumn] is {} sorted&&sortOrder!=SortOrder.None)grid.Sort(sorted,sortOrder==SortOrder.Ascending?System.ComponentModel.ListSortDirection.Ascending:System.ComponentModel.ListSortDirection.Descending);
        foreach(DataGridViewRow item in grid.Rows)if(item.DataBoundItem is System.Data.DataRowView row&&row.Row.Table.Columns.Contains("id")&&Convert.ToString(row[row.Row.Table.Columns.Contains("variant_id")?"variant_id":"id"])==selectedId){grid.CurrentCell=item.Cells.Cast<DataGridViewCell>().FirstOrDefault(c=>c.Visible);item.Selected=true;break;}
        if(grid.CurrentCell is null&&grid.Rows.Count>0)grid.CurrentCell=grid.Rows[0].Cells.Cast<DataGridViewCell>().FirstOrDefault(c=>c.Visible);
        if(scroll>=0&&scroll<grid.Rows.Count)grid.FirstDisplayedScrollingRowIndex=scroll;
        pagination.Text = total==0?"0 kết quả":$"Trang {requestedPage} · {(requestedPage-1)*20+1}–{(requestedPage-1)*20+rows.Count} / {total} kết quả";
        previous.Enabled=requestedPage>1;next.Enabled=requestedPage*20<total;UpdateActions();
        Status.Text = rows.Count == 0 ? (search.TextLength==0?"Chưa có dữ liệu trong phạm vi được phép.":"Không có kết quả phù hợp. Sửa từ khóa hoặc chọn Xóa lọc.") : "";Status.Visible=Status.Text.Length>0;
        if(rows.Count==0){emptyTitle.Text=search.TextLength==0?"Chưa có hồ sơ":"Không tìm thấy kết quả";emptyGuidance.Text=search.TextLength>0?"Thử từ khóa khác hoặc chọn Xóa lọc.":module switch{"warranty"=>"Yêu cầu bảo hành sẽ xuất hiện tại đây.","stock_adjustments"=>"Đề nghị kiểm kê sẽ xuất hiện tại đây.","media"=>"Tài nguyên công khai sẽ xuất hiện tại đây.",_=>"Hồ sơ trong phạm vi được phép sẽ xuất hiện tại đây."};emptyState.Visible=true;emptyState.BringToFront();Status.Visible=false;}
    });
    }
    private void UpdateActions()
    {
        if(disposingView||IsDisposed||Disposing)return;var row=Selected();var selected=row.EnumerateObject().Any();detailButton.Enabled=selected&&!opening;
        foreach(var pair in actionButtons){var allowed=(!ModuleActions.RequiresSelection(pair.Value)||selected)&&ModuleActions.Available(pair.Value,row);pair.Key.Enabled=!opening&&allowed;pair.Key.AccessibleDescription=allowed?"Mở biểu mẫu "+pair.Value.Label:"Chọn hồ sơ ở bước phù hợp để thực hiện.";}
        var identity=Ui.Text(row,"sku","order_no","code","name","display_name","staff_code");if(identity.Length==0)identity=RecordPresentation.CellText("id",Ui.Text(row,"id"));
        selectionInfo.Text=selected?"Đang chọn: "+identity:"Chọn một hồ sơ trong danh sách để xem, sửa hoặc duyệt.";
        workflow.Text=WorkflowGuide.For(module,Ui.Text(row,"state","status","business_status"));workflow.Visible=workflow.Text.Length>0&&ClientSize.Height*96/Math.Max(96,DeviceDpi)>=600;
    }
    private JsonElement Selected()
    {
        var index=grid.CurrentRow?.Index??-1;
        if(!disposingView&&!IsDisposed&&!Disposing&&index>=0&&index<binding.Count&&binding[index] is System.Data.DataRowView view&&view.Row.Table.Columns.Contains("id"))
        {var key=view.Row.Table.Columns.Contains("variant_id")?"variant_id":"id";var id=Convert.ToString(view[key]);var row=rows.FirstOrDefault(x=>Ui.Text(x,key)==id);if(row.ValueKind==JsonValueKind.Object)return row;}
        return JsonSerializer.SerializeToElement(new{});
    }
    private async Task DetailAsync()
    {
        var selected = Selected();if(opening||!selected.EnumerateObject().Any())return;
        opening=true;UpdateActions();try{
        if(module=="customers"){using var customer=new CustomerDetailForm(Api,Ui.Text(selected,"id","user_id"));ActiveCustomerDetail=customer;try{await customer.ShowDialogAsync(FindForm() ?? (IWin32Window)this);}finally{ActiveCustomerDetail=null;}return;}
        using var detail = new RecordDetailForm(selected, Title(module)); await detail.ShowDialogAsync(FindForm() ?? (IWin32Window)this);
        }finally{opening=false;if(!IsDisposed)UpdateActions();}
    }
    internal void OpenFirstCustomer(){if(module!="customers"||grid.Rows.Count==0)throw new InvalidOperationException("Không có khách hàng trong danh sách native.");grid.CurrentCell=grid.Rows[0].Cells.Cast<DataGridViewCell>().First(c=>c.Visible);detailButton.PerformClick();}
    private async Task ExecuteAsync(ActionSpec action,Control source)
    {
        if(opening||ModuleActions.RequiresSelection(action)&&!Selected().EnumerateObject().Any()||!ModuleActions.Available(action,Selected()))return;using var busy=Ui.Busy(source,"Đang mở…");opening=true;UpdateActions();
        var actionRow=Selected();actionRead?.Dispose();actionRead=new();
        try { var needsLookups=action.Fields.Any(f=>f.Lookup is not null||f.Kind is FieldKind.PurchaseLines or FieldKind.ReceiptLines or FieldKind.QuoteLines);var lookups = needsLookups?await Api.GetAsync<JsonElement>("api/admin/lookups",actionRead.Token):JsonSerializer.SerializeToElement(new{});if(IsDisposed||!Visible)return;var row=actionRow;if(module=="settings"){var key=action.Fields.Any(x=>x.Key=="shippingVnd")?"commerce":"exchangeRateVnd";row=rows.FirstOrDefault(x=>Ui.Text(x,"id","key")==key);} using var form = new NativeCommandForm(Api, action, row, lookups); if (await form.ShowDialogAsync(FindForm() ?? (IWin32Window)this) == DialogResult.OK&&!IsDisposed) await RefreshAsync(); }catch(OperationCanceledException){} catch (Exception ex) { if(!IsDisposed){Status.Text=Ui.Error(ex);Status.ForeColor=Ui.Danger;} }finally{opening=false;if(!IsDisposed)UpdateActions();}
    }
    private async Task UploadAsync(Control sourceButton)
    {
        using var file = new OpenFileDialog { Filter = "Ảnh / mô hình 3D trên máy|*.jpg;*.jpeg;*.png;*.webp;*.glb", Multiselect = false, Title = "Chọn tài nguyên có nguồn / quyền sử dụng rõ ràng" };
        if (file.ShowDialog(FindForm()) != DialogResult.OK) return;
        using var metadata = new MediaMetadataForm(); if (await metadata.ShowDialogAsync(FindForm() ?? (IWin32Window)this) != DialogResult.OK) return;
        if(opening)return;opening=true;using var busy=Ui.Busy(sourceButton,"Đang tải lên…");
        try { Status.Text = "Đang tải và kiểm tra nội dung tệp…"; await Api.UploadAsync(file.FileName, "public", metadata.SourceUrl, metadata.LicenseNote);if(!IsDisposed) await RefreshAsync(); } catch (Exception ex) {if(!IsDisposed){Status.Text=Ui.Error(ex);Status.ForeColor=Ui.Danger;} }finally{opening=false;}
    }
    internal static string Title(string module)=>module switch{"categories"=>"Nhóm sản phẩm","catalogue"=>"Sản phẩm & phiên bản","inventory"=>"Tồn kho","suppliers"=>"Nhà cung cấp","purchases"=>"Nhập hàng","stock_adjustments"=>"Kiểm kê & điều chỉnh","payments"=>"Thanh toán & đối soát","returns"=>"Trả hàng","refunds"=>"Hoàn tiền","trade_ins"=>"Thu cũ & số dư","warranty"=>"BotZone Care","customers"=>"Khách hàng","promotions"=>"Ưu đãi","media"=>"Ảnh & tài nguyên","staff"=>"Nhân viên & quyền","settings"=>"Cài đặt cửa hàng","audit"=>"Nhật ký hoạt động",_=>module};
    internal static string ValueLabel(string key,string value)
    {
        var translated=AdminVocabulary.Value(key,value);if(translated!=value)return translated;
        if(key=="reason")return value switch{"defective"=>"Lỗi sản phẩm","wrong_item"=>"Giao sai sản phẩm",_=>value};
        if(key=="reception_method")return value switch{"store"=>"Tại cửa hàng","remote"=>"Gửi từ xa",_=>value};
        if(key=="damage_kind")return value switch{"hardware"=>"Lỗi phần cứng","accidental"=>"Rơi vỡ / va đập","liquid"=>"Vào nước",_=>value};
        if(key=="entity_type")return value switch{"product"=>"Sản phẩm","command"=>"Thao tác nghiệp vụ","return"=>"Trả hàng","trade_in"=>"Thu cũ","warranty"=>"Bảo hành","order"=>"Đơn hàng","inventory"=>"Tồn kho","media"=>"Tài nguyên",_=>value};
        if(key=="action")return value switch{"import_product_photo"=>"Nhập ảnh sản phẩm","import_product_specifications"=>"Nhập thông số sản phẩm","save_cart"=>"Cập nhật giỏ hàng","sync_wishlist"=>"Cập nhật yêu thích","save_landing"=>"Lưu nội dung trang chủ","publish_landing"=>"Xuất bản trang chủ","restore_landing"=>"Khôi phục trang chủ","preview_landing"=>"Xem trước trang chủ","revoke_landing_preview"=>"Thu hồi bản xem trước","upload_media"=>"Tải tài nguyên lên","update_translation"=>"Cập nhật bản dịch",_=>ModuleActions.CommandLabel(value)};
        return key is not ("role" or "active" or "status" or "state" or "business_status" or "validation_status" or "method" or "payment_method")?value:key=="role"?value switch{"owner"=>"Chủ cửa hàng","order_staff"=>"Xử lý đơn","warehouse_staff"=>"Kho","content_staff"=>"Nội dung",_=>value}:key=="active"?(value.Equals("true",StringComparison.OrdinalIgnoreCase)?"Đang hoạt động":"Ngừng hoạt động"):value switch{"pending"=>"Chờ xử lý","requested"=>"Đã đề nghị","approved"=>"Đã duyệt","rejected"=>"Từ chối","failed"=>"Thất bại","cancelled"=>"Đã hủy","completed"=>"Hoàn tất","draft"=>"Bản nháp","published"=>"Đã xuất bản","active"=>"Đang hoạt động","archived"=>"Đã lưu trữ","discontinued"=>"Ngừng kinh doanh","received"=>"Đã nhận","partially_received"=>"Đã nhận một phần","success"=>"Thành công","valid"=>"Hợp lệ","expired"=>"Hết hạn","credited"=>"Đã lập chứng từ giảm","closed"=>"Đã đóng","inspected"=>"Đã kiểm định","quoted"=>"Đã báo giá","accepted"=>"Đã chấp nhận","returned"=>"Đã trả lại","contact_pending"=>"Chờ liên hệ","exception"=>"Cần kiểm tra thêm","repairing"=>"Đang sửa chữa","repaired"=>"Đã sửa chữa","replace"=>"Chờ thay thế","replaced"=>"Đã thay thế","refund"=>"Đã duyệt hoàn tiền","refunded"=>"Đã hoàn tiền","cod"=>"Thu tiền khi giao","banking"=>"Chuyển khoản",_=>value};
    }
    private static string Description(string module,string role)=>module switch
    {
        "catalogue" when role=="content_staff"=>"Biên tập Việt / Anh. Giá bán, phiên bản và tồn kho chỉ xem.",
        "catalogue"=>"Mẫu là dòng sản phẩm; mỗi phiên bản bán có màu, dung lượng và giá riêng.",
        "inventory"=>"Tồn trong lô, đang giữ và chờ kiểm tra được theo dõi riêng. Điều chỉnh qua chứng từ.",
        "purchases"=>"Theo dõi phiếu đã duyệt, nhận đủ hoặc từng phần qua quy trình kho.",
        "stock_adjustments"=>role=="owner"?"Đối chiếu lý do và thiết bị trước khi duyệt chênh lệch.":"Theo dõi đề nghị kiểm kê. Chủ cửa hàng quyết định áp chênh lệch.",
        "payments"=>"Đối soát khoản thu độc lập với trạng thái giao hàng.",
        "refunds"=>"Kết quả hoàn thực tế tách biệt với chứng từ điều chỉnh giảm.",
        "returns"=>"Tiếp nhận, kiểm tra hàng và duyệt theo bước được cấp quyền.",
        "trade_ins"=>"Nhận máy, kiểm tra, báo giá và cấp số dư mua hàng cho khách.",
        "warranty"=>"Gia hạn bảo hành phần cứng. Xử lý theo điều kiện và thẩm quyền.",
        "customers"=>"Tra cứu thông tin khách, đơn hàng, điểm thưởng và số dư mua hàng.",
        "media"=>"Ảnh và mô hình 3D công khai, có nguồn và điều kiện sử dụng.",
        "staff"=>"Thay đổi quyền hoặc khóa tài khoản được máy chủ kiểm tra và ghi nhật ký.",
        "settings"=>"Chính sách áp dụng cho giao dịch mới. Chứng từ lịch sử giữ nguyên.",
        "audit"=>"Hoạt động trong phạm vi quyền của tài khoản đã đăng nhập.",
        _=>"Tìm, mở hồ sơ và thực hiện thao tác theo quyền hiện tại."
    };
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==(Keys.Control|Keys.F)){search.Focus();return true;}return base.ProcessCmdKey(ref msg,keyData);}
    internal static string FieldLabel(string key)=>Labels.GetValueOrDefault(key,Ui.DisplayName(key));
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase) { ["credit_vnd"]="Số dư mua hàng (₫)",["balance_points"]="Số dư điểm",["net_spend_vnd"]="Chi tiêu ròng VND",["tier"]="Hạng",["order_count"]="Số đơn",["id"]="Mã nội bộ",["name"]="Tên",["sku"]="Mã phiên bản",["priceVnd"]="Giá bán VND",["price_vnd"]="Giá bán VND",["available"]="Có thể bán",["status"]="Trạng thái",["state"]="Trạng thái",["createdAt"]="Ngày tạo",["created_at"]="Ngày tạo",["orderNo"]="Mã đơn",["order_no"]="Mã đơn",["customerName"]="Khách hàng",["totalVnd"]="Tổng VND",["total_vnd"]="Tổng VND",["role"]="Vai trò",["active"]="Hoạt động",["email"]="Email",["code"]="Mã",["quantity"]="Số lượng",["trackingNo"]="Mã vận đơn mô phỏng",["balanceVnd"]="Số dư VND",["balancePoints"]="Điểm",["reason"]="Lý do",["display_name"]="Tên khách hàng",["phone"]="Điện thoại",["email_verified"]="Email đã xác minh",["supplier"]="Nhà cung cấp",["contact"]="Liên hệ",["qty_on_hand"]="Số lượng trong lô",["qty_reserved"]="Đang giữ",["quantity_delta"]="Chênh lệch",["unit_cost_vnd"]="Giá vốn VND",["received_at"]="Ngày nhận",["model_code"]="Mã mẫu sản phẩm",["business_status"]="Kinh doanh",["care_fee_vnd"]="Phí Care VND",["standard_warranty_months"]="Bảo hành (tháng)",["staff_code"]="Mã nhân viên",["code_normalized"]="Mã ưu đãi",["requested_amount_vnd"]="Số tiền yêu cầu VND",["method"]="Phương thức",["amount_vnd"]="Số tiền VND",["action"]="Hành động",["entity_type"]="Loại hồ sơ",["occurred_at"]="Thời điểm",["value"]="Giá trị",["version"]="Phiên bản",["slug"]="Đường dẫn" };
    protected override void Dispose(bool disposing) { if (disposing&&!disposingView) {disposingView=true; actionRead?.Cancel();actionRead?.Dispose();actionRead=null;debounce.Dispose();grid.DataSource=null; binding.Dispose(); } base.Dispose(disposing); }
}

internal sealed class RecordDetailForm : Form
{
    public RecordDetailForm(JsonElement row, string title)
    {
        Text = title; Size = new Size(850, 650); MinimumSize = new Size(620, 430); StartPosition = FormStartPosition.CenterParent; AutoScaleMode = AutoScaleMode.Dpi;
        var tabs = Ui.Tabs();
        var properties=row.ValueKind==JsonValueKind.Object?row.EnumerateObject().ToList():[];
        var scalars=properties.Where(p=>p.Value.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object)).ToList();
        var principal=scalars.Where(p=>!RecordPresentation.Technical(p.Name)&&p.Value.ValueKind!=JsonValueKind.Null).OrderBy(p=>RecordPresentation.Priority(p.Name)).Take(6).ToList();
        void AddSection(string name,IEnumerable<JsonProperty> values)
        {
            var fields=new TableLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,ColumnCount=2,Padding=new Padding(20),BackColor=Color.White};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,32));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,68));
            foreach(var property in values)Add(fields,property.Name,property.Value.ToString());
            var page=new TabPage(name);page.Controls.Add(fields);tabs.TabPages.Add(page);
        }
        if(principal.Count>0)AddSection("Tóm tắt",principal);
        var secondary=scalars.Where(p=>principal.All(x=>x.Name!=p.Name)).ToList();if(secondary.Count>0)AddSection("Thông tin khác",secondary);
        var related=properties.Where(p=>p.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object).ToList();
        if(related.Count>0)
        {
            var links=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(20),BackColor=Color.White};
            foreach(var property in related)
            {
                var value=property.Value.Clone();var label=RecordPresentation.Label(property.Name);var count=value.ValueKind==JsonValueKind.Array?$" · {value.GetArrayLength()} dòng":"";
                links.Controls.Add(Ui.Button(label+count,async(_,_)=>{if(value.ValueKind==JsonValueKind.Object){using var detail=new RecordDetailForm(value,label);await detail.ShowDialogAsync(this);}else if(value.EnumerateArray().All(x=>x.ValueKind==JsonValueKind.Object)){using var list=new OperationRecordListForm(label,value.EnumerateArray().Select(x=>x.Clone()).ToList());await list.ShowDialogAsync(this);}else Ui.ShowInformation(this,label,string.Join(Environment.NewLine,value.EnumerateArray().Select(x=>x.ToString())));}));
            }
            var page=new TabPage("Liên quan");page.Controls.Add(links);tabs.TabPages.Add(page);
        }
        if(tabs.TabCount==0){var empty=new TabPage("Tóm tắt");empty.Controls.Add(Ui.Label("Chưa có thông tin."));tabs.TabPages.Add(empty);}
        var close = Ui.Button("Đóng", (_, _) => Close()); CancelButton = close; Controls.Add(Ui.Vertical(Ui.PageHeader(title,Ui.Text(row,"order_no","code","sku","name","display_name"),close), tabs));Ui.Prepare(this);
    }
    private static void Add(TableLayoutPanel panel, string key, string value) { var row = panel.RowCount++; panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));var caption=Ui.Label(RecordPresentation.Label(key));caption.ForeColor=Ui.Muted;panel.Controls.Add(caption, 0, row);var display=key.EndsWith("vnd",StringComparison.OrdinalIgnoreCase)?Ui.Money(value):key.EndsWith("_at")&&DateTimeOffset.TryParse(value,out var date)?Ui.VietDate(date):ModuleView.ValueLabel(key,value);var text=new TextBox { ReadOnly = true, Text = display,AccessibleName=RecordPresentation.Label(key), Multiline = display.Length > 90, Height = display.Length > 90 ? 70 : 34, Dock = DockStyle.Top, BackColor = Color.White,Margin=new Padding(0,0,0,12) };panel.Controls.Add(Ui.FrameInput(text), 1, row); }
}

internal static class RecordPresentation
{
    internal static bool Technical(string key)=>key=="id"||key.EndsWith("_id",StringComparison.OrdinalIgnoreCase)||key.EndsWith("Id",StringComparison.Ordinal)||key is "version" or "revision" or "snapshot" or "metadata";
    internal static int Priority(string key)=>Array.IndexOf(new[]{"order_no","code","name","display_name","sku","model_code","status","state","business_status","active","quantity","amount_vnd","total_vnd","net_amount_vnd","price_vnd","created_at"},key) is var index&&index>=0?index:100;
    internal static string Label(string key)=>key switch{"summary"=>"Nội dung chính","items"=>"Dòng hàng","units"=>"Thiết bị & phân bổ","snapshot"=>"Thông tin tại thời điểm giao dịch","address_snapshot"=>"Địa chỉ đã lưu","customer_snapshot"=>"Khách hàng đã lưu","options"=>"Thuộc tính phiên bản","identifiers"=>"Số máy / IMEI","events"=>"Lịch sử","attempts"=>"Lần thanh toán","receipts"=>"Khoản thu","shipments"=>"Giao nhận","max_uses"=>"Giới hạn lượt","max_uses_per_customer"=>"Lượt mỗi khách","version_no"=>"Phiên bản","requested_at"=>"Ngày đề nghị","description"=>"Mô tả","note"=>"Ghi chú","updated_at"=>"Cập nhật","approved_at"=>"Ngày duyệt","source_url"=>"Nguồn","license_note"=>"Quyền sử dụng","variant_id"=>"Mã phiên bản nội bộ","order_id"=>"Đơn liên quan","customer_id"=>"Mã khách hàng","reception_method"=>"Cách tiếp nhận","damage_kind"=>"Loại sự cố","inspection_result"=>"Kết quả kiểm tra","inspected_at"=>"Ngày kiểm tra","inspected_by"=>"Người kiểm tra","actor_user_id"=>"Người thực hiện","entity_id"=>"Hồ sơ liên quan","completed_at"=>"Ngày hoàn tất","fee_vnd"=>"Phí VND","lot_id"=>"Lô liên quan","approved_by"=>"Người duyệt","requested_by"=>"Người đề nghị",_=>ModuleView.FieldLabel(key)};
    internal static string CellText(string key,string value)
    {
        // Abbreviate references only for display; the bound row and detail retain the complete identifier.
        if(Technical(key)&&Guid.TryParse(value,out var id))return id.ToString("N")[..8].ToUpperInvariant()+"…";
        if(key=="actor_user_id"&&string.IsNullOrWhiteSpace(value))return "Không ghi nhận";
        return ModuleView.ValueLabel(key,value);
    }
    internal static HashSet<string> Columns(string module,DataGridView grid)
    {
        string[] preferred=module switch{
            "categories"=>["code","name_vi","active"],"catalogue"=>["name","sku","price_vnd","available","business_status"],"inventory"=>["id","sku","qty_on_hand","qty_reserved","quarantine","received_at"],"purchases"=>["code","supplier","status","created_at"],"suppliers"=>["code","name","contact","active"],"payments"=>["order_no","method","status","requested_amount_vnd","created_at"],"returns"=>["id","order_id","state","reason","created_at"],"refunds"=>["order_no","status","amount_vnd","completed_at"],"trade_ins"=>["id","customer_id","reception_method","state","created_at"],"warranty"=>["id","state","damage_kind","description","created_at"],"customers"=>["display_name","phone","email","tier","order_count"],"promotions"=>["code_normalized","active","max_uses","max_uses_per_customer"],"staff"=>["staff_code","name","role","active"],"settings"=>["id","summary","version"],"audit"=>["occurred_at","actor_user_id","action","entity_type","entity_id"],"stock_adjustments"=>["id","sku","quantity_delta","status","reason","created_at"],"media"=>["id","mime","validation_status","size_bytes","created_at"],"orders"=>["order_no","order_status","payment_method","total_vnd","created_at"],_=>[]};
        var available=grid.Columns.Cast<DataGridViewColumn>().Select(c=>c.Name).ToHashSet();var chosen=preferred.Where(available.Contains).ToHashSet();
        if(chosen.Count==0)chosen=available.Where(x=>!Technical(x)&&x is not ("events" or "options" or "value" or "identifiers")).OrderBy(Priority).Take(5).ToHashSet();
        if(chosen.Count==0&&grid.Columns.Count>0)chosen.Add(grid.Columns[0].Name);return chosen;
    }
    internal static void ApplyColumns(DataGridView grid,HashSet<string> visible)
    {
        var rowIndex=grid.CurrentRow?.Index??0;
        foreach(DataGridViewColumn column in grid.Columns)column.Visible=visible.Contains(column.Name);
        var displayIndex=0;foreach(var name in visible)if(grid.Columns[name] is {} column)column.DisplayIndex=displayIndex++;
        if(grid.CurrentCell is null&&grid.Rows.Count>0)grid.CurrentCell=grid.Rows[Math.Clamp(rowIndex,0,grid.Rows.Count-1)].Cells.Cast<DataGridViewCell>().FirstOrDefault(c=>c.Visible);
    }
}

internal sealed class OperationColumnsForm:Form
{
    private readonly CheckedListBox choices=new(){Dock=DockStyle.Fill,CheckOnClick=true,BorderStyle=BorderStyle.None,DisplayMember=nameof(Choice.Label),AccessibleName="Các cột trong bảng"};
    internal HashSet<string> Selected=>choices.CheckedItems.Cast<Choice>().Select(x=>x.Value).ToHashSet();
    internal OperationColumnsForm(DataGridView grid)
    {
        Text="Cột hiển thị";Size=new Size(480,540);StartPosition=FormStartPosition.CenterParent;
        foreach(DataGridViewColumn column in grid.Columns.Cast<DataGridViewColumn>().OrderBy(x=>x.DisplayIndex))choices.Items.Add(new Choice(column.Name,column.HeaderText),column.Visible);
        var hint=Ui.Label("Giữ ít nhất một cột trong bảng.");var apply=Ui.Button("Áp dụng",(_,_)=>{if(Selected.Count==0){hint.ForeColor=Ui.Danger;choices.Focus();return;}DialogResult=DialogResult.OK;Close();},true);var close=Ui.Button("Hủy",(_,_)=>Close());AcceptButton=apply;CancelButton=close;
        var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=3};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.Controls.Add(hint,0,0);panel.Controls.Add(choices,0,1);panel.Controls.Add(Ui.Bar(apply,close),0,2);Controls.Add(panel);Ui.Prepare(this);
    }
}

internal sealed class OperationRecordListForm:Form
{
    internal OperationRecordListForm(string title,List<JsonElement> records)
    {
        Text=title;Size=new Size(900,600);MinimumSize=new Size(600,420);StartPosition=FormStartPosition.CenterParent;var grid=Ui.Grid();grid.AutoGenerateColumns=true;var table=Ui.Table(records);grid.Disposed+=(_,_)=>table.Dispose();HashSet<string>? visibleColumns=null;
        grid.DataBindingComplete+=(_,_)=>{foreach(DataGridViewColumn column in grid.Columns)column.HeaderText=RecordPresentation.Label(column.Name);RecordPresentation.ApplyColumns(grid,visibleColumns??RecordPresentation.Columns("record",grid));};grid.DataSource=table;
        async Task Open(){if(grid.CurrentRow?.DataBoundItem is not System.Data.DataRowView view)return;var index=table.Rows.IndexOf(view.Row);if(index<0||index>=records.Count)return;using var detail=new RecordDetailForm(records[index],title);await detail.ShowDialogAsync(this);}
        grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await Open();};grid.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Open();}};
        var close=Ui.Button("Đóng",(_,_)=>Close());CancelButton=close;Controls.Add(Ui.Vertical(Ui.PageHeader(title,$"{records.Count} dòng",close),Ui.Bar(Ui.Button("Mở dòng đã chọn",async(_,_)=>await Open()),Ui.Button("Cột hiển thị",async(_,_)=>{using var picker=new OperationColumnsForm(grid);if(await picker.ShowDialogAsync(this)==DialogResult.OK){visibleColumns=picker.Selected;RecordPresentation.ApplyColumns(grid,visibleColumns);}})),grid));Ui.Prepare(this);
    }
}

internal sealed class MediaMetadataForm : Form
{
    private readonly TextBox source = new() { Dock = DockStyle.Top }, license = new() { Dock = DockStyle.Top, Multiline = true, Height = 90 };
    public string SourceUrl => source.Text.Trim(); public string LicenseNote => license.Text.Trim();
    public MediaMetadataForm()
    {
        Text="Nguồn và quyền sử dụng tài nguyên";Size=new Size(720,560);MinimumSize=new Size(560,440);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.Dpi;
        source.Name="mediaSource";license.Name="mediaLicense";license.AcceptsReturn=true;license.ScrollBars=ScrollBars.Vertical;
        var sourceError=Ui.Label(" ");var licenseError=Ui.Label(" ");foreach(var label in new[]{sourceError,licenseError}){label.ForeColor=Ui.Danger;label.Font=new Font("Segoe UI",9);label.Margin=new Padding(0,4,0,8);}
        TableLayoutPanel Field(string label,Control input,Label error){var group=(TableLayoutPanel)Ui.Field(label,input);group.Dock=DockStyle.Top;group.RowCount=3;group.Controls.Add(error,0,2);return group;}
        var fields=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,AutoScroll=true,Padding=new Padding(16),BackColor=Ui.Surface};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));fields.RowStyles.Add(new RowStyle(SizeType.Percent,100));fields.Controls.Add(Field("URL nguồn / tác giả *",source,sourceError),0,0);fields.Controls.Add(Field("Điều kiện sử dụng *",license,licenseError),0,1);
        var submit=Ui.Button("Tiếp tục tải lên",(_,_)=>{var missingSource=string.IsNullOrWhiteSpace(SourceUrl);var missingLicense=string.IsNullOrWhiteSpace(LicenseNote);sourceError.Text=missingSource?"Nhập nguồn hoặc tác giả của tài nguyên.":" ";licenseError.Text=missingLicense?"Nhập điều kiện hoặc giấy phép sử dụng.":" ";if(missingSource||missingLicense){(missingSource?source:license).Focus();return;}DialogResult=DialogResult.OK;Close();},true);var cancel=Ui.Button("Hủy",(_,_)=>Close());AcceptButton=submit;CancelButton=cancel;
        source.TextChanged+=(_,_)=>{if(SourceUrl.Length>0)sourceError.Text=" ";};license.TextChanged+=(_,_)=>{if(LicenseNote.Length>0)licenseError.Text=" ";};
        var footer=Ui.Bar(submit,cancel);footer.FlowDirection=FlowDirection.RightToLeft;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(20),BackColor=Ui.Paper};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.Controls.Add(Ui.PageHeader("Thông tin tài nguyên","Nguồn và quyền sử dụng"),0,0);layout.Controls.Add(fields,0,1);layout.Controls.Add(footer,0,2);Controls.Add(layout);Ui.Prepare(this);
    }
}
