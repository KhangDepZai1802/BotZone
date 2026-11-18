using System.Drawing.Imaging;

namespace BotZone.Admin.WinForms;

internal static class CustomerSmoke
{
    internal static async Task RunAsync(ApiClient owner,string output,List<object> checks,CancellationToken token)
    {
        await Check(owner);
        using var staff=new ApiClient(owner.BaseUri.ToString());await staff.LoginAsync("nv01@botzone.local",Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026",token);
        try{await Check(staff);}finally{await staff.LogoutAsync(token);}
        async Task Check(ApiClient api)
        {
            using var main=new MainForm(api,api.Actor!,false);main.Show();await main.NavigateAsync("customers");
            if(main.CurrentPage is not ModuleView module||module.HasLoadError)throw new InvalidOperationException("Native customer list failed: "+main.CurrentPage?.StatusMessage);
            module.OpenFirstCustomer();var limit=DateTime.UtcNow.AddSeconds(25);while((module.ActiveCustomerDetail is null||!module.ActiveCustomerDetail.Loaded&&!module.ActiveCustomerDetail.HasLoadError)&&DateTime.UtcNow<limit)await Task.Delay(50,token);
            var detail=module.ActiveCustomerDetail??throw new InvalidOperationException("Native customer detail did not open.");
            try
            {
                if(!detail.Loaded||detail.HasLoadError)throw new InvalidOperationException(detail.StatusMessage);
                var tabs=Descendants(detail).OfType<TabControl>().Single();if(tabs.TabCount!=8)throw new InvalidOperationException("Expected summary + seven history tabs.");
                foreach(TabPage tab in tabs.TabPages){tabs.SelectedTab=tab;detail.PerformLayout();detail.Refresh();await Task.Delay(70,token);using var bitmap=new Bitmap(detail.Width,detail.Height);detail.DrawToBitmap(bitmap,new Rectangle(Point.Empty,detail.Size));bitmap.Save(Path.Combine(output,api.Actor!.Role+"-customer-tab-"+tabs.SelectedIndex+".png"),ImageFormat.Png);}
                checks.Add(new{test="native-customer-list-detail",pass=true,role=api.Actor!.Role,tabCount=tabs.TabCount,rows=detail.RowCounts,status=detail.StatusMessage,deviceDpi=detail.DeviceDpi});
            }
            finally{detail.Close();main.Dispose();}
        }
        static IEnumerable<Control> Descendants(Control control){foreach(Control child in control.Controls){yield return child;foreach(var descendant in Descendants(child))yield return descendant;}}
    }
}
