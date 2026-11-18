using System.Drawing.Imaging;
using System.Text.Json;

namespace BotZone.Admin.WinForms;

/// <summary>Explicit integration mode; real native fields/buttons and API-created clone fixtures.</summary>
internal static class CrudSmoke
{
    internal static async Task RunAsync(ApiClient api,string output,List<object> checks,CancellationToken token)
    {
        async Task<JsonElement> FormCommand(string module,string command,object row,Dictionary<string,object?> values,bool edit=false)
        {
            var actions=ModuleActions.For(module).Where(x=>x.Command==command).ToList();var spec=edit?actions.Last():actions.First();var lookups=await api.GetAsync<JsonElement>("api/admin/lookups",token);
            using var form=new NativeCommandForm(api,spec,JsonSerializer.SerializeToElement(row),lookups);form.Show();
            foreach(var(key,value)in values)
            {
                var control=form.Controls.Find(key,true).Single();switch(control)
                {
                    case TextBox text:text.Text=value?.ToString()??"";break;
                    case CheckBox checkbox:checkbox.Checked=Convert.ToBoolean(value);break;
                    case NumericUpDown number:number.Value=Convert.ToDecimal(value);break;
                    case ComboBox combo:var found=false;for(var i=0;i<combo.Items.Count;i++)if(((Choice)combo.Items[i]!).Value==value?.ToString()){combo.SelectedIndex=i;found=true;break;}if(!found)throw new InvalidOperationException("Lookup missing for "+key);break;
                    case CheckedListBox list:var selected=((IEnumerable<string>)value!).ToHashSet();for(var i=0;i<list.Items.Count;i++)list.SetItemChecked(i,selected.Contains(((Choice)list.Items[i]).Value));break;
                    default:throw new InvalidOperationException("Unsupported smoke field "+key);
                }
            }
            await Task.Delay(100,token);using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(output,module+"-"+command+(edit?"-edit":"")+".png"),ImageFormat.Png);}
            form.AcceptButton!.PerformClick();var until=DateTime.UtcNow.AddSeconds(20);while(!form.Completed&&!form.IsDisposed&&DateTime.UtcNow<until)await Task.Delay(50,token);
            if(!form.Completed)throw new InvalidOperationException(command+": "+form.SubmissionStatus);checks.Add(new{test="native-command-form",command,pass=true,result=form.CommandResult.Clone()});return form.CommandResult.Clone();
        }
        var marker="WF"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();var slug=marker.ToLowerInvariant();
        var category=await FormCommand("categories","save_category",new{},new(){["code"]=marker,["slug"]=slug,["nameVi"]="Nhóm kiểm thử WinForms",["nameEn"]="WinForms test category",["active"]=true});var categoryId=Ui.Text(category,"id");
        var product=await FormCommand("catalogue","create_product",new{},new(){["categoryId"]=categoryId,["modelCode"]=marker,["slug"]="product-"+slug,["nameVi"]="Model kiểm thử WinForms",["nameEn"]="WinForms test model",["generation"]="Local smoke",["sourceUrl"]="https://www.apple.com/vn/",["poster"]="/media/accessories.svg"});var productId=Ui.Text(product,"id");
        var variant=await FormCommand("catalogue","create_variant",new{id=productId},new(){["productId"]=productId,["sku"]=marker+"-SKU",["priceVnd"]="123456",["trackingMode"]="quantity",["color"]="Smoke",["storage"]="Test",["standardWarrantyMonths"]=12,["active"]=false});
        await FormCommand("catalogue","update_product_status",new{id=productId,business_status="draft"},new(){["businessStatus"]="active",["reason"]="Native CRUD test activates unpublished draft"});
        var lookup=await api.GetAsync<JsonElement>("api/admin/lookups",token);var target=Ui.Rows(lookup,"products").First(p=>Ui.Text(p,"id")!=productId);await FormCommand("catalogue","set_compatibility",new{id=productId},new(){["accessoryProductId"]=productId,["targetProductIds"]=new[]{Ui.Text(target,"id")}});
        var supplier=await FormCommand("suppliers","save_supplier",new{},new(){["code"]=marker,["name"]="Nhà cung cấp kiểm thử native",["contact"]="Local integration fixture only",["active"]=false});
        var settings=await api.GetAsync<JsonElement>("api/admin/data/settings",token);foreach(var row in Ui.Rows(settings))if(Ui.Text(row,"id") is "exchangeRateVnd" or "commerce")
        {
            var commerce=Ui.Text(row,"id")=="commerce";await FormCommand("settings","update_settings",row,new(){["reason"]="Native CRUD roundtrip retains current setting"},commerce);
        }
        using(var content=new ApiClient(api.BaseUri.ToString()))
        {
            await content.LoginAsync("content@botzone.local",Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026",token);
            try{await content.CommandAsync("update_product_status",new{productId,businessStatus="active",role="owner",reason="Role spoof must be rejected"},Guid.NewGuid().ToString(),token);throw new InvalidOperationException("Content role spoof succeeded.");}catch(ApiException ex)when(ex.Status==403){checks.Add(new{test="content-cannot-spoof-owner-CRUD",pass=true,status=403});}
            await content.LogoutAsync(token);
        }
        await FormCommand("catalogue","update_product_status",new{id=productId,business_status="active"},new(){["businessStatus"]="archived",["reason"]="Archive test fixture without deleting history"});
        await FormCommand("categories","save_category",new{id=categoryId,code=marker,slug,name_vi="Nhóm kiểm thử WinForms",name_en="WinForms test category",active=true},new(){["active"]=false},true);
        var final=await api.GetAsync<JsonElement>("api/admin/data/settings",token);foreach(var original in Ui.Rows(settings).Where(x=>Ui.Text(x,"id") is "commerce" or "exchangeRateVnd"))
        {
            var updated=Ui.Rows(final).Single(x=>Ui.Text(x,"id")==Ui.Text(original,"id"));if(!JsonElement.DeepEquals(original.GetProperty("value"),updated.GetProperty("value")))throw new InvalidOperationException("Settings value changed in roundtrip: "+Ui.Text(original,"id"));
        }
        checks.Add(new{test="CRUD-fixtures-preserved",pass=true,marker,categoryId,productId,variantId=Ui.Text(variant,"id"),supplierId=Ui.Text(supplier,"id"),note="New clone fixtures preserved; model archived, category/supplier inactive; settings values unchanged with new audit versions."});
    }
}
