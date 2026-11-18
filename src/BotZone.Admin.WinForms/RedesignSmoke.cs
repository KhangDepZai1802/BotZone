using System.Drawing.Imaging;
using System.Text.Json;
using System.Runtime.InteropServices;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

/// <summary>Read-only redesign acceptance through the real STA Windows message loop.</summary>
internal static class RedesignSmoke
{
    internal static async Task RunAsync(string apiUrl,string output,List<object> checks,CancellationToken token)
    {
        using var observer=new ApiClient(apiUrl);
        await observer.LoginAsync("owner@botzone.local",Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026",token);
        var before=await observer.GetAsync<JsonElement>("api/admin/landing",token);
        await CheckLifecycleAsync(observer,checks,token);
        foreach(var(email,role,home)in new[]{("owner@botzone.local","owner","overview"),("nv01@botzone.local","order_staff","orders"),("warehouse@botzone.local","warehouse_staff","overview"),("content@botzone.local","content_staff","overview")})
        {
            using var api=new ApiClient(apiUrl);
            var actor=await api.LoginAsync(email,Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026",token);
            Require(actor.Role==role,"Seeded role mismatch.");
            using var form=new MainForm(api,actor,false);form.Show();await form.NavigateAsync(home);await PaintAsync(form,token);
            Require(form.CurrentPage?.HasLoaded==true&&form.CurrentPage.HasLoadError==false,role+" home failed: "+form.CurrentPage?.StatusMessage);
            var homePage=form.CurrentPage;
            Capture(form,output,role+"-home");
            checks.Add(new{test="redesign-role-home",role,home,pass=true,view=homePage!.GetType().Name,deviceDpi=form.DeviceDpi,scalePercent=form.DeviceDpi*100/96,actualWidth=form.Width,actualHeight=form.Height});
            if(role=="owner")
            {
                var shell=form.Controls.OfType<TableLayoutPanel>().Single();
                foreach(var size in new[]{new Size(1366,768),new Size(1920,1080)})
                {
                    form.Size=size;await PaintAsync(form,token);var widths=shell.GetColumnWidths();var requested=shell.ColumnStyles[0].Width;
                    Require(Math.Abs(widths[0]-requested)<=1,"Shell rail did not relayout after a window resize.");
                    checks.Add(new{test="native-shell-responsive-layout",pass=true,deviceDpi=form.DeviceDpi,clientWidth=form.ClientSize.Width,clientHeight=form.ClientSize.Height,configuredRailWidth=requested,columnWidths=widths});
                    Capture(form,output,$"owner-home-{size.Width}x{size.Height}");
                }
            }
            form.Size=new Size(1000,720);await PaintAsync(form,token);Capture(form,output,role+"-home-compact");
            var quick=Descendants(form).OfType<ComboBox>().FirstOrDefault(x=>x.AccessibleName?.Contains("Ctrl+K",StringComparison.Ordinal)==true);
            if(quick is not null)await File.WriteAllTextAsync(Path.Combine(output,role+"-layout.json"),JsonSerializer.Serialize(new{deviceDpi=form.DeviceDpi,windowWidth=form.Width,quickType=quick.GetType().Name,quickBounds=quick.Bounds.ToString(),quickMinimum=quick.MinimumSize.ToString(),textWidth=TextRenderer.MeasureText(quick.Text,quick.Font).Width,parentType=quick.Parent?.GetType().Name,parentBounds=quick.Parent?.Bounds.ToString()},new JsonSerializerOptions{WriteIndented=true}),token);
            foreach(var heading in Descendants(homePage!).OfType<PageHeading>())foreach(var command in Descendants(heading).OfType<Button>().Where(x=>x.Visible))Require(FullyVisible(command),"Compact "+role+" home clipped or covered its header action.");
            checks.Add(new{test="compact-role-home-actions-visible",role,pass=true});
            checks.Add(new{test="redesign-logical-compact-layout",role,pass=true,deviceDpi=form.DeviceDpi,logicalClientWidth=form.ClientSize.Width*96d/form.DeviceDpi,note="Window resize on the actual monitor; no DPI simulation or hardware 100% / 150% claim."});
            try{await CheckNavigationGroupsAsync(form,role,home,output,checks,token);}catch(Exception ex){await File.WriteAllTextAsync(Path.Combine(output,role+"-navigation-error.txt"),ex.ToString(),token);throw;}
            form.Size=new Size(1366,768);
            if(role=="content_staff")await CheckContentQueueAsync(form,output,checks,token);
            if(role is "owner" or "order_staff" or "warehouse_staff")
            {
                var first=form.NavigateAsync("orders");var last=form.NavigateAsync("overview");await Task.WhenAll(first,last);
                Require(form.CurrentPage is OverviewView,"A superseded navigation replaced the latest page.");
                await form.NavigateAsync(home);Require(ReferenceEquals(homePage,form.CurrentPage),"Returning to a module replaced its cached page.");
                checks.Add(new{test="rapid-navigation-latest-and-cache",role,pass=true});
            }
            if(role=="owner")
            {
                var a=form.NavigateAsync("invoices",new InvoiceQuery{QuickSearch="UI-NO-MATCH-OLD"});
                var b=form.NavigateAsync("invoices",new InvoiceQuery{QuickSearch="UI-NO-MATCH-LATEST"});await Task.WhenAll(a,b);
                Require(form.CurrentPage is InvoicesView invoices&&invoices.CurrentResult?.MatchedCount==0,"Latest invoice query did not bind.");
                Require(Descendants(form.CurrentPage!).OfType<TextBox>().Any(x=>x.Text=="UI-NO-MATCH-LATEST"),"A previous invoice query replaced the latest filter.");
                checks.Add(new{test="rapid-invoice-query-latest",pass=true});
            }
            if(role is "owner" or "content_staff")
            {
                await form.NavigateAsync("landing");await PaintAsync(form,token);
                Require(form.CurrentPage is LandingView&&form.CurrentPage.HasLoaded&&!form.CurrentPage.HasLoadError,"Landing did not load.");
                foreach(var name in new[]{"saveLanding","publishLanding"})Require(form.Controls.Find(name,true).Single().Enabled==(role=="content_staff"),role+" landing permission differs: "+name);
                var restoreAction=form.Controls.Find("restoreLanding",true).Single();var historyChoices=Descendants(form.CurrentPage!).OfType<ListBox>().Single(x=>x.AccessibleName=="Phiên bản đã xuất bản");
                Require(!restoreAction.Enabled,role+" restore is available before a history selection.");
                if(historyChoices.Items.Count>0){historyChoices.SelectedIndex=0;Require(restoreAction.Enabled==(role=="content_staff"),role+" restore permission differs after history selection.");historyChoices.SelectedIndex=-1;Require(!restoreAction.Enabled,role+" restore remained available without a selection.");}
                checks.Add(new{test="landing-restore-requires-authorized-history-selection",role,pass=true,historyChoices=historyChoices.Items.Count,persisted=false});
                var fields=Descendants(form.CurrentPage!).OfType<TextBox>().Where(x=>x.Parent is not UpDownBase).ToList();Require(fields.All(x=>x.ReadOnly==(role=="owner")),"Landing editability differs from its role.");
                var landingTabs=Descendants(form.CurrentPage!).OfType<TabControl>().Single();Require(landingTabs.TabPages.Cast<TabPage>().Select(x=>x.Text).SequenceEqual(new[]{"Nội dung","Hiển thị","Lịch sử"}),"Landing does not have the three primary task groups.");
                Require(!fields.Single(x=>x.AccessibleName=="English title").Visible&&!fields.Single(x=>x.AccessibleName=="Màu đại diện (#RRGGBB)").Visible,"Landing showed secondary language or technical options by default.");
                checks.Add(new{test="landing-three-primary-groups-default-content",role,pass=true});
                Capture(form,output,role+"-landing");
                if(role=="content_staff")
                {
                    var title=fields.Single(x=>x.AccessibleName=="Tiêu đề tiếng Việt");var original=title.Text;title.Text="";
                    ((Button)form.Controls.Find("saveLanding",true).Single()).PerformClick();
                    Require(form.CurrentPage!.HasLoadError&&title.Focused,"Invalid landing title did not retain field focus and show an error.");
                    Capture(form,output,"content-landing-inline-error");title.Text=original;
                    checks.Add(new{test="landing-inline-validation-input-only",pass=true,persisted=false});
                    var english=fields.Single(x=>x.AccessibleName=="English title");var englishOriginal=english.Text;english.Text="";((Button)form.Controls.Find("saveLanding",true).Single()).PerformClick();
                    Require(english.Focused&&english.Visible&&landingTabs.SelectedTab!.Text=="Nội dung","Missing English content did not reveal and focus its language.");Capture(form,output,"content-landing-english-error");english.Text=englishOriginal;
                    var colors=fields.Single(x=>x.AccessibleName=="Màu đại diện (#RRGGBB)");var colorsOriginal=colors.Text;colors.Text="invalid";((Button)form.Controls.Find("saveLanding",true).Single()).PerformClick();
                    Require(colors.Focused&&colors.Visible&&landingTabs.SelectedTab!.Text=="Hiển thị","Invalid hidden advanced field did not reveal and focus its group.");Capture(form,output,"content-landing-advanced-error");colors.Text=colorsOriginal;
                    Descendants(form.CurrentPage!).OfType<ComboBox>().Single(x=>x.AccessibleName=="Ngôn ngữ nội dung").SelectedIndex=0;Descendants(form.CurrentPage!).OfType<ComboBox>().Single(x=>x.AccessibleName=="Tùy chọn hiển thị").SelectedIndex=0;landingTabs.SelectedIndex=0;
                    checks.Add(new{test="landing-hidden-language-and-advanced-error-disclosure",pass=true,persisted=false});
                    form.Size=new Size(1000,720);await PaintAsync(form,token);Capture(form,output,"content-landing-compact");
                    foreach(var actionName in new[]{"saveLanding","publishLanding"})Require(FullyVisible(form.Controls.Find(actionName,true).Single()),"Compact landing clipped the "+actionName+" action.");
                    var activeEditor=Descendants(form.CurrentPage!).OfType<TabControl>().Single().SelectedTab!;
                    Require(activeEditor.ClientSize.Height>=150&&FullyVisible(title),"Compact landing left too little room to edit the first field.");
                    checks.Add(new{test="compact-landing-write-actions-visible",pass=true,editorHeight=activeEditor.ClientSize.Height,firstFieldVisible=true});form.Size=new Size(1366,768);
                    await CheckDirtyGuardAsync(form,title,original,output,checks,token);
                }
                checks.Add(new{test="landing-owner-readonly-content-editable",role,pass=true});
            }
            if(role!="owner")
            {
                var current=form.CurrentPage;
                try{await form.NavigateAsync("settings");throw new InvalidOperationException("Unauthorized module opened.");}
                catch(ApiException ex)when(ex.Status==403){Require(ReferenceEquals(current,form.CurrentPage),"Denied navigation discarded the current page.");}
                try{await api.GetAsync<JsonElement>("api/admin/data/settings",token);throw new InvalidOperationException("Unauthorized settings read succeeded.");}
                catch(ApiException ex)when(ex.Status==403){checks.Add(new{test="settings-permission-ui-and-api",role,pass=true,status=403});}
            }
            await OperationReadChecks.RunAsync(api,form,output,checks,token);
            await api.LogoutAsync(token);Require(await api.SessionAsync(token)is null,"Logout left an authenticated session.");
            checks.Add(new{test="redesign-role-logout",role,pass=true});
        }
        var after=await observer.GetAsync<JsonElement>("api/admin/landing",token);
        Require(JsonElement.DeepEquals(before,after),"Read-only redesign smoke changed landing draft or history.");
        checks.Add(new{test="landing-draft-and-history-unchanged",pass=true,revision=Ui.Text(after.GetProperty("draft"),"revision"),note="No business write command submitted; session login/logout only."});
        await observer.LogoutAsync(token);
    }

    private static async Task CheckLifecycleAsync(ApiClient api,List<object> checks,CancellationToken token)
    {
        using(var page=new ProbePage(api))
        {
            var old=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);var newer=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var oldRequest=page.LoadAsync(old.Task);var oldToken=page.LastToken;var newRequest=page.LoadAsync(newer.Task);
            Require(oldToken.IsCancellationRequested,"Starting a new load did not cancel the previous request.");newer.SetResult("latest");await newRequest;old.SetResult("old");await oldRequest;
            Require(page.Value=="latest"&&page.Applied==1,"An old request overwrote the latest data.");
            var late=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);var pending=page.LoadAsync(late.Task);var lateToken=page.LastToken;page.Deactivate();late.SetResult("deactivated");await pending;
            Require(lateToken.IsCancellationRequested&&page.Value=="latest"&&page.Applied==1,"A deactivated page applied a late result.");
        }
        var disposed=new ProbePage(api);var disposal=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);var disposingRequest=disposed.LoadAsync(disposal.Task);var disposedToken=disposed.LastToken;disposed.Dispose();disposal.SetResult("disposed");await disposingRequest;
        Require(disposedToken.IsCancellationRequested&&disposed.Applied==0,"A disposed page applied a late result.");
        checks.Add(new{test="page-latest-deactivate-dispose",pass=true,note="Controlled out-of-order task completion through the real UI synchronization context."});
        var reducedBefore=Motion.ReduceForTesting;var osAnimationBefore=ClientAnimationEnabled();
        try
        {
            Motion.ReduceForTesting=true;Require(!Motion.Enabled,"Reduced motion override did not disable interpolation.");
            using var form=new Form{Text="Native busy focus acceptance",Size=new Size(500,220),StartPosition=FormStartPosition.CenterScreen};Ui.Prepare(form);
            var clicks=0;var button=(BzButton)Ui.Button("Kiểm tra thao tác",(_,_)=>clicks++,true);form.Controls.Add(Ui.Vertical(button));form.Show();form.Activate();button.Select();await PaintAsync(form,token);
            Require(button.Focused,"Native test button did not receive focus.");var bounds=button.Bounds;
            using(var outer=Ui.Busy(button,"Đang xử lý…"))
            {
                using(var inner=Ui.Busy(button,"Đang đối chiếu…")){button.PerformClick();Require(clicks==0&&button.IsBusy&&button.Focused&&button.Enabled&&button.Bounds==bounds,"Busy state changed focus/bounds or accepted a repeated click.");}
                Require(button.IsBusy,"Nested busy state ended before its outer request.");button.PerformClick();Require(clicks==0,"Pending outer busy state accepted a click.");
            }
            button.PerformClick();Require(clicks==1&&!button.IsBusy&&button.Focused&&button.Bounds==bounds,"Completion did not restore the original native action state.");
            checks.Add(new{test="busy-reclick-focus-bounds-nesting-reduced-motion",pass=true,deviceDpi=form.DeviceDpi});
        }
        finally{Motion.ReduceForTesting=reducedBefore;}
        Require(ClientAnimationEnabled()==osAnimationBefore,"OS client area animation preference changed during the test.");
        checks.Add(new{test="reduced-motion-test-restored-no-os-setting-change",pass=true});
    }
    private static async Task CheckDirtyGuardAsync(MainForm form,TextBox title,string original,string output,List<object> checks,CancellationToken token)
    {
        var landing=form.CurrentPage;var edited=original+" · Nháp kiểm tra local";title.Text=edited;
        await ChooseAsync("Ở lại","content-landing-dirty-keep");Require(ReferenceEquals(form.CurrentPage,landing)&&title.Text==edited,"Staying in the dirty editor lost the local draft.");
        await ChooseAsync("Bỏ thay đổi","content-landing-dirty-discard");Require(form.CurrentPage is ContentHomeView,"Discard did not complete the requested navigation.");
        await form.NavigateAsync("landing");Require(ReferenceEquals(form.CurrentPage,landing)&&title.Text==original,"Discard or page caching failed to restore the saved title.");
        checks.Add(new{test="landing-dirty-keep-discard-and-cache",pass=true,persisted=false});
        async Task ChooseAsync(string label,string captureName)
        {
            var completion=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            form.BeginInvoke((Action)(async()=>{try{await form.NavigateAsync("overview");completion.TrySetResult();}catch(Exception ex){completion.TrySetException(ex);}}));
            Form? dialog=null;var deadline=DateTime.UtcNow.AddSeconds(15);
            try
            {
                while(dialog is null&&DateTime.UtcNow<deadline){dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>x.Owner==form&&x.Text=="Thay đổi chưa lưu"&&x.Visible&&x.IsHandleCreated);if(dialog is null)await Task.Delay(30,token);}
                Require(dialog is not null,"Dirty editor did not open its native confirmation.");await PaintAsync(dialog!,token);Capture(dialog!,output,captureName);
                Descendants(dialog!).OfType<Button>().Single(x=>x.Text==label).PerformClick();await completion.Task.WaitAsync(token);
            }
            finally{if(dialog is not null&&!dialog.IsDisposed)dialog.Dispose();}
        }
    }
    private static async Task CheckContentQueueAsync(MainForm form,string output,List<object> checks,CancellationToken token)
    {
        var home=form.CurrentPage as ContentHomeView??throw new InvalidOperationException("Content home missing.");var grid=Descendants(home).OfType<DataGridView>().Single();Require(grid.Columns.Count==3,"Content queue exposes extra detail columns by default.");Require(grid.Rows.Count>0,"Content fixture has no sections.");
        var index=Math.Min(1,grid.Rows.Count-1);grid.CurrentCell=grid.Rows[index].Cells["Name"];grid.Rows[index].Selected=true;var expected=home.SelectedContentId;
        ((Button)home.Controls.Find("contentSectionDetails",true).Single()).PerformClick();await Until(()=>home.ActiveSectionDetails?.Visible==true,"Selected section detail did not open.");var dialog=home.ActiveSectionDetails!;await PaintAsync(dialog,token);Capture(dialog,output,"content-section-detail");
        ((Button)dialog.Controls.Find("editContentSection",true).Single()).PerformClick();await Until(()=>form.CurrentPage is LandingView landing&&landing.SelectedSectionId==expected,"Editing a selected section opened another section.");Capture(form,output,"content-selected-section-editor");
        await form.NavigateAsync("overview");Require(ReferenceEquals(home,form.CurrentPage)&&home.SelectedContentId==expected,"Content queue lost its selected section when returning.");checks.Add(new{test="content-three-column-queue-detail-section-navigation",pass=true,sectionId=expected});
        async Task Until(Func<bool> condition,string error){var end=DateTime.UtcNow.AddSeconds(20);while(!condition()&&DateTime.UtcNow<end)await Task.Delay(30,token);Require(condition(),error);}
    }
    private static async Task CheckNavigationGroupsAsync(MainForm form,string role,string home,string output,List<object> checks,CancellationToken token)
    {
        var group=form.Controls.Find("navGroup_reports",true).OfType<Button>().SingleOrDefault();
        if(role=="content_staff"){Require(group is null,"Content role exposed the financial report navigation group.");checks.Add(new{test="content-navigation-report-group-hidden",pass=true});return;}
        Require(group is not null,"Allowed report navigation group missing.");group!.PerformClick();await Task.Delay(60,token);
        var menu=group.ContextMenuStrip;Require(menu?.Visible==true,"Compact report group did not open its native menu.");
        var expected=role=="warehouse_staff"?new[]{"Báo cáo nghiệp vụ"}:new[]{"Thống kê hóa đơn","Báo cáo nghiệp vụ"};Require(menu!.Items.Cast<ToolStripItem>().Select(x=>x.Text).SequenceEqual(expected),"Compact navigation menu does not match the role's allowed reports.");
        using(var bitmap=new Bitmap(menu.Width,menu.Height)){menu.DrawToBitmap(bitmap,new Rectangle(Point.Empty,menu.Size));bitmap.Save(Path.Combine(output,role+"-navigation-reports-menu.png"),ImageFormat.Png);}
        ((ToolStripMenuItem)menu.Items[0]).PerformClick();if(!menu.IsDisposed&&menu.Visible)menu.Close();
        bool Ready()=>form.CurrentPage?.HasLoaded==true&&!form.CurrentPage.HasLoadError&&(role=="warehouse_staff"?form.CurrentPage is OperationalReportsView:form.CurrentPage is ReportsView);
        var deadline=DateTime.UtcNow.AddSeconds(20);while(!Ready()&&DateTime.UtcNow<deadline)await Task.Delay(30,token);
        Require(Ready(),"Compact native report selection did not load the permitted report.");
        form.Size=new Size(1920,1080);await PaintAsync(form,token);var modules=Descendants(form).OfType<BzButton>().Where(x=>x.Navigation&&!x.Name.StartsWith("navGroup_",StringComparison.Ordinal)&&x.IconKey is "reports" or "operational_reports").ToArray();Require(modules.Length==expected.Length&&modules.All(x=>x.Visible),"Wide sidebar did not expand its selected report group.");
        group.PerformClick();Require(modules.All(x=>!x.Visible),"Collapsing a wide group left module controls visible.");group.PerformClick();Require(modules.All(x=>x.Visible),"Reopening a wide group did not restore module controls.");
        Capture(form,output,role+"-navigation-groups-expanded");checks.Add(new{test="native-compact-report-menu-role-and-wide-group-toggle",role,pass=true,menuItems=expected});
        await form.NavigateAsync(home);
    }
    private sealed class ProbePage(ApiClient api):PageView(api)
    {
        internal string Value="";internal int Applied;internal CancellationToken LastToken;
        internal Task LoadAsync(Task<string> result)=>LoadLatestAsync(token=>{LastToken=token;return result;},value=>{Value=value;Applied++;});
    }
    private static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException(message);}
    private static bool FullyVisible(Control control)
    {
        if(!control.Visible||control.Width<=0||control.Height<=0)return false;var original=control.RectangleToScreen(control.ClientRectangle);var visible=original;
        for(var parent=control.Parent;parent is not null;parent=parent.Parent)visible=Rectangle.Intersect(visible,parent.RectangleToScreen(parent.ClientRectangle));
        if(visible.Width<original.Width||visible.Height<original.Height)return false;
        var center=control.PointToScreen(new Point(control.Width/2,control.Height/2));Control hit=control.FindForm()??control.TopLevelControl!;
        for(var i=0;i<40;i++){var child=hit.GetChildAtPoint(hit.PointToClient(center),GetChildAtPointSkip.Invisible);if(child is null||child==hit)break;hit=child;}
        return ReferenceEquals(hit,control)||control.Contains(hit);
    }
    [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action,uint parameter,out int value,uint flags);
    private static bool ClientAnimationEnabled()=>SystemParametersInfo(0x1042,0,out var value,0)&&value!=0;
    private static async Task PaintAsync(Form form,CancellationToken token){form.PerformLayout();form.Refresh();await Task.Delay(170,token);}
    private static void Capture(Form form,string output,string name){using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(output,name+".png"),ImageFormat.Png);}
    private static IEnumerable<Control> Descendants(Control root){foreach(Control child in root.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
}
