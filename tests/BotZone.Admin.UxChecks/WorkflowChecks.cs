using System.Collections;
using System.Data;
using System.Reflection;
using System.Text.Json;
using BotZone.Admin.WinForms;

internal sealed partial class Harness
{
    private static object? Invoke(object value,string name,params object?[] args)=>value.GetType().GetMethod(name,Instance)!.Invoke(value,args);
    private static async Task Until(Func<bool> condition,string failure){for(var i=0;i<150;i++){if(condition())return;await Task.Delay(80);}throw new InvalidOperationException(failure);}
    private async Task<Form> OpenCommand(Control page,Button button,Form owner,string name)
    {
        Check(name+"-enabled",button.Enabled);if(!button.Enabled)throw new InvalidOperationException(name+" is disabled.");
        button.PerformClick();await Until(()=>Application.OpenForms.Cast<Form>().Any(f=>f.GetType().Name=="NativeCommandForm"),name+" did not open in one click.");
        var form=Application.OpenForms.Cast<Form>().Single(f=>f.GetType().Name=="NativeCommandForm");await Foreground(form);await Capture(form,name);return form;
    }
    private async Task Workflows(ApiClient api,Form main)
    {
        await OverviewDialogs(main);
        await Call(main,"NavigateAsync","overview",null);var overview=(Control)Property(main,"CurrentPage")!;
        Check("overview-has-no-table",!Desc(overview).OfType<DataGridView>().Any());
        var distribution=Field<Control>(overview,"distribution");var points=((IEnumerable)Property(distribution,"Points")!).Cast<object>().ToList();
        var response=await api.GetAsync<JsonElement>("api/admin/overview");var expected=response.GetProperty("orders").EnumerateArray().Sum(x=>x.GetProperty("count").GetInt32());
        Check("overview-chart-preserves-order-total",points.Sum(p=>(decimal)Property(p,"Value")!)==expected);
        if(points.Count>0)
        {
            var point=points.FindIndex(p=>(string?)Property(p,"Key")=="handed_off");if(point<0)point=0;Invoke(distribution,"SelectRow",point);
            var args=new object?[]{Message.Create(distribution.Handle,0,IntPtr.Zero,IntPtr.Zero),Keys.Enter};Invoke(distribution,"ProcessCmdKey",args);
            await Until(()=>Property(main,"CurrentPage")?.GetType().Name=="OrdersView","Chart did not open orders.");
            var orders=Property(main,"CurrentPage")!;await Until(()=>((ComboBox)Field<ComboBox>(orders,"view")).SelectedItem is {} item&&(string?)Property(item,"Value")== (string?)Property(points[point],"Key"),"Chart filter was lost.");Check("overview-enter-filters-the-selected-status",true);
        }
        await Call(main,"NavigateAsync","catalogue",null);var catalogue=(Control)Property(main,"CurrentPage")!;
        Check("catalogue-direct-actions",new[]{"create_product","create_variant","set_compatibility","update_product_status","update_variant"}.All(command=>Desc(catalogue).OfType<Button>().Any(b=>b.Name=="action_"+command)));
        Check("catalogue-no-action-picker",!Desc(catalogue).OfType<ComboBox>().Any(c=>c.AccessibleName=="Thao tác theo quyền"));
        var grid=Field<DataGridView>(catalogue,"grid");var selected=(DataRowView)grid.CurrentRow!.DataBoundItem!;
        var beforeWrites=requests.Entries.Count(x=>x.Path=="/api/commands");
        using(var edit=await OpenCommand(catalogue,Desc(catalogue).OfType<Button>().Single(b=>b.Name=="action_update_variant"),main,"catalogue-edit-direct"))
        {
            var context=Field<IDictionary>(edit,"context");var price=edit.Controls.Find("priceVnd",true).OfType<TextBox>().Single();
            Check("catalogue-action-keeps-variant-id",Convert.ToString(context["variantId"])==Convert.ToString(selected["variant_id"]));
            Check("catalogue-action-prefills-selected-price",price.Text==Convert.ToString(selected["price_vnd"]));edit.Close();
        }
        await Foreground(main);
        using(var create=await OpenCommand(catalogue,Desc(catalogue).OfType<Button>().Single(b=>b.Name=="action_create_variant"),main,"catalogue-add-variant-direct"))
        {
            var context=Field<IDictionary>(create,"context");Check("new-variant-prefills-selected-product",Convert.ToString(context["productId"])==Convert.ToString(selected["id"]));create.Close();
        }
        await Foreground(main);await Call(main,"NavigateAsync","settings",null);var settings=(Control)Property(main,"CurrentPage")!;
        Check("settings-cards-have-no-table",!Desc(settings).OfType<DataGridView>().Any());
        Check("settings-no-raw-setting-keys",!Desc(settings).OfType<Label>().Any(l=>l.Text is "commerce" or "exchangeRateVnd"));
        using(var edit=await OpenCommand(settings,Desc(settings).OfType<Button>().Single(b=>b.Name=="edit_shipping"),main,"settings-edit-direct"))
        {
            var responseSettings=await api.GetAsync<JsonElement>("api/admin/data/settings?page=1&pageSize=20");var source=responseSettings.GetProperty("rows").EnumerateArray().Single(x=>x.GetProperty("id").GetString()=="commerce");
            Check("settings-preserves-current-policy",edit.Controls.Find("shippingVnd",true).OfType<TextBox>().Single().Text==source.GetProperty("value").GetProperty("shippingVnd").ToString());
            Check("shipping-edit-shows-only-relevant-fields",!edit.Controls.Find("pointsPerVnd",true).Any()&&!edit.Controls.Find("returnDays",true).Any());
            var context=Field<IDictionary>(edit,"context");Check("shipping-edit-retains-other-policy-values",Convert.ToString(context["pointsPerVnd"])==source.GetProperty("value").GetProperty("pointsPerVnd").ToString());edit.Close();
        }
        foreach(var section in new[]{"returns","points","currency"})
        {
            await Foreground(main);
            using var edit=await OpenCommand(settings,Desc(settings).OfType<Button>().Single(b=>b.Name=="edit_"+section),main,"settings-edit-"+section);
            var context=Field<IDictionary>(edit,"context");
            Check("settings-section-"+section,section=="currency"?edit.Controls.Find("exchangeRateVnd",true).Any():context.Contains("shippingVnd")&&context.Contains("returnDays")&&context.Contains("pointsPerVnd"));edit.Close();
        }
        await Foreground(main);await Call(main,"NavigateAsync","reports",null);var report=(Control)Property(main,"CurrentPage")!;var chart=Field<Control>(report,"chart");var reportGrid=Field<DataGridView>(report,"grid");
        Check("invoice-report-opens-as-chart",chart.Visible&&!reportGrid.Visible);
        var binding=Field<BindingSource>(report,"binding");if(binding.Count>1){Invoke(chart,"SelectRow",1);Check("chart-selection-matches-source-row",binding.Position==1);}
        Desc(report).OfType<Button>().Single(b=>b.Name=="chartTable").PerformClick();Check("table-one-click-switch",reportGrid.Visible&&!chart.Visible);
        Desc(report).OfType<Button>().Single(b=>b.Name=="chartLine").PerformClick();Check("chart-one-click-switch",chart.Visible&&!reportGrid.Visible);
        var samples=new[]{("Bars",new[]{-10m,0m,20m}), ("Line",new[]{-10m,0m,20m}), ("Donut",new[]{0m,0m,0m})};
        foreach(var (kind,values) in samples)
        {
            using var form=new Form{Text="Chart edge cases",Size=new Size(700,460),StartPosition=FormStartPosition.CenterScreen};var visual=(Control)Create("InsightChart");visual.Dock=DockStyle.Fill;form.Controls.Add(visual);
            var enumType=admin.GetType("BotZone.Admin.WinForms.InsightKind")!;visual.GetType().GetProperty("Kind",Instance)!.SetValue(visual,Enum.Parse(enumType,kind));
            var pointType=admin.GetType("BotZone.Admin.WinForms.InsightPoint")!;var array=Array.CreateInstance(pointType,values.Length);for(var i=0;i<values.Length;i++)array.SetValue(Create("InsightPoint","Mục "+(i+1),values[i],null),i);
            Invoke(visual,"SetPoints",array,"Kiểm tra giá trị âm và bằng không","₫");form.Show();await Foreground(form);Invoke(visual,"SelectRow",values.Length-1);await Capture(form,"chart-edge-"+kind.ToLowerInvariant());Check("chart-keeps-signed-values-"+kind,((IEnumerable)Property(visual,"Points")!).Cast<object>().Select(p=>(decimal)Property(p,"Value")!).SequenceEqual(values));form.Close();
        }
        await Foreground(main);Check("opening-and-cancelling-actions-does-not-write",requests.Entries.Count(x=>x.Path=="/api/commands")==beforeWrites);
        await WarrantyRules(api,main);await OtherRoles(main);
    }
    private async Task WarrantyRules(ApiClient api,Form main)
    {
        var type=admin.GetType("BotZone.Admin.WinForms.ModuleActions")!;var actions=((IEnumerable)type.GetMethod("For",Static)!.Invoke(null,["warranty"])!).Cast<object>().ToList();
        Check("warranty-actions-do-not-ask-for-a-second-action",actions.All(a=>!((IEnumerable)Property(a,"Fields")!).Cast<object>().Any(f=>(string?)Property(f,"Key")=="action")));
        var row=JsonSerializer.SerializeToElement(new{id=Guid.NewGuid(),variant_id=Guid.NewGuid(),state="repairing"});var available=type.GetMethod("Available",Static)!;
        var relevant=actions.Where(a=>((IEnumerable)Property(a,"Roles")!).Cast<string>().Contains("warehouse_staff")).ToList();
        var steps=new List<string>();foreach(var a in relevant){var context=(IDictionary)((Delegate)Property(a,"Context")!).DynamicInvoke(row)!;if((bool)available.Invoke(null,[a,row])!)steps.Add((string)context["action"]!);}
        Check("warranty-repairing-offers-complete-or-unrepairable",steps.Order().SequenceEqual(new[]{"repaired","unrepairable"}.Order()));
        var replace=actions.Single(a=>((IEnumerable)Property(a,"Roles")!).Cast<string>().Contains("warehouse_staff")&&(string?)((IDictionary)((Delegate)Property(a,"Context")!).DynamicInvoke(row)!)["action"]=="replace");
        var fields=((IEnumerable)Property(replace,"Fields")!).Cast<object>().Select(f=>(string)Property(f,"Key")!).ToArray();Check("replacement-still-asks-for-device-or-lot",fields.Contains("replacementUnitId")&&fields.Contains("replacementLotId"));
        await Task.CompletedTask;
    }
    private async Task OtherRoles(Form owner)
    {
        owner.Hide();
        try
        {
            foreach(var role in new[]{"warehouse","order","content"})
            {
                using var api=new ApiClient(Environment.GetEnvironmentVariable("BOTZONE_API_URL")??"http://127.0.0.1:5080");var actor=await api.LoginAsync((role=="order"?"nv01":role)+"@botzone.local",Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026");
                using var main=(Form)Create("MainForm",api,actor,false);main.Show();Fit(main,true);await Foreground(main);
                var modules=role=="warehouse"?new[]{"overview","purchases","warranty","operational_reports"}:role=="order"?new[]{"orders","returns","trade_ins","warranty","reports"}:new[]{"overview","catalogue","landing","media"};
                foreach(var module in modules){await Call(main,"NavigateAsync",module,null);await Settle();var page=(Control)Property(main,"CurrentPage")!;Check(role+"-"+module+"-loads",!(bool)Property(page,"HasLoadError")!);await Capture(main,role+"-"+module);if(role=="warehouse")Check(role+"-"+module+"-no-owner-approval",!Desc(page).OfType<Button>().Any(b=>b.Name=="action_approve_purchase"));}
                main.Close();await Until(()=>main.IsDisposed,"Role window did not finish closing.");Check(role+"-close-ends-session",api.Actor is null);
            }
        }
        finally{owner.Show();await Foreground(owner);}
    }
}
