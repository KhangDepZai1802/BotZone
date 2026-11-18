using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BotZone.Admin.WinForms;
using BotZone.Contracts;

internal static class Program
{
    internal static string Output=Path.GetFullPath("artifacts/winforms/ux-round5/first-review");
    internal static bool Targeted;
    [DllImport("kernel32.dll")] private static extern uint SetErrorMode(uint mode);
    [STAThread] private static void Main(string[] args)
    {
        try
        {
            SetErrorMode(0x0002);
            if(args.Length>0)Output=Path.GetFullPath(args[0]);Targeted=args.Contains("--targeted",StringComparer.OrdinalIgnoreCase);Directory.CreateDirectory(Output);
            AppDomain.CurrentDomain.UnhandledException+=(_,e)=>Fatal("unhandled",e.ExceptionObject as Exception??new Exception(e.ExceptionObject.ToString()));
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(_,e)=>Fatal("ui-thread",e.Exception);
            TaskScheduler.UnobservedTaskException+=(_,e)=>{e.SetObserved();Fatal("unobserved-task",e.Exception);};
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.SetDefaultFont(new Font("Segoe UI",10.5f));
            using var harness=new Harness();Application.Run(harness);
        }
        catch(Exception ex){Fatal("initialization",ex);}
    }
    internal static void Fatal(string stage,Exception error)
    {
        try{Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"fatal.json"),JsonSerializer.Serialize(new{passed=false,stage,failure=error.ToString(),processId=Environment.ProcessId,at=DateTimeOffset.UtcNow},new JsonSerializerOptions{WriteIndented=true}));}catch{}
        Environment.Exit(3);
    }
}

internal sealed partial class Harness:Form
{
    private const BindingFlags Instance=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    private const BindingFlags Static=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    private readonly Assembly admin=typeof(ApiClient).Assembly;
    private readonly List<object> checks=[],captures=[];
    private readonly HttpAudit requests=new();
    private int failed;
    private string threadDesktop="",inputDesktop="",loadedHash="",publishedHash="";
    private readonly string publishedPath=Path.GetFullPath(Environment.GetEnvironmentVariable("BOTZONE_AUDIT_ASSEMBLY")??"artifacts/admin/win-x64/BotZone.Admin.WinForms.dll");
    private Color navSurface;
    [StructLayout(LayoutKind.Sequential)] private struct Rect{internal int Left,Top,Right,Bottom;internal Rectangle Bounds=>Rectangle.FromLTRB(Left,Top,Right,Bottom);}
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint{internal int X,Y;internal NativePoint(Point p){X=p.X;Y=p.Y;}}
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle,out Rect rectangle);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr SendMessage(IntPtr handle,uint message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr handle);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr handle,uint flags);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr handle,out uint processId);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr handle);
    [DllImport("user32.dll")] private static extern IntPtr GetThreadDesktop(uint threadId);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll",SetLastError=true)] private static extern IntPtr OpenInputDesktop(uint flags,bool inherit,uint access);
    [DllImport("user32.dll")] private static extern bool CloseDesktop(IntPtr desktop);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] private static extern bool GetUserObjectInformation(IntPtr handle,int index,StringBuilder name,int length,out int required);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr handle,int attribute,out Rect rectangle,int size);

    internal Harness()
    {
        Text="BotZone live shell audit bootstrap";ShowInTaskbar=false;FormBorderStyle=FormBorderStyle.None;Size=new Size(1,1);StartPosition=FormStartPosition.Manual;
        Shown+=async(_,_)=>{Hide();await RunAsync();};
    }
    private object Create(string name,params object?[] args)=>Activator.CreateInstance(admin.GetType("BotZone.Admin.WinForms."+name)!,Instance,null,args,null)!;
    private static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,Instance)!.GetValue(value)!;
    private static object? Property(object value,string name)=>value.GetType().GetProperty(name,Instance)!.GetValue(value);
    private static Task Call(object value,string name,params object?[] args)=>(Task)value.GetType().GetMethod(name,Instance)!.Invoke(value,args)!;
    private static IEnumerable<Control> Desc(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var item in Desc(child))yield return item;}}
    private static List<Button> Buttons(Control root)=>Desc(root).OfType<Button>().Where(button=>button.GetType().Name=="BzButton"&&button.Visible).ToList();
    private static IEnumerable<(string Key,Button Button)> Nav(object main,string name){var dictionary=Field<IDictionary>(main,name);return dictionary.Keys.Cast<string>().Select(key=>(key,(Button)dictionary[key]!));}
    private void Check(string name,bool pass,object? data=null){checks.Add(new{name,pass,data});if(!pass)failed++;}
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static string DesktopName(IntPtr handle){if(handle==IntPtr.Zero)return "unavailable";var name=new StringBuilder(256);return GetUserObjectInformation(handle,2,name,name.Capacity*2,out _)?name.ToString():"error:"+Marshal.GetLastWin32Error();}
    private async Task RunAsync()
    {
        Form? main=null;ApiClient? api=null;Exception? failure=null;
        try
        {
            threadDesktop=DesktopName(GetThreadDesktop(GetCurrentThreadId()));var desktop=OpenInputDesktop(0,false,1);try{inputDesktop=DesktopName(desktop);}finally{if(desktop!=IntPtr.Zero)CloseDesktop(desktop);}
            if(threadDesktop!=inputDesktop||inputDesktop=="unavailable")throw new InvalidOperationException($"Desktop mismatch: thread={threadDesktop}, input={inputDesktop}. Use the coordinated Default desktop launcher.");
            loadedHash=Hash(admin.Location);publishedHash=Hash(publishedPath);Check("exact-published-assembly",loadedHash==publishedHash,new{loaded=admin.Location,publishedPath,loadedHash,publishedHash});if(loadedHash!=publishedHash)throw new InvalidOperationException("Harness DLL copy differs from the published Release.");
            navSurface=(Color)admin.GetType("BotZone.Admin.WinForms.Ui")!.GetField("NavSurface",Static)!.GetValue(null)!;
            Check("sidebar-light-surface",navSurface.GetBrightness()>=.95f,new{color=Hex(navSurface)});
            api=new ApiClient(Environment.GetEnvironmentVariable("BOTZONE_API_URL")??"http://127.0.0.1:5080");var actor=await api.LoginAsync("owner@botzone.local",Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026");Check("real-owner-session",actor.Role=="owner");
            main=(Form)Create("MainForm",api,actor,false);main.Show();Fit(main,true);await Foreground(main);var modules=Nav(main,"buttons").Select(item=>item.Key).ToArray();
            var priorities=new[]{"overview","catalogue","settings","operational_reports","orders","reports","returns"};var ordered=(Program.Targeted?priorities:priorities.Concat(modules)).Distinct().ToArray();
            foreach(var key in ordered)
            {
                await Call(main,"NavigateAsync",key,null);await Settle();var page=Property(main,"CurrentPage");var error=page is not null&&(bool)Property(page,"HasLoadError")!;Check("module-loaded-"+key,page is not null&&!error,new{page=page?.GetType().Name,error});
                await Capture(main,"wide-"+key);Sidebar(main,key,false);
                if(key=="overview"&&!Program.Targeted)
                {
                    var group=Nav(main,"groups").Single(pair=>pair.Key=="goods").Button;group.PerformClick();await Settle();await Capture(main,"wide-overview-goods");Sidebar(main,"overview",false);
                }
                if(key=="invoices"&&!Program.Targeted)
                {
                    var previous=Field<Button>(page!,"previous");Check("invoice-first-page-disabled",!previous.Enabled);await Capture(main,"wide-invoices-disabled-pagination");
                    var result=(InvoiceListResult)Property(page!,"CurrentResult")!;
                    if(result.Items.Count>0){using var detail=(Form)Create("InvoiceDetailForm",api,result.Items[0].Id);detail.Show(main);await Call(detail,"LoadAsync");Fit(detail,false);await Foreground(detail);await Capture(detail,"invoice-detail-live");detail.Close();await Foreground(main);}
                }
                if(key=="landing"){var tabs=Field<TabControl>(page!,"tabs");tabs.SelectedTab=Field<TabPage>(page!,"displayTab");await Settle();await Capture(main,"landing-display-ampersand-live");}
            }
            await Call(main,"NavigateAsync","reports",null);var report=Property(main,"CurrentPage")!;var metric=Field<ComboBox>(report,"metric");metric.Focus();metric.DroppedDown=true;await Settle();await Capture(main,"reports-dropdown-live",true);metric.DroppedDown=false;
            if(!Program.Targeted)
            {
            var date=Field<DateTimePicker>(report,"from");date.Focus();await Settle();await Capture(main,"reports-date-focused-live");
            var originalDate=date.Value;var requestCount=requests.Entries.Length;
            try
            {
                Check("date-native-editor-focused",date.Focused,new{date.ContainsFocus,date.Enabled});
                SendMessage(date.Handle,0x0100,(IntPtr)Keys.Up,(IntPtr)1);SendMessage(date.Handle,0x0101,(IntPtr)Keys.Up,new IntPtr(unchecked((int)0xC0000001)));
                Check("date-native-up-key-edits-value",date.Value!=originalDate,new{originalDate,editedDate=date.Value,method="WM_KEYDOWN/WM_KEYUP to owned date HWND; no global input"});
                await Capture(main,"reports-date-edited-live");
            }
            finally{date.Value=originalDate;}
            Check("date-edit-restored-without-apply",date.Value==originalDate&&requests.Entries.Length==requestCount,new{date.Value,requestCount,afterRequestCount=requests.Entries.Length});
            metric.Focus();await Settle();await Capture(main,"reports-date-blurred-live");date.GetType().GetMethod("OpenCalendar",Instance)!.Invoke(date,null);await Settle();await Capture(main,"reports-calendar-live",true);if(Field<ToolStripDropDown?>(date,"calendar") is {} calendar)calendar.Close();
            await Call(main,"NavigateAsync","invoices",null);Fit(main,false);await Foreground(main);await Capture(main,"compact-invoices");Sidebar(main,"invoices",true);var operations=Nav(main,"groups").Single(pair=>pair.Key=="operations").Button;operations.PerformClick();await Settle();var menu=operations.ContextMenuStrip??throw new InvalidOperationException("Compact operations menu did not open.");Check("compact-invoice-menu-checked",menu.Items.OfType<ToolStripMenuItem>().Count(item=>item.Checked)==1&&menu.Items.OfType<ToolStripMenuItem>().Any(item=>item.Checked&&item.Text=="Hóa đơn"),new{menu.ShowCheckMargin,menu.ShowImageMargin});Check("compact-active-menu-tick-space",menu.ShowCheckMargin,new{menu.ShowCheckMargin,menu.ShowImageMargin});await Capture(main,"compact-operations-menu-invoice-active",true);menu.Close();
            foreach(var key in new[]{"overview","catalogue","settings","operational_reports","orders","returns","reports"}){await Call(main,"NavigateAsync",key,null);await Settle();await Capture(main,"compact-"+key);Sidebar(main,key,true);if(key=="settings"){var cards=Field<FlowLayoutPanel>(Property(main,"CurrentPage")!,"cards");cards.AutoScrollPosition=new Point(0,cards.DisplayRectangle.Height);await Capture(main,"compact-settings-lower");cards.AutoScrollPosition=Point.Empty;}}
            }
            else{var date=Field<DateTimePicker>(report,"from");date.Focus();await Capture(main,"reports-date-focused-live");}
            Fit(main,true);await Foreground(main);await Workflows(api,main);
            await api.LogoutAsync();Check("no-business-write-requests",requests.Entries.All(item=>item.IsAllowed),requests.Entries.Where(item=>!item.IsAllowed).ToArray());Check(Program.Targeted?"priority-modules-captured":"all-owner-modules-captured",ordered.Length==(Program.Targeted?priorities.Length:modules.Length),new{count=ordered.Length,modules=ordered});
        }
        catch(Exception ex){failure=ex;}
        finally
        {
            main?.Dispose();api?.Dispose();requests.Dispose();
            File.WriteAllText(Path.Combine(Program.Output,"result.json"),JsonSerializer.Serialize(new{passed=failure is null&&failed==0,failure=failure?.ToString(),failed,targeted=Program.Targeted,threadDesktop,inputDesktop,processId=Environment.ProcessId,assembly=admin.Location,loadedHash,publishedPath,publishedHash,captureMethod="Graphics.CopyFromScreen(SourceCopy); physical GetWindowRect; DwmFlush; guarded foreground and occlusion",checks,captures,requests=requests.Entries,noBusinessWrites=requests.Entries.All(item=>item.IsAllowed),noGlobalKeyboardOrMouseInput=true,noOtherProcessWindowChanged=true,dpiNote="Only actual monitor DPI is claimed; compact/wide resize is not DPI emulation.",finished=DateTimeOffset.UtcNow},new JsonSerializerOptions{WriteIndented=true}));
            Environment.ExitCode=failure is null&&failed==0?0:2;Close();
        }
    }
    private static void Fit(Form form,bool wide)
    {
        var area=Screen.FromControl(form).WorkingArea;form.WindowState=FormWindowState.Normal;form.StartPosition=FormStartPosition.Manual;form.Size=new Size(Math.Min(wide?1920:1000,area.Width),Math.Min(wide?1000:720,area.Height));form.Location=new Point(area.Left+(area.Width-form.Width)/2,area.Top+(area.Height-form.Height)/2);
    }
    private static async Task Settle(){await Task.Delay(450);DwmFlush();await Task.Delay(50);}
    private static async Task Foreground(Form form){form.Activate();if(GetForegroundWindow()!=form.Handle)SetForegroundWindow(form.Handle);await Settle();if(GetForegroundWindow()!=form.Handle)throw new InvalidOperationException("Owned test form is not foreground; capture refused.");}
    private void Sidebar(Form main,string active,bool compact)
    {
        var groups=Nav(main,"groups").ToArray();var modules=Nav(main,"buttons").ToArray();Check("sidebar-mode-"+(compact?"compact-":"wide-")+active,Field<bool>(main,"compactNavigation")==compact);
        if(compact){Check("compact-hides-child-modules-"+active,modules.Where(item=>item.Key!="overview").All(item=>!item.Button.Visible));return;}
        var children=modules.Where(item=>item.Button.Visible&&(int)Property(item.Button,"NavigationLevel")! ==2).Select(item=>item.Button).ToArray();
        Check("wide-groups-not-selected-"+active,groups.All(item=>!(bool)Property(item.Button,"Selected")!));Check("wide-active-module-only-"+active,modules.Count(item=>(bool)Property(item.Button,"Selected")!)==1&&modules.Single(item=>(bool)Property(item.Button,"Selected")!).Key==active);
        if(children.Length>0)Check("wide-group-smaller-than-child-"+active,groups.All(item=>item.Button.Font.SizeInPoints<children[0].Font.SizeInPoints&&item.Button.Height<children[0].Height));
    }
    private new async Task Capture(Form form,string name,bool allowOwnPopups=false)
    {
        await Settle();var flush=DwmFlush();var bounds=Guard(form,allowOwnPopups);var client=form.RectangleToScreen(form.ClientRectangle);var samples=new List<Point>();for(var y=client.Top+4;y<client.Bottom;y+=48)for(var x=client.Left+4;x<client.Right;x+=48)samples.Add(new Point(x,y));
        var buttons=Buttons(form).Select(button=>(Button:button,Bounds:button.RectangleToScreen(button.ClientRectangle))).Where(item=>VisibleBounds(item.Button)==item.Bounds).ToArray();
        foreach(var item in buttons){samples.AddRange(new[]{new Point(item.Bounds.Left+2,item.Bounds.Top+2),new Point(item.Bounds.Right-3,item.Bounds.Top+2),new Point(item.Bounds.Left+2,item.Bounds.Bottom-3),new Point(item.Bounds.Right-3,item.Bounds.Bottom-3),new Point(item.Bounds.Left+item.Bounds.Width/2,item.Bounds.Top+item.Bounds.Height/2)});}
        foreach(var point in samples){var root=GetAncestor(WindowFromPoint(new NativePoint(point)),2);GetWindowThreadProcessId(root,out var pid);if(root!=form.Handle&&(!allowOwnPopups||pid!=Environment.ProcessId))throw new InvalidOperationException($"{name}: covered at {point}; root HWND {root}, PID {pid}. No accepted image written.");}
        using var bitmap=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format32bppArgb);using(var graphics=Graphics.FromImage(bitmap))graphics.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size,CopyPixelOperation.SourceCopy);
        if(Guard(form,allowOwnPopups)!=bounds)throw new InvalidOperationException(name+": window moved during capture.");bitmap.Save(Path.Combine(Program.Output,name+".png"),ImageFormat.Png);
        var tested=0;var bad=new List<object>();
        foreach(var item in buttons)
        {
            var points=new[]{new Point(item.Bounds.Left+1,item.Bounds.Top+1),new Point(item.Bounds.Right-2,item.Bounds.Top+1),new Point(item.Bounds.Left+1,item.Bounds.Bottom-2),new Point(item.Bounds.Right-2,item.Bounds.Bottom-2)};
            if(points.Any(point=>GetAncestor(WindowFromPoint(new NativePoint(point)),2)!=form.Handle))continue;var expected=ParentColor(item.Button);var actual=points.Select(point=>bitmap.GetPixel(point.X-bounds.Left,point.Y-bounds.Top)).ToArray();tested++;
            if(actual.Any(color=>Difference(color,expected)>4))bad.Add(new{item.Button.Name,item.Button.Text,expected=Hex(expected),actual=actual.Select(Hex).ToArray()});
        }
        Check("button-corners-"+name,bad.Count==0,new{tested,bad});
        if(form.GetType().Name=="MainForm"&&!Field<bool>(form,"compactNavigation")&&!allowOwnPopups)
        {
            var children=Nav(form,"buttons").Where(item=>item.Button.Visible&&(int)Property(item.Button,"NavigationLevel")! ==2&&VisibleBounds(item.Button)==item.Button.RectangleToScreen(item.Button.ClientRectangle)).ToArray();
            var insetChecks=children.Select(item=>{var rectangle=item.Button.RectangleToScreen(item.Button.ClientRectangle);var point=new Point(rectangle.Left+Math.Max(3,12*item.Button.DeviceDpi/96),rectangle.Top+rectangle.Height/2);var actual=bitmap.GetPixel(point.X-bounds.Left,point.Y-bounds.Top);return new{item.Key,pass=Difference(actual,navSurface)<=4,actual=Hex(actual),expected=Hex(navSurface)};}).ToArray();Check("child-inset-pixels-"+name,insetChecks.All(item=>item.pass),insetChecks);
        }
        GetWindowRect(form.Handle,out var raw);captures.Add(new{name,at=DateTimeOffset.UtcNow,hwnd=form.Handle.ToInt64(),bounds,rawWindowBounds=raw.Bounds,captureBoundsRule="DWM visible frame when available; otherwise GetWindowRect intersected with physical monitor (invisible frame/shadow excluded)",deviceDpi=form.DeviceDpi,windowDpi=GetDpiForWindow(form.Handle),foreground=GetForegroundWindow().ToInt64(),dwmFlushHResult=flush,occlusionSamples=samples.Count,allowOwnPopups,buttons=buttons.Select(item=>new{item.Button.Name,item.Button.Text,item.Button.Enabled,item.Bounds,level=Property(item.Button,"NavigationLevel"),selected=Property(item.Button,"Selected")}).ToArray()});
    }
    private static Rectangle Guard(Form form,bool allowOwnPopups)
    {
        if(!form.Visible||!IsWindowVisible(form.Handle)||IsIconic(form.Handle))throw new InvalidOperationException("Capture form is not visibly restored.");var foreground=GetForegroundWindow();GetWindowThreadProcessId(foreground,out var pid);if(foreground!=form.Handle&&(!allowOwnPopups||pid!=Environment.ProcessId))throw new InvalidOperationException("Capture form is not foreground.");if(!GetWindowRect(form.Handle,out var window))throw new InvalidOperationException("Cannot read physical HWND bounds.");var monitor=Screen.FromHandle(form.Handle).Bounds;var bounds=DwmGetWindowAttribute(form.Handle,9,out var visible,Marshal.SizeOf<Rect>())==0?visible.Bounds:Rectangle.Intersect(window.Bounds,monitor);if(bounds.Width<=0||bounds.Height<=0||!monitor.Contains(bounds)||!monitor.Contains(form.RectangleToScreen(form.ClientRectangle)))throw new InvalidOperationException("The visible frame/client is not fully inside a physical monitor.");return bounds;
    }
    private static Rectangle VisibleBounds(Control control){var rectangle=control.RectangleToScreen(control.ClientRectangle);for(var parent=control.Parent;parent is not null;parent=parent.Parent)rectangle=Rectangle.Intersect(rectangle,parent.RectangleToScreen(parent.ClientRectangle));return rectangle;}
    private static Color ParentColor(Control control){for(var parent=control.Parent;parent is not null;parent=parent.Parent)if(parent.BackColor.A==255)return parent.BackColor;return SystemColors.Control;}
    private static int Difference(Color a,Color b)=>Math.Max(Math.Abs(a.R-b.R),Math.Max(Math.Abs(a.G-b.G),Math.Abs(a.B-b.B)));
    private static string Hex(Color color)=>$"#{color.R:X2}{color.G:X2}{color.B:X2}";
}

internal sealed class HttpAudit:IObserver<DiagnosticListener>,IObserver<KeyValuePair<string,object?>>,IDisposable
{
    internal sealed record Entry(string Method,string Path){internal bool IsAllowed=>Method is "GET" or "HEAD"||Method=="POST"&&Path is "/api/auth/login" or "/api/auth/logout" or "/api/admin/invoices/search" or "/api/admin/reports/invoices";}
    private readonly ConcurrentQueue<Entry> entries=new();private readonly List<IDisposable> subscriptions=[];private readonly IDisposable all;
    internal HttpAudit(){all=DiagnosticListener.AllListeners.Subscribe(this);}
    internal Entry[] Entries=>entries.ToArray();
    public void OnNext(DiagnosticListener listener){if(listener.Name=="HttpHandlerDiagnosticListener")subscriptions.Add(listener.Subscribe(this));}
    public void OnNext(KeyValuePair<string,object?> value){if(value.Key.EndsWith(".Start",StringComparison.Ordinal)&&value.Value?.GetType().GetProperty("Request")?.GetValue(value.Value) is HttpRequestMessage request&&request.RequestUri is {} uri)entries.Enqueue(new(request.Method.Method,uri.AbsolutePath));}
    public void OnError(Exception error){}public void OnCompleted(){}
    public void Dispose(){all.Dispose();foreach(var subscription in subscriptions)subscription.Dispose();}
}
