using System.Drawing.Imaging;
using System.Text.Json;

namespace BotZone.Admin.WinForms;

/// <summary>Explicitly fulfills one pre-existing browser order using the real login/list/detail/command controls.</summary>
internal static class FulfillmentSmoke
{
    internal static async Task RunAsync(string apiUrl,string orderId,string output,List<object> checks,CancellationToken token)
    {
        if(!Guid.TryParse(orderId,out _))throw new InvalidOperationException("--fulfill-order requires one existing order GUID.");
        foreach(var stage in new[]{(Email:"nv01@botzone.local",Role:"order_staff",Command:"confirm_order",Before:"pending"),(Email:"warehouse@botzone.local",Role:"warehouse_staff",Command:"handoff",Before:"confirmed")})
        {
            using var login=new LoginForm();login.Show();login.FillCredentials(apiUrl,stage.Email,Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026");await Task.Delay(100,token);Capture(login,stage.Role+"-login");login.AcceptButton!.PerformClick();
            await Until(()=>login.ActiveMain is not null,"Native login: "+stage.Email,()=>login.LoginStatus);
            var main=login.ActiveMain!;var api=login.AuthenticatedApi!;
            try
            {
                if(api.Actor?.Role!=stage.Role)throw new InvalidOperationException("Unexpected role from native LoginForm.");
                checks.Add(new{test="native-login-button",pass=true,role=stage.Role});
                await Until(()=>main.CurrentPage is not null&&!string.IsNullOrWhiteSpace(main.CurrentPage.StatusMessage),"Initial main navigation",()=>main.CurrentPage?.StatusMessage??"Not shown");
                main.Size=new Size(1366,768);await main.NavigateAsync("orders");
                if(main.CurrentPage is not OrdersView orders||orders.HasLoadError)throw new InvalidOperationException("Native orders load failed: "+main.CurrentPage?.StatusMessage);
                Capture(main,stage.Role+"-orders");orders.SelectAndOpen(orderId);await Until(()=>orders.ActiveDetail?.CurrentState is not null,"Order detail load",()=>orders.ActiveDetail?.StatusText??"No dialog");
                var detail=orders.ActiveDetail!;if(detail.CurrentState!=stage.Before)throw new InvalidOperationException($"Expected {stage.Before}; actual {detail.CurrentState}. No command sent.");Capture(detail,stage.Role+"-before");
                var button=detail.Controls.Find(stage.Command,true).OfType<Button>().Single();if(!button.Enabled)throw new InvalidOperationException("Native action is disabled: "+stage.Command);button.PerformClick();await Until(()=>detail.ActiveCommand is not null,"Native command dialog",()=>detail.StatusText);
                var command=detail.ActiveCommand!;await Task.Delay(100,token);Capture(command,stage.Role+"-confirmation");command.AcceptButton!.PerformClick();await Until(()=>command.Completed,"Native confirmation: "+stage.Command,()=>command.SubmissionStatus);
                await Until(()=>detail.CurrentState!=stage.Before,"Refreshed order state",()=>detail.StatusText);Capture(detail,stage.Role+"-after");
                var actual=await api.GetAsync<JsonElement>("api/orders/"+orderId,token);var state=Ui.Text(actual.GetProperty("order"),"orderStatus","order_status");
                if(stage.Command=="confirm_order"&&state!="confirmed")throw new InvalidOperationException("Confirmation did not persist.");
                if(stage.Command=="handoff")
                {
                    var shipment=Ui.Rows(actual,"shipments").LastOrDefault();if(Ui.Text(shipment,"status")!="handed_off")throw new InvalidOperationException("Handoff shipment is absent.");
                    // Warehouse response intentionally omits financial fields; the browser customer verifies invoice after this native run.
                }
                checks.Add(new{test="native-order-command",pass=true,role=stage.Role,command=stage.Command,orderId,state,result=command.CommandResult.Clone(),deviceDpi=detail.DeviceDpi});detail.Close();
            }
            finally
            {
                if(!main.IsDisposed)main.Close();await Until(()=>login.ActiveMain is null,"Native logout",()=>login.LoginStatus);checks.Add(new{test="native-close-logout",pass=true,role=stage.Role});login.Close();
            }
        }
        async Task Until(Func<bool> done,string label,Func<string> detail){var limit=DateTime.UtcNow.AddSeconds(30);while(!done()&&DateTime.UtcNow<limit)await Task.Delay(50,token);if(!done())throw new InvalidOperationException(label+": "+detail());}
        void Capture(Form form,string name){form.PerformLayout();form.Refresh();using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(output,name+".png"),ImageFormat.Png);}
    }
}
