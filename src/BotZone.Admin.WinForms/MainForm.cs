using BotZone.Contracts;
namespace BotZone.Admin.WinForms;

internal sealed class MainForm : Form
{
    private readonly ApiClient api;
    private Actor actor;
    private readonly TableLayoutPanel layout=new(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=2};
    private readonly FlowLayoutPanel navigation=new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(10),BackColor=Ui.NavSurface};
    private readonly Panel content=new(){Dock=DockStyle.Fill,BackColor=Ui.Paper,Padding=new Padding(2)};
    private readonly Label heading=Ui.Label("Không gian cửa hàng");
    private readonly ToolStripStatusLabel connection=new(){Spring=true,TextAlign=ContentAlignment.MiddleLeft};
    private readonly ToolStripStatusLabel identity=new();
    private readonly FlowLayoutPanel sessionBanner;
    private bool sessionExpired,recovering;
    internal bool SessionNeedsRecovery=>sessionExpired;
    private readonly Dictionary<string,PageView> pages=new();
    private readonly Dictionary<string,BzButton> buttons=new();
    private readonly Dictionary<string,BzButton> groups=new();
    private string openGroup="operations";
    private bool compactNavigation;
    private readonly ToolTip tips=new();
    private readonly ComboBox quick=new BzComboBox(){DropDownStyle=ComboBoxStyle.DropDownList,Width=300,AccessibleName="Đi đến chức năng · Ctrl+K"};
    private readonly BrandMark brandLabel=new();
    private readonly Label accountLabel=Ui.Label("");
    private readonly Panel sideHeader=new(){Dock=DockStyle.Fill,BackColor=Ui.NavSurface,Padding=new Padding(20,24,12,16)};
    private readonly FlowLayoutPanel sideFooter=new(){Dock=DockStyle.Fill,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Ui.NavSurface,Padding=new Padding(12)};
    private bool resizingNav;
    private readonly BusyLabel loading=new(){AutoSize=true,ForeColor=Ui.Muted,Padding=new Padding(20,0,0,0)};
    private bool closing,loggedOut,building;private readonly bool showNavigationErrors;private long navigationGeneration;private CancellationTokenSource? navigationCancellation;private string? currentKey;
    internal PageView? CurrentPage{get;private set;}
    public MainForm(ApiClient api,Actor actor,bool initializeOnShow=true)
    {
        this.api=api;this.actor=actor;showNavigationErrors=initializeOnShow;Text="BotZone — Quản trị cửa hàng";Ui.Prepare(this);Size=new Size(1366,768);MinimumSize=new Size(800,480);StartPosition=FormStartPosition.CenterScreen;KeyPreview=true;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,224));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var sidebar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Ui.NavSurface,Margin=Padding.Empty};sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,90));sidebar.RowStyles.Add(new RowStyle(SizeType.Percent,100));sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));brandLabel.ForeColor=Ui.Ink;brandLabel.Dock=DockStyle.Fill;sideHeader.Controls.Add(brandLabel);sidebar.Controls.Add(sideHeader,0,0);sidebar.Controls.Add(navigation,0,1);sidebar.Controls.Add(sideFooter,0,2);layout.Controls.Add(sidebar,0,0);
        var main=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Ui.Paper,Margin=Padding.Empty};main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));main.RowStyles.Add(new RowStyle(SizeType.AutoSize));main.RowStyles.Add(new RowStyle(SizeType.AutoSize));main.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var top=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=3,RowCount=1,BackColor=Ui.Surface,Padding=new Padding(24,12,24,10),Margin=Padding.Empty};top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));heading.ForeColor=Ui.Muted;heading.Anchor=AnchorStyles.Left;top.Controls.Add(heading,0,0);top.Controls.Add(loading,1,0);top.Controls.Add(Ui.FrameInput(quick),2,0);
        sessionBanner=Ui.Bar(Ui.Label("Phiên đã hết hạn. Bản nháp vẫn được giữ."),Ui.Button("Đăng nhập lại",async(_,_)=>await RecoverSessionAsync(),true));sessionBanner.Name="sessionRecoveryBanner";sessionBanner.Padding=new Padding(24,10,16,4);sessionBanner.BackColor=Ui.Soft;sessionBanner.Visible=false;
        main.Controls.Add(top,0,0);main.Controls.Add(sessionBanner,0,1);main.Controls.Add(content,0,2);layout.Controls.Add(main,1,0);var status=new StatusStrip{BackColor=Ui.Paper,SizingGrip=true};status.Items.Add(connection);status.Items.Add(identity);layout.Controls.Add(status,0,1);layout.SetColumnSpan(status,2);Controls.Add(layout);BuildNavigation();api.SessionExpired+=SessionExpired;
        navigation.SizeChanged+=(_,_)=>ResizeNavigation();Resize+=(_,_)=>ResizeNavigation();Shown+=(_,_)=>ResizeNavigation();quick.SelectionChangeCommitted+=async(_,_)=>{if(!building&&quick.SelectedItem is Choice c)await NavigateAsync(c.Value);};
        if(initializeOnShow)Shown+=async(_,_)=>await NavigateAsync(actor.Role=="order_staff"?"orders":"overview");
        KeyDown+=async(_,e)=>{if(e.Control&&e.KeyCode==Keys.K){e.SuppressKeyPress=true;quick.Focus();quick.DroppedDown=true;}else if(e.KeyCode==Keys.F5&&CurrentPage is not null&&CurrentPage.CanLeave()){e.Handled=true;await CurrentPage.RefreshAsync();}};
        FormClosing+=async(_,e)=>{if(loggedOut)return;e.Cancel=true;if(closing)return;if(pages.Values.Any(p=>p.HasPendingWrite)&&MessageBox.Show(this,"Một thao tác ghi đang chờ kết quả. Đóng ứng dụng không hủy giao dịch tại máy chủ. Hãy đối chiếu hồ sơ khi mở lại. Tiếp tục đóng?","Thao tác đang xử lý",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;if(pages.Values.Any(p=>!p.CanLeave()))return;closing=true;navigationCancellation?.Cancel();foreach(var page in pages.Values)page.Deactivate();connection.Text="Đang kết thúc phiên…";try{await api.LogoutAsync();}catch(Exception ex){connection.Text=Ui.Error(ex);}finally{loggedOut=true;Close();}};
    }
    private void BuildNavigation()
    {
        building=true;foreach(Control c in navigation.Controls.Cast<Control>().ToArray())c.Dispose();navigation.Controls.Clear();buttons.Clear();groups.Clear();quick.Items.Clear();
        foreach(Control c in sideFooter.Controls.Cast<Control>().ToArray())if(c!=accountLabel)c.Dispose();sideFooter.Controls.Clear();accountLabel.Text=actor.Name+"\n"+RoleLabel(actor.Role);accountLabel.ForeColor=Ui.NavMuted;accountLabel.Font=new Font("Segoe UI",9);sideFooter.Controls.Add(accountLabel);
        var allowed=Modules.Where(x=>x.Roles.Contains(actor.Role)).ToArray();
        foreach(var group in new[]{("home","Tổng quan","overview"),("operations","Đơn & thanh toán","orders"),("goods","Hàng hóa","catalogue"),("care","Khách & hậu mãi","customers"),("reports","Báo cáo","reports"),("content","Nội dung","landing"),("system","Cửa hàng","settings")})
        {
          var modules=allowed.Where(x=>GroupOf(x.Key)==group.Item1).ToArray();if(modules.Length==0)continue;
          if(group.Item1!="home"){
            var groupKey=group.Item1;var groupButton=(BzButton)Ui.Button(group.Item2,(_,_)=>OpenGroup(groupKey));groupButton.Name="navGroup_"+groupKey;groupButton.Navigation=true;groupButton.NavigationLevel=1;groupButton.IconKey=group.Item3;groupButton.AutoSize=false;groupButton.MinimumSize=new Size(0,32);groupButton.Height=34;groupButton.Font=new Font("Segoe UI Semibold",9.25f);groupButton.Margin=new Padding(0,14,0,4);groupButton.Tag=group.Item2;groupButton.AccessibleName=group.Item2;navigation.Controls.Add(groupButton);groups[groupKey]=groupButton;tips.SetToolTip(groupButton,group.Item2);
          }
          foreach(var module in modules){
            var title=module.Key=="overview"?actor.Role switch{"warehouse_staff"=>"Công việc kho","content_staff"=>"Việc nội dung",_=>"Tổng quan"}:module.Key=="landing"&&actor.Role=="owner"?"Nội dung · chỉ xem":module.Title;
            var b=(BzButton)Ui.Button(title,async(_,_)=>await NavigateAsync(module.Key));b.Name="navModule_"+module.Key;b.Navigation=true;b.NavigationLevel=module.Key=="overview"?0:2;b.IconKey=module.Key;b.MinimumSize=new Size(0,36);b.AutoSize=false;b.Height=40;b.TextAlign=ContentAlignment.MiddleLeft;b.Tag=title;b.AccessibleName=title;b.Margin=new Padding(0,0,0,3);b.Font=new Font("Segoe UI",10);navigation.Controls.Add(b);buttons[module.Key]=b;tips.SetToolTip(b,title);quick.Items.Add(new Choice(module.Key,title));
          }
        }
        var logout=(BzButton)Ui.Button("Đăng xuất",(_,_)=>Close());logout.Navigation=true;logout.IconKey="logout";logout.MinimumSize=new Size(0,36);logout.AutoSize=false;logout.Height=40;logout.Tag="Đăng xuất";logout.AccessibleName="Đăng xuất";sideFooter.Controls.Add(logout);tips.SetToolTip(logout,"Đăng xuất");identity.Text=actor.Name+" · "+RoleLabel(actor.Role);building=false;ResizeNavigation();
    }
    internal async Task NavigateAsync(string key,InvoiceQuery? query=null)
    {
        if(CurrentPage?.CanLeave()==false)return;
        var requested=Modules.FirstOrDefault(x=>x.Key==key);if(requested is null||!requested.Roles.Contains(actor.Role))throw new ApiException(403,"FORBIDDEN","Bạn không có quyền mở module này.");
        navigationCancellation?.Cancel();navigationCancellation?.Dispose();navigationCancellation=new();var token=navigationCancellation.Token;var generation=++navigationGeneration;SelectNav(key);loading.Text="Đang mở…";loading.IsBusy=true;
        try
        {
            var fresh=await api.SessionAsync(token);if(generation!=navigationGeneration||IsDisposed)return;
            if(fresh is null)throw new ApiException(401,"AUTH_REQUIRED","Phiên đã hết hạn. Chọn Đăng nhập lại để tiếp tục; bản nháp vẫn được giữ.");
            if(fresh.Role!=actor.Role){actor=fresh;foreach(var p in pages.Values)p.Dispose();pages.Clear();CurrentPage=null;BuildNavigation();}
            if(!requested.Roles.Contains(actor.Role))throw new ApiException(403,"FORBIDDEN","Quyền đã thay đổi. Bạn không được mở module này.");
            CurrentPage?.Deactivate();content.Controls.Clear();
            if(query is not null&&pages.Remove(key,out var previous))previous.Dispose();
            if(!pages.TryGetValue(key,out var page))
            {
                page=key switch{"invoices"=>new InvoicesView(api,query),"reports"=>new ReportsView(api,q=>{_=NavigateAsync("invoices",q);}),"operational_reports"=>new OperationalReportsView(api,actor),"orders"=>new OrdersView(api),"landing"=>new LandingView(api,actor),"overview" when actor.Role=="content_staff"=>new ContentHomeView(api,k=>{_=NavigateAsync(k);},async section=>{await NavigateAsync("landing");if(CurrentPage is LandingView landing)landing.SelectSection(section);}),"overview"=>new OverviewView(api,actor,k=>{_=NavigateAsync(k);}),_=>new ModuleView(api,actor,key)};pages[key]=page;
            }
            CurrentPage=page;currentKey=key;heading.Text=RoleLabel(actor.Role)+"  /  "+requested.Title;page.Dock=DockStyle.Fill;content.Controls.Add(page);SelectNav(key);Ui.Pulse(content);connection.Text="Đã kết nối";
            if(!page.HasLoaded||page.HasLoadError)await page.RefreshAsync();
        }
        catch(OperationCanceledException){}
        catch(Exception ex){if(generation!=navigationGeneration||IsDisposed)return;connection.Text=Ui.Error(ex);SelectNav(currentKey);if(!showNavigationErrors)throw;}
        finally{if(generation==navigationGeneration&&!IsDisposed){loading.IsBusy=false;loading.Text="";}}
    }
    private void SessionExpired(object? sender,EventArgs e)
    {
        if(IsDisposed||Disposing||!IsHandleCreated)return;
        var version=api.SessionVersion;
        try{BeginInvoke((Action)(()=>{if(IsDisposed||recovering||closing||version!=api.SessionVersion)return;sessionExpired=true;sessionBanner.Visible=true;connection.Text="Cần đăng nhập lại";}));}catch(InvalidOperationException){}
    }
    internal async Task RecoverSessionAsync()
    {
        if(recovering||closing||IsDisposed)return;recovering=true;
        navigationCancellation?.Cancel();++navigationGeneration;loading.IsBusy=false;loading.Text="";
        foreach(var page in pages.Values)page.Deactivate();
        try
        {
            using var dialog=new SessionRecoveryForm(api,actor);
            if(await dialog.ShowDialogAsync(this)!=DialogResult.OK||dialog.RestoredActor is not {} fresh||IsDisposed)return;
            var changed=fresh.Id!=actor.Id||fresh.Role!=actor.Role;actor=fresh;sessionExpired=false;sessionBanner.Visible=false;connection.Text="Đã kết nối · Nội dung đang nhập được giữ";
            if(changed)
            {
                foreach(var page in pages.Values)page.Dispose();pages.Clear();content.Controls.Clear();CurrentPage=null;currentKey=null;BuildNavigation();
                await NavigateAsync(actor.Role=="order_staff"?"orders":"overview");
                connection.Text="Quyền đã thay đổi · Giao diện đã được cập nhật";
            }
            else{accountLabel.Text=actor.Name+"\n"+RoleLabel(actor.Role);identity.Text=actor.Name+" · "+RoleLabel(actor.Role);}
        }
        finally{recovering=false;}
    }
    private void SelectNav(string? key){foreach(var pair in buttons)pair.Value.Selected=pair.Key==key;if(key is not null&&GroupOf(key)!="home")openGroup=GroupOf(key);building=true;for(var i=0;i<quick.Items.Count;i++)if(quick.Items[i] is Choice c&&c.Value==key)quick.SelectedIndex=i;building=false;UpdateGroupVisibility();}
    private static string GroupOf(string key)=>key switch{"overview"=>"home","orders" or "invoices" or "payments"=>"operations","categories" or "catalogue" or "inventory" or "suppliers" or "purchases" or "stock_adjustments"=>"goods","customers" or "promotions" or "returns" or "refunds" or "trade_ins" or "warranty"=>"care","reports" or "operational_reports"=>"reports","landing" or "media"=>"content",_=>"system"};
    private void OpenGroup(string key)
    {
        if(!compactNavigation){openGroup=openGroup==key?"":key;UpdateGroupVisibility();return;}
        var menu=new BzContextMenu{ShowCheckMargin=true};
        foreach(var module in Modules.Where(x=>GroupOf(x.Key)==key&&x.Roles.Contains(actor.Role))){var item=new ToolStripMenuItem(buttons[module.Key].Tag as string){Checked=currentKey==module.Key,Padding=new Padding(12,7,12,7)};item.Click+=async(_,_)=>await NavigateAsync(module.Key);menu.Items.Add(item);}
        var source=groups[key];source.ContextMenuStrip=menu;menu.Closed+=(_,_)=>{if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)(()=>{if(!source.IsDisposed&&ReferenceEquals(source.ContextMenuStrip,menu))source.ContextMenuStrip=null;menu.Dispose();}));else menu.Dispose();};menu.Show(source,new Point(source.Width,0));
    }
    private void UpdateGroupVisibility()
    {
        navigation.SuspendLayout();
        foreach(var pair in buttons)pair.Value.Visible=pair.Key=="overview"||(!compactNavigation&&GroupOf(pair.Key)==openGroup);
        foreach(var pair in groups)
        {
            var button=pair.Value;button.Text=button.Tag as string??"";button.Expanded=!compactNavigation&&openGroup==pair.Key;
            button.Selected=compactNavigation&&buttons.Any(module=>module.Value.Selected&&GroupOf(module.Key)==pair.Key);
            button.AccessibleDescription=compactNavigation?"Mở menu chức năng":button.Expanded?"Thu gọn nhóm chức năng":"Mở nhóm chức năng";
        }
        navigation.ResumeLayout();
    }
    private void ResizeNavigation()
    {
        if(resizingNav||layout.ColumnStyles.Count==0)return;resizingNav=true;
        try{var compact=ClientSize.Width*96/Math.Max(96,DeviceDpi)<1120;compactNavigation=compact;layout.ColumnStyles[0].Width=(compact?64:224)*DeviceDpi/96f;layout.PerformLayout();navigation.Padding=new Padding(compact?4:10,8,compact?4:10,8);sideHeader.Padding=new Padding(compact?10:20,20,compact?10:20,16);brandLabel.IconOnly=compact;brandLabel.Invalidate();accountLabel.Visible=!compact;sideFooter.Padding=new Padding(compact?4:12);
        foreach(var b in navigation.Controls.OfType<BzButton>().Concat(sideFooter.Controls.OfType<BzButton>())){b.Width=Math.Max(38,b.Parent!.ClientSize.Width-b.Parent.Padding.Horizontal-2);b.Text=b.Tag as string??b.Text;b.IconOnly=compact;b.TextAlign=ContentAlignment.MiddleLeft;b.Padding=new Padding(compact?2:10,4,compact?2:10,4);b.Invalidate();}UpdateGroupVisibility();}
        finally{resizingNav=false;}
    }
    private static string ShortName(string text)=>text switch{"Tổng quan"=>"TQ","Công việc kho"=>"CV","Việc nội dung"=>"ND","Đơn hàng"=>"ĐH","Hóa đơn"=>"HĐ","Đăng xuất"=> "↪",_=>string.Concat(text.Split(' ',StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x=>char.ToUpper(x[0])))};
    internal static string RoleLabel(string role)=>role switch{"owner"=>"Chủ cửa hàng","order_staff"=>"Xử lý đơn","warehouse_staff"=>"Nhân viên kho","content_staff"=>"Nhân viên nội dung",_=>role};
    protected override void Dispose(bool disposing){if(disposing){api.SessionExpired-=SessionExpired;navigationCancellation?.Cancel();navigationCancellation?.Dispose();navigationCancellation=null;foreach(var p in pages.Values)p.Dispose();pages.Clear();tips.Dispose();}base.Dispose(disposing);}
    private sealed record Module(string Key,string Title,string[] Roles);
    private static readonly Module[] Modules=[new("overview","Tổng quan",["owner","order_staff","warehouse_staff","content_staff"]),new("orders","Đơn hàng",["owner","order_staff","warehouse_staff"]),new("invoices","Hóa đơn",["owner","order_staff"]),new("reports","Thống kê hóa đơn",["owner","order_staff"]),new("operational_reports","Báo cáo nghiệp vụ",["owner","order_staff","warehouse_staff"]),new("categories","Nhóm sản phẩm",["owner"]),new("catalogue","Catalogue / SKU",["owner","order_staff","warehouse_staff","content_staff"]),new("inventory","Kho / serial",["owner","warehouse_staff"]),new("suppliers","Nhà cung cấp",["owner","warehouse_staff"]),new("purchases","Nhập hàng",["owner","warehouse_staff"]),new("stock_adjustments","Kiểm kê / điều chỉnh",["owner","warehouse_staff"]),new("payments","Thanh toán demo",["owner","order_staff"]),new("returns","Trả hàng",["owner","order_staff","warehouse_staff"]),new("refunds","Hoàn tiền",["owner","order_staff"]),new("trade_ins","Thu cũ / credit",["owner","order_staff","warehouse_staff"]),new("warranty","BotZone Care",["owner","order_staff","warehouse_staff"]),new("customers","Khách hàng / điểm",["owner","order_staff"]),new("promotions","Ưu đãi",["owner","order_staff","content_staff"]),new("landing","Landing CMS",["owner","content_staff"]),new("media","Thư viện media",["owner","content_staff"]),new("staff","Nhân viên / quyền",["owner"]),new("settings","Cấu hình",["owner"]),new("audit","Nhật ký",["owner","order_staff","warehouse_staff","content_staff"])];
}
