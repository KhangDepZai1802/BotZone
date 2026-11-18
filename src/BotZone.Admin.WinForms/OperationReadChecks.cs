using System.Drawing.Imaging;
using System.Text.Json;

namespace BotZone.Admin.WinForms;

/// <summary>Native navigation and input-only checks. No business command is submitted.</summary>
internal static class OperationReadChecks
{
    internal static async Task RunAsync(ApiClient api, MainForm form, string output, List<object> checks, CancellationToken token)
    {
        var role = api.Actor?.Role ?? "unknown";
        if (role is "owner" or "order_staff" or "warehouse_staff")
        {
            await form.NavigateAsync("orders");
            var orders = form.CurrentPage as OrdersView ?? throw new InvalidOperationException("Orders view was not opened.");
            var data = await api.GetAsync<JsonElement>("api/orders", token);
            var row = Ui.Rows(data, "orders").FirstOrDefault();
            if (row.ValueKind == JsonValueKind.Object)
            {
                var search = Descendants(orders).OfType<TextBox>().Single(x => x.AccessibleName == "Tìm kiếm" || x.AccessibleName == "Tìm mã đơn");
                var orderNo = Ui.Text(row, "order_no"); search.Text = orderNo;
                orders.SelectAndOpen(Ui.Text(row, "id"));
                await Until(() => orders.ActiveDetail?.CurrentState is not null, "Embedded order detail did not load.");
                var detail = orders.ActiveDetail!;
                if (detail.TopLevel || detail.Parent != orders) throw new InvalidOperationException("Order detail is not embedded in its list workspace.");
                var detailTabs=Descendants(detail).OfType<TabControl>().Single();
                if(detailTabs.TabCount>3)throw new InvalidOperationException("Order detail still exposes more than three primary sections.");
                var primaryItems=Descendants(detailTabs.TabPages[0]).OfType<DataGridView>().Single();
                if(primaryItems.Columns.Cast<DataGridViewColumn>().Count(x=>x.Visible)>5)throw new InvalidOperationException("Order items default grid exposes secondary columns.");
                checks.Add(new{test="native-order-progressive-sections",role,pass=true,primarySections=detailTabs.TabCount,defaultItemColumns=primaryItems.Columns.Cast<DataGridViewColumn>().Count(x=>x.Visible)});
                Capture(form, role + "-order-detail-embedded");
                foreach(TabPage page in detailTabs.TabPages){detailTabs.SelectedTab=page;await Task.Delay(50,token);Capture(form,role+"-order-section-"+detailTabs.SelectedIndex);}detailTabs.SelectedIndex=0;
                var action = Descendants(detail).OfType<Button>().FirstOrDefault(x => x.Name.Length > 0 && x.Enabled);
                if (action is not null)
                {
                    action.PerformClick(); await Until(() => detail.ActiveCommand is {Visible:true,IsHandleCreated:true}, "Order confirmation did not open.");
                    var command = detail.ActiveCommand!;await Task.Delay(80,token); Capture(command, role + "-order-confirmation-no-submit"); command.Close();
                    await Until(() => detail.ActiveCommand is null, "Order confirmation did not close.");
                    checks.Add(new { test = "native-order-command-open-close-without-submit", role, action = action.Name, pass = true });
                }
                detail.Close(); await Until(() => orders.ActiveDetail is null, "Back did not restore the order list.");
                if (search.Text != orderNo) throw new InvalidOperationException("Back discarded the order search.");
                await form.NavigateAsync("catalogue"); await form.NavigateAsync("orders");
                if (!ReferenceEquals(form.CurrentPage, orders) || search.Text != orderNo) throw new InvalidOperationException("Module return discarded order context.");
                Capture(form, role + "-orders-return-context");
                checks.Add(new { test = "native-order-embedded-back-and-module-context", role, pass = true, filter = orderNo, deviceDpi = form.DeviceDpi });
            }
        }
        if (role == "owner")
        {
            await form.NavigateAsync("catalogue");
            var catalogue = form.CurrentPage ?? throw new InvalidOperationException("Catalogue did not open.");
            var grid = Descendants(catalogue).OfType<DataGridView>().Single();
            if(grid.Columns.Cast<DataGridViewColumn>().Count(x=>x.Visible)>5)throw new InvalidOperationException("Catalogue default grid is not reduced to essential columns.");
            checks.Add(new{test="native-catalogue-essential-columns",pass=true,columns=grid.Columns.Cast<DataGridViewColumn>().Where(x=>x.Visible).Select(x=>x.Name).ToArray()});
            var sameProduct = grid.Rows.Cast<DataGridViewRow>().Where(r => r.DataBoundItem is System.Data.DataRowView)
                .GroupBy(r => Convert.ToString(((System.Data.DataRowView)r.DataBoundItem!)["id"]))
                .FirstOrDefault(group => group.Count() > 1);
            if (sameProduct is not null)
            {
                var row = sameProduct.Last(); var selected = (System.Data.DataRowView)row.DataBoundItem!;
                grid.CurrentCell = row.Cells["sku"]; row.Selected = true;
                var commands = Descendants(catalogue).OfType<ComboBox>().Single(x => x.AccessibleName == "Thao tác theo quyền");
                for (var i = 0; i < commands.Items.Count; i++) if ((commands.Items[i] as Choice)?.Value == "update_variant") commands.SelectedIndex = i;
                Descendants(catalogue).OfType<Button>().Single(x => x.Text == "Thực hiện").PerformClick();
                await Until(() => Application.OpenForms.OfType<NativeCommandForm>().Any(x=>x.Visible&&x.IsHandleCreated), "Variant edit form did not open.");
                var command = Application.OpenForms.OfType<NativeCommandForm>().Single();
                await Task.Delay(80,token);
                var sku = Convert.ToString(selected["sku"])!;
                if (!Descendants(command).OfType<Label>().Any(x => x.Text.Contains(sku, StringComparison.Ordinal))) throw new InvalidOperationException("Catalogue action targeted a different SKU of the same product.");
                var price = command.Controls.Find("priceVnd", true).OfType<TextBox>().Single();
                if (price.Text != Convert.ToString(selected["price_vnd"])) throw new InvalidOperationException("Catalogue edit did not retain selected SKU price.");
                Capture(command, "owner-selected-sku-no-submit"); command.Close();
                await Until(() => !Application.OpenForms.OfType<NativeCommandForm>().Any(), "Variant edit form did not close.");
                checks.Add(new { test = "native-catalogue-action-targets-selected-variant", pass = true, sku, persisted = false });
            }
            if(grid.CurrentRow?.DataBoundItem is System.Data.DataRowView selectedRow&&grid.Columns["care_fee_vnd"] is {} careColumn)
            {
                var selectedKey=Convert.ToString(selectedRow["variant_id"]);
                Descendants(catalogue).OfType<Button>().Single(x=>x.Text=="Cột hiển thị").PerformClick();
                await Until(()=>Application.OpenForms.OfType<OperationColumnsForm>().Any(x=>x.Visible),"Column picker did not open.");
                var picker=Application.OpenForms.OfType<OperationColumnsForm>().Single();var list=Descendants(picker).OfType<CheckedListBox>().Single();
                for(var i=0;i<list.Items.Count;i++)if(((Choice)list.Items[i]).Value=="care_fee_vnd")list.SetItemChecked(i,true);
                picker.AcceptButton!.PerformClick();await Until(()=>!Application.OpenForms.OfType<OperationColumnsForm>().Any(),"Column picker did not close.");
                await catalogue.RefreshAsync();
                if(grid.Columns["care_fee_vnd"] is not {Visible:true}||grid.CurrentRow?.DataBoundItem is not System.Data.DataRowView restored||Convert.ToString(restored["variant_id"])!=selectedKey)throw new InvalidOperationException("Column preference or selected record was lost after refresh.");
                Capture(form,"owner-catalogue-secondary-column");checks.Add(new{test="native-columns-preserve-preference-selection",pass=true,column="care_fee_vnd",persisted=false});
            }
            var sourceData=await api.GetAsync<JsonElement>("api/admin/data/catalogue?page=1&pageSize=20",token);var sourceRow=Ui.Rows(sourceData).FirstOrDefault();
            if(sourceRow.ValueKind==JsonValueKind.Object)
            {
                using var record=new RecordDetailForm(sourceRow,"Sản phẩm / SKU");record.Show(form);await Task.Delay(70,token);var sections=Descendants(record).OfType<TabControl>().Single();
                var scalarCount=sourceRow.EnumerateObject().Count(p=>p.Value.ValueKind is not(JsonValueKind.Array or JsonValueKind.Object));
                if(sections.TabCount>3||Descendants(sections.TabPages[0]).OfType<TextBox>().Count()>6||Descendants(record).OfType<TextBox>().Count()!=scalarCount)throw new InvalidOperationException("Record disclosure lost source fields or crowded the summary.");
                Capture(record,"owner-record-summary");if(sections.TabCount>1){sections.SelectedIndex=1;await Task.Delay(40,token);Capture(record,"owner-record-secondary-fields");}
                var sectionCount=sections.TabCount;record.Close();checks.Add(new{test="native-record-summary-keeps-all-scalars",pass=true,scalarCount,sections=sectionCount});
            }
            var lookups=await api.GetAsync<JsonElement>("api/admin/lookups",token);
            using(var compatibility=new NativeCommandForm(api,ModuleActions.For("catalogue").Single(x=>x.Command=="set_compatibility"),sourceRow,lookups))
            {
                compatibility.Show(form);compatibility.Size=new Size(1000,720);await Task.Delay(60,token);
                var picker=Descendants(compatibility).OfType<MultiLookupPicker>().Single();var list=Descendants(picker).OfType<CheckedListBox>().Single();var search=Descendants(picker).OfType<TextBox>().Single();
                if(list.Items.Count>=2)
                {
                    var first=(Choice)list.Items[0];var last=(Choice)list.Items[^1];list.SetItemChecked(0,true);list.SetItemChecked(list.Items.Count-1,true);
                    search.Text="no-match-input-only";
                    if(list.Items.Count!=0||!picker.SelectedValues.ToHashSet().SetEquals(new[]{first.Value,last.Value}))throw new InvalidOperationException("Filtering discarded the hidden selections.");
                    Capture(compatibility,"owner-multiselect-no-match-preserves-selection");search.Clear();Descendants(picker).OfType<CheckBox>().Single().Checked=true;
                    if(list.Items.Count!=2||picker.SelectedValues.Length!=2)throw new InvalidOperationException("Selected-only filter did not retain both chosen models.");
                    await Task.Delay(120,token);Capture(compatibility,"owner-multiselect-selected-only");checks.Add(new{test="native-multiselect-search-keeps-hidden-selections",pass=true,selected=2,persisted=false});
                }
            }
            using(var purchase=new NativeCommandForm(api,ModuleActions.For("purchases").Single(x=>x.Command=="create_purchase"),JsonSerializer.SerializeToElement(new{}),lookups))
            {
                purchase.Show(form);purchase.Size=new Size(1000,720);await Task.Delay(50,token);var lines=Descendants(purchase).OfType<LinesEditor>().Single();var lineGrid=Descendants(lines).OfType<DataGridView>().Single();
                Descendants(lines).OfType<Button>().Single(x=>x.Name=="addLine").PerformClick();await Until(()=>Application.OpenForms.OfType<OperationChoiceForm>().Any(x=>x.Visible),"Searchable item picker did not open.");
                var picker=Application.OpenForms.OfType<OperationChoiceForm>().Single();var search=Descendants(picker).OfType<TextBox>().Single();var results=Descendants(picker).OfType<ListBox>().Single();
                if(results.Items.Count==0)throw new InvalidOperationException("No SKU lookup choices available.");var first=(Choice)results.Items[0];search.Text=first.Label;
                if(results.Items.Count==0||picker.Selected?.Value!=first.Value)throw new InvalidOperationException("Item search lost the chosen SKU identity.");Capture(picker,"owner-line-item-search");picker.AcceptButton!.PerformClick();await Until(()=>!Application.OpenForms.OfType<OperationChoiceForm>().Any(),"Item picker did not close.");
                lineGrid.EndEdit();var row=lineGrid.Rows.Cast<DataGridViewRow>().Single(x=>!x.IsNewRow);row.Cells["quantity"].Value=0;row.Cells["value"].Value="1000";
                try{lines.Values();throw new InvalidOperationException("Invalid quantity was accepted.");}catch(InvalidOperationException ex)when(ex.Message!="Invalid quantity was accepted."){}
                if(lineGrid.CurrentCell?.OwningColumn?.Name!="quantity")throw new InvalidOperationException("Quantity error did not focus its cell.");row.Cells["quantity"].Value=1;row.Cells["value"].Value="-1";
                try{lines.Values();throw new InvalidOperationException("Invalid cost was accepted.");}catch(InvalidOperationException ex)when(ex.Message!="Invalid cost was accepted."){}
                if(lineGrid.CurrentCell?.OwningColumn?.Name!="value")throw new InvalidOperationException("Cost error did not focus its cell.");Capture(purchase,"owner-purchase-line-error-cell");row.Cells["value"].Value="1000";var payload=lines.Values().Single();
                if(payload["variantId"]?.ToString()!=first.Value||payload["unitCostVnd"]?.ToString()!="1000")throw new InvalidOperationException("New item picker changed the line contract.");
                Descendants(lines).OfType<Button>().Single(x=>x.Name=="removeLine").PerformClick();if(lineGrid.Rows.Cast<DataGridViewRow>().Any(x=>!x.IsNewRow))throw new InvalidOperationException("Remove line did not remove the selected input row.");
                checks.Add(new{test="native-line-picker-search-errors-remove-preserve-contract",pass=true,persisted=false});
            }
            using(var draft=new NativeCommandForm(api,ModuleActions.For("catalogue").Single(x=>x.Command=="create_product"),JsonSerializer.SerializeToElement(new{}),lookups))
            {
                draft.Show(form);await Task.Delay(80,token);
                var sections=Descendants(draft).OfType<TabControl>().Single();
                if(sections.TabCount!=2)throw new InvalidOperationException("Product form did not group its eight inputs.");
                foreach(var key in new[]{"modelCode","slug","nameVi","nameEn"})draft.Controls.Find(key,true).OfType<TextBox>().Single().Text="UI-input-only";
                var source=draft.Controls.Find("sourceUrl",true).OfType<TextBox>().Single();source.Clear();
                Capture(draft,"owner-product-form-primary-no-submit");draft.AcceptButton!.PerformClick();await Task.Delay(60,token);
                if(sections.SelectedTab?.Text!="Thông số & ảnh"||!source.Focused||draft.Controls.Find("nameVi",true).OfType<TextBox>().Single().Text!="UI-input-only"||draft.Completed)throw new InvalidOperationException("Cross-section validation lost focus/input or attempted completion.");
                Capture(draft,"owner-product-form-error-section-no-submit");
                checks.Add(new{test="native-form-section-validation-preserves-input",pass=true,sections=sections.TabCount,field="sourceUrl",persisted=false});
            }
        }
        if (role == "warehouse_staff")
        {
            var lookups = await api.GetAsync<JsonElement>("api/admin/lookups", token);
            var purchaseLine = Ui.Rows(lookups, "purchaseItems").FirstOrDefault();
            if (purchaseLine.ValueKind == JsonValueKind.Object)
            {
                var selected = JsonSerializer.SerializeToElement(new { id = Ui.Text(purchaseLine, "purchaseOrderId") });
                using var receipt = new NativeCommandForm(api, ModuleActions.For("purchases").Single(a => a.Command == "receive_goods"), selected, lookups);
                receipt.Show(form);
                var lines = Descendants(receipt).OfType<LinesEditor>().Single();
                var grid = Descendants(lines).OfType<DataGridView>().Single();
                var id = Ui.Text(purchaseLine, "id");
                var rowIndex = grid.Rows.Add(id, Ui.Text(purchaseLine, "quantity"), 1, "");
                grid.CurrentCell = grid.Rows[rowIndex].Cells["value"];
                var scanner = Descendants(lines).OfType<TextBox>().Single();
                var scan = Descendants(lines).OfType<Button>().Single(x=>x.Name=="recordSerial");
                scanner.Text = "UI-INPUT-ONLY-001"; scanner.Focus(); scan.PerformClick();
                if (!scanner.Focused || scanner.Text.Length != 0 || Convert.ToString(grid.Rows[rowIndex].Cells["value"].Value) != "UI-INPUT-ONLY-001") throw new InvalidOperationException("Successful scan did not preserve scanner focus and append the serial.");
                scanner.Text = "UI-INPUT-ONLY-001"; scan.PerformClick();
                if (!scanner.Focused || scanner.Text != "UI-INPUT-ONLY-001") throw new InvalidOperationException("Duplicate scan discarded input or focus.");
                if (lines.Values().Count != 1) throw new InvalidOperationException("Input-only receipt line was not retained.");
                Capture(receipt, "warehouse-receipt-input-only");
                checks.Add(new { test = "native-receipt-scanner-focus-duplicate-preserves-input", pass = true, persisted = false, note = "Synthetic serial exists only in this control; no command submitted.", deviceDpi = receipt.DeviceDpi });
                // Dispose a test-owned form instead of invoking the user's dirty-close confirmation.
            }
        }
        if(role=="content_staff")
        {
            using var metadata=new MediaMetadataForm();metadata.Show(form);await Task.Delay(60,token);var source=metadata.Controls.Find("mediaSource",true).OfType<TextBox>().Single();var license=metadata.Controls.Find("mediaLicense",true).OfType<TextBox>().Single();
            metadata.AcceptButton!.PerformClick();if(!source.Focused||metadata.DialogResult!=DialogResult.None)throw new InvalidOperationException("Media source validation did not keep the form open and focus source.");source.Text="UI input only";metadata.AcceptButton!.PerformClick();
            if(!license.Focused||source.Text!="UI input only"||metadata.DialogResult!=DialogResult.None)throw new InvalidOperationException("Media license validation lost source input or closed the form.");Capture(metadata,"content-media-inline-error-no-upload");checks.Add(new{test="native-media-metadata-errors-preserve-input",pass=true,persisted=false});
        }
        async Task Until(Func<bool> condition, string failure)
        {
            var until = DateTime.UtcNow.AddSeconds(25);
            while (!condition() && DateTime.UtcNow < until) await Task.Delay(40, token);
            if (!condition()) throw new InvalidOperationException(failure);
        }
        void Capture(Form target, string name)
        {
            target.PerformLayout(); target.Refresh();
            using var bitmap = new Bitmap(target.Width, target.Height);
            target.DrawToBitmap(bitmap, new Rectangle(Point.Empty, target.Size));
            bitmap.Save(Path.Combine(output, name + ".png"), ImageFormat.Png);
        }
    }
    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
}
