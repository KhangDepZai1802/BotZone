using System.Drawing.Imaging;
using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal static class RoleSmoke
{
    internal static async Task RunAsync(string apiUrl,string output,List<object> checks,CancellationToken token)
    {
        foreach(var(email,expectedRole,module)in new[]{("owner@botzone.local","owner","landing"),("nv01@botzone.local","order_staff","invoices"),("warehouse@botzone.local","warehouse_staff","inventory"),("content@botzone.local","content_staff","landing")})
        {
            using var api=new ApiClient(apiUrl);var actor=await api.LoginAsync(email,Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026",token);if(actor.Role!=expectedRole)throw new InvalidOperationException("Fixture role mismatch.");
            using var form=new MainForm(api,actor,false);form.Show();await form.NavigateAsync(module);await Task.Delay(120,token);
            if(form.CurrentPage?.HasLoadError!=false)throw new InvalidOperationException(expectedRole+": "+form.CurrentPage?.StatusMessage);
            if(module=="landing")
            {
                foreach(var controlName in new[]{"saveLanding","publishLanding","restoreLanding"})
                {
                    var control=form.Controls.Find(controlName,true).Single();if(control.Enabled!=(expectedRole=="content_staff"))throw new InvalidOperationException(expectedRole+" CMS button has wrong enabled state: "+controlName);
                }
                if(expectedRole=="content_staff"&&form.CurrentPage is LandingView landing)
                {
                    var before=landing.CurrentRevision;((Button)form.Controls.Find("saveLanding",true).Single()).PerformClick();var deadline=DateTime.UtcNow.AddSeconds(15);
                    while(landing.CurrentRevision==before&&!landing.HasLoadError&&DateTime.UtcNow<deadline)await Task.Delay(60,token);
                    if(landing.CurrentRevision<=before)throw new InvalidOperationException("Native Save draft did not complete: "+landing.StatusMessage);
                    checks.Add(new{test="native-CMS-save-button",pass=true,oldRevision=before,newRevision=landing.CurrentRevision,note="Saved the existing section content through the native button; no publish."});
                }
            }
            using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(output,"role-"+expectedRole+".png"),ImageFormat.Png);}
            var focusMoved=form.SelectNextControl(null,true,true,true,true);checks.Add(new{test="native-role-and-focus-traversal",role=expectedRole,pass=true,module,deviceDpi=form.DeviceDpi,programmaticFocusTraversal=focusMoved,note="Native SelectNextControl; physical keyboard/manual interaction not claimed."});
            async Task Forbidden(string label,Func<Task> request)
            {
                try{await request();throw new InvalidOperationException(label+" unexpectedly succeeded.");}catch(ApiException ex)when(ex.Status==403){checks.Add(new{test=label,role=expectedRole,pass=true,status=403});}
            }
            if(expectedRole=="owner")await Forbidden("owner-cannot-publish-CMS",async()=>await api.CommandAsync("publish_landing",new{pageSlug="home",revision=0},Guid.NewGuid().ToString(),token));
            if(expectedRole=="content_staff")
            {
                await Forbidden("content-cannot-read-financial-invoices",async()=>await api.PostAsync<InvoiceListResult>("api/admin/invoices/search",new InvoiceQuery(),token));
                await Forbidden("content-cannot-edit-price",async()=>await api.CommandAsync("update_variant",new{variantId=Guid.NewGuid(),priceVnd="1",active=true,standardWarrantyMonths=12,careFeeVnd=(string?)null},Guid.NewGuid().ToString(),token));
            }
            if(expectedRole=="warehouse_staff")await Forbidden("warehouse-cannot-export-financial-invoices",async()=>await api.PostAsync<InvoiceExportResult>("api/invoices/exports",new ExportRequest(),token));
            if(expectedRole=="order_staff")
            {
                var report=await api.PostAsync<InvoiceStatisticsResult>("api/admin/reports/invoices",new ReportRequest{GroupBy="staff",Query=new InvoiceQuery{FromInclusive="2024-01-01",ToExclusive="2027-01-01"}},token);
                if(report.Rows.Any(row=>row.DimensionId!=actor.Id))throw new InvalidOperationException("Staff report contains another staff scope.");checks.Add(new{test="order-staff-report-own-scope",pass=true,rowCount=report.Rows.Count,staffId=actor.Id});
                await Forbidden("order-staff-cannot-read-settings",async()=>await api.GetAsync<JsonElement>("api/admin/data/settings",token));
            }
            await api.LogoutAsync(token);if(await api.SessionAsync(token)is not null)throw new InvalidOperationException("Logout did not revoke session.");checks.Add(new{test="role-logout",role=expectedRole,pass=true});
        }
    }
}
