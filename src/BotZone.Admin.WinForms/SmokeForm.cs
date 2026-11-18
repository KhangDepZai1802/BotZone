using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

/// <summary>Runs the actual Windows message loop and native controls against a real API.</summary>
internal sealed class SmokeForm : Form
{
    private readonly string[] args;
    private readonly Label status=Ui.Label("Đang kiểm tra WinForms với API local…");
    private readonly CancellationTokenSource cancellation=new(TimeSpan.FromMinutes(4));
    private readonly List<object> checks=[];
    private readonly string output;
    public SmokeForm(string[] args)
    {
        this.args=args;Text="BotZone native smoke";AutoScaleMode=AutoScaleMode.Dpi;Size=new Size(620,210);StartPosition=FormStartPosition.CenterScreen;Controls.Add(Ui.Vertical(status));
        output=Path.GetFullPath(Option("--output")??Path.Combine("artifacts","winforms",DateTime.Now.ToString("yyyyMMdd-HHmmss")));Directory.CreateDirectory(output);
        Shown+=async(_,_)=>await RunAsync();FormClosed+=(_,_)=>{cancellation.Cancel();cancellation.Dispose();};
    }
    private string? Option(string name){var inline=args.FirstOrDefault(x=>x.StartsWith(name+"=",StringComparison.Ordinal));if(inline is not null)return inline[(name.Length+1)..];var i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
    private async Task RunAsync()
    {
        var passed=false;string? failure=null;var started=DateTimeOffset.UtcNow;
        var apiUrl=Option("--api")??Environment.GetEnvironmentVariable("BOTZONE_API_URL")??"http://localhost:5080";
        using var api=new ApiClient(apiUrl);MainForm? main=null;
        try
        {
            if(Option("--fulfill-order") is string orderId){await FulfillmentSmoke.RunAsync(apiUrl,orderId,output,checks,cancellation.Token);passed=true;return;}
            using(var login=new LoginForm()){login.Show(this);await PaintAsync(login);CaptureNative(login,"login");login.Close();}
            var actor=await api.LoginAsync(Option("--email")??Environment.GetEnvironmentVariable("BOTZONE_SMOKE_EMAIL")??"owner@botzone.local",Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026",cancellation.Token);
            if(actor.Role is not("owner" or "order_staff"))throw new InvalidOperationException("Smoke hóa đơn cần tài khoản owner hoặc order_staff đã seed.");
            checks.Add(new{test="cookie-session-csrf",pass=(await api.SessionAsync(cancellation.Token))?.Id==actor.Id,role=actor.Role});
            if(args.Contains("--redesign-only")){await RedesignSmoke.RunAsync(apiUrl,output,checks,cancellation.Token);await api.LogoutAsync(cancellation.Token);passed=true;return;}

            if(args.Contains("--inventory-only")){checks.AddRange(await InventorySmoke.RunAsync(api,cancellation.Token));await api.LogoutAsync(cancellation.Token);passed=true;return;}
            if(args.Contains("--roles-only")){await RoleSmoke.RunAsync(apiUrl,output,checks,cancellation.Token);await api.LogoutAsync(cancellation.Token);passed=true;return;}
            if(args.Contains("--crud-only")){await CrudSmoke.RunAsync(api,output,checks,cancellation.Token);await api.LogoutAsync(cancellation.Token);passed=true;return;}
            if(args.Contains("--customers-only")){await CustomerSmoke.RunAsync(api,output,checks,cancellation.Token);await api.LogoutAsync(cancellation.Token);passed=true;return;}
            var query=new InvoiceQuery{Page=1,PageSize=20};var list=await api.PostAsync<InvoiceListResult>("api/admin/invoices/search",query,cancellation.Token);
            checks.Add(new{test="typed-invoice-list",pass=true,rows=list.Items.Count,matched=list.MatchedCount,totalVnd=list.MatchedTotalVnd});
            main=new MainForm(api,actor,false);main.Show(this);await main.NavigateAsync("invoices");
            if(main.CurrentPage is not InvoicesView invoices||invoices.CurrentResult is null||invoices.HasLoadError)throw new InvalidOperationException("Native invoice binding chưa thành công: "+main.CurrentPage?.StatusMessage);
            foreach(var size in new[]{new Size(1366,768),new Size(1920,1080)})
            {
                main.Size=size;await PaintAsync(main);CaptureNative(main,$"invoices-{size.Width}x{size.Height}");
                checks.Add(new{test="native-layout",requestedWidth=size.Width,requestedHeight=size.Height,actualWidth=main.Width,actualHeight=main.Height,deviceDpi=main.DeviceDpi,scalePercent=main.DeviceDpi*100/96,pass=main.Width==size.Width&&main.Height==size.Height,note="Actual DeviceDpi; this does not emulate an untested monitor DPI."});
            }
            using(var filter=new FilterDialog(null,await api.GetAsync<JsonElement>("api/admin/lookups",cancellation.Token))){filter.Show(main);await PaintAsync(filter);CaptureNative(filter,"native-filter");filter.Close();}
            if(args.Contains("--layout-only"))
            {
                checks.Add(new{test="scope",pass=true,note="Login, typed list, native layout and filter only. Invoice detail, other modules and exports not tested in this run."});await api.LogoutAsync(cancellation.Token);passed=true;return;
            }
            if(list.MatchedCount==0)throw new InvalidOperationException("API chạy nhưng chưa có fixture hóa đơn; không thể chứng minh màn chi tiết/export.");
            using(var detail=new InvoiceDetailForm(api,list.Items[0].Id)){detail.Show(main);await detail.LoadAsync();if(detail.CurrentInvoice is null)throw new InvalidOperationException("Native detail chưa nhận dữ liệu.");await PaintAsync(detail);CaptureNative(detail,"invoice-detail");checks.Add(new{test="native-invoice-detail",pass=true,itemCount=detail.CurrentInvoice.Items.Count});detail.Close();}
            var moduleFailures=new List<string>();
            foreach(var module in new[]{"reports","operational_reports","orders","catalogue","customers","promotions"}.Concat(actor.Role=="owner"?new[]{"categories","inventory","suppliers","purchases","stock_adjustments","payments","returns","refunds","trade_ins","warranty","landing","media","staff","settings","audit"}:[]))
            {
                try{status.Text="Đang kiểm tra module "+module;await main.NavigateAsync(module);await PaintAsync(main);if(main.CurrentPage?.HasLoadError!=false)throw new InvalidOperationException(module+": "+main.CurrentPage?.StatusMessage);CaptureNative(main,module);checks.Add(new{test="native-module",module,pass=true,status=main.CurrentPage.StatusMessage});if(main.CurrentPage is OperationalReportsView operations)foreach(var kind in operations.ReportKinds){await operations.SelectReportAsync(kind);if(operations.HasLoadError)throw new InvalidOperationException(kind+": "+operations.StatusMessage);checks.Add(new{test="native-operational-report",kind,pass=true,status=operations.StatusMessage});}}
                catch(Exception ex){var message=Ui.Error(ex);moduleFailures.Add(message);checks.Add(new{test="native-module",module,pass=false,error=message});}
            }
            var export=await api.PostAsync<InvoiceExportResult>("api/invoices/exports",new ExportRequest{Query=query,Kind="list",Formats=["xlsx","pdf"]},cancellation.Token);
            if(export.Links.Count!=2)throw new InvalidOperationException("API chưa trả đủ XLSX/PDF cho cùng snapshot.");
            foreach(var(format,url)in export.Links)
            {
                var path=Path.Combine(output,"invoices."+format);await api.DownloadAsync(url,path,cancellation.Token);var bytes=await File.ReadAllBytesAsync(path,cancellation.Token);
                var signature=format=="pdf"?bytes.Length>4&&System.Text.Encoding.ASCII.GetString(bytes,0,4)=="%PDF":bytes.Length>2&&bytes[0]==0x50&&bytes[1]==0x4b;
                if(!signature)throw new InvalidOperationException("File xuất sai signature: "+format);
                checks.Add(new{test="same-snapshot-export",pass=true,format,bytes=bytes.Length,sha256=Convert.ToHexString(SHA256.HashData(bytes)),exportId=export.ExportId,export.RowCount,export.TotalVnd,export.ReadAt});
            }
            await api.LogoutAsync(cancellation.Token);var after=await api.SessionAsync(cancellation.Token);if(after is not null)throw new InvalidOperationException("Session vẫn tồn tại sau logout.");checks.Add(new{test="logout",pass=true});if(moduleFailures.Count>0)throw new InvalidOperationException(string.Join(" | ",moduleFailures));passed=true;
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(output,"runtime-error.txt"),ex.ToString());failure=Ui.Error(ex);checks.Add(new{test="runtime",pass=false,error=failure});}
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(output,"smoke.json"),JsonSerializer.Serialize(new{started,finished=DateTimeOffset.UtcNow,mode=Option("--fulfill-order") is not null?"fulfill-order":args.Contains("--customers-only")?"customers-only":args.Contains("--crud-only")?"crud-only":args.Contains("--redesign-only")?"redesign-only":args.Contains("--roles-only")?"roles-only":args.Contains("--inventory-only")?"inventory-only":args.Contains("--layout-only")?"layout-only":"full",passed,api=apiUrl,os=Environment.OSVersion.VersionString,processArchitecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),deviceDpi=DeviceDpi,failure,checks},new JsonSerializerOptions{WriteIndented=true}));
            main?.Dispose();Environment.ExitCode=passed?0:1;Close();
        }
    }
    private static async Task PaintAsync(Form form){form.PerformLayout();form.Refresh();await Task.Delay(150);}
    private void CaptureNative(Form form,string name){using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(output,name+".png"),ImageFormat.Png);}
}
