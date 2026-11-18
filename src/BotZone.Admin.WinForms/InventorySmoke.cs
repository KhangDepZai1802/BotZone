using System.Text.Json;

namespace BotZone.Admin.WinForms;

/// <summary>Explicit opt-in integration checks. Creates identifiable test POs through API only.</summary>
internal static class InventorySmoke
{
    internal static async Task<List<object>> RunAsync(ApiClient owner,CancellationToken token)
    {
        var checks=new List<object>();using var warehouse=new ApiClient(owner.BaseUri.ToString());await warehouse.LoginAsync("warehouse@botzone.local",Environment.GetEnvironmentVariable("BOTZONE_SMOKE_PASSWORD")??"BotZoneDemo!2026",token);
        async Task<JsonElement> Call(ApiClient api,string command,object input,string? key=null)=>await api.CommandAsync(command,input,key??Guid.NewGuid().ToString(),token);
        async Task Rejected(string label,ApiClient api,string command,object input,int expected)
        {
            try{await Call(api,command,input);throw new InvalidOperationException(label+" unexpectedly succeeded.");}catch(ApiException ex)when(ex.Status==expected){checks.Add(new{test=label,pass=true,expectedStatus=expected,code=ex.Code});}
        }
        try
        {
            var catalogue=await owner.GetAsync<JsonElement>("api/catalogue",token);var variants=Ui.Rows(catalogue,"products").SelectMany(p=>Ui.Rows(p,"variants")).ToList();var serial=variants.First(v=>Ui.Text(v,"trackingMode")=="serialized");var quantity=variants.First(v=>Ui.Text(v,"trackingMode")=="quantity");
            var lookups=await owner.GetAsync<JsonElement>("api/admin/lookups",token);var supplier=Ui.Rows(lookups,"suppliers").First();var marker="WF"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
            var purchase=await Call(warehouse,"create_purchase",new{supplierId=Ui.Text(supplier,"id"),items=new[]{new{variantId=Ui.Text(serial,"id"),quantity=2,unitCostVnd="10000"},new{variantId=Ui.Text(quantity,"id"),quantity=3,unitCostVnd="100"}}});var purchaseId=Ui.Text(purchase,"purchaseId");
            await Rejected("warehouse-cannot-approve-purchase",warehouse,"approve_purchase",new{purchaseId},403);await Call(owner,"approve_purchase",new{purchaseId});
            lookups=await warehouse.GetAsync<JsonElement>("api/admin/lookups",token);var items=Ui.Rows(lookups,"purchaseItems").Where(p=>Ui.Text(p,"purchaseOrderId")==purchaseId).ToList();var serialItem=items.First(p=>Ui.Text(p,"name")==Ui.Text(serial,"sku"));var quantityItem=items.First(p=>Ui.Text(p,"name")==Ui.Text(quantity,"sku"));var serialItemId=Ui.Text(serialItem,"id");
            var firstInput=new{purchaseId,items=new[]{new{purchaseOrderItemId=serialItemId,quantity=1,serials=new[]{marker+"A"}}}};await Rejected("owner-cannot-physically-receive",owner,"receive_goods",firstInput,403);
            var receiveKey=Guid.NewGuid().ToString();var first=await Call(warehouse,"receive_goods",firstInput,receiveKey);var replay=await Call(warehouse,"receive_goods",firstInput,receiveKey);if(Ui.Text(first,"receiptId")!=Ui.Text(replay,"receiptId"))throw new InvalidOperationException("Receipt retry duplicated receipt.");checks.Add(new{test="partial-receipt-idempotency",pass=true,purchaseId,receiptId=Ui.Text(first,"receiptId"),status=Ui.Text(first,"status")});
            await Rejected("over-receipt-rejected",warehouse,"receive_goods",new{purchaseId,items=new[]{new{purchaseOrderItemId=serialItemId,quantity=3,serials=new[]{marker+"B",marker+"C",marker+"D"}}}},409);
            await Rejected("duplicate-serial-rejected",warehouse,"receive_goods",new{purchaseId,items=new[]{new{purchaseOrderItemId=serialItemId,quantity=1,serials=new[]{marker+"A"}}}},409);
            var other=await Call(warehouse,"create_purchase",new{supplierId=Ui.Text(supplier,"id"),items=new[]{new{variantId=Ui.Text(quantity,"id"),quantity=1,unitCostVnd="100"}}});var otherId=Ui.Text(other,"purchaseId");await Call(owner,"approve_purchase",new{purchaseId=otherId});lookups=await warehouse.GetAsync<JsonElement>("api/admin/lookups",token);var otherLine=Ui.Rows(lookups,"purchaseItems").First(p=>Ui.Text(p,"purchaseOrderId")==otherId);
            await Rejected("cross-purchase-line-rejected",warehouse,"receive_goods",new{purchaseId,items=new[]{new{purchaseOrderItemId=Ui.Text(otherLine,"id"),quantity=1,serials=Array.Empty<string>()}}},404);
            var final=await Call(warehouse,"receive_goods",new{purchaseId,items=new[]{new{purchaseOrderItemId=serialItemId,quantity=1,serials=new[]{marker+"B"}},new{purchaseOrderItemId=Ui.Text(quantityItem,"id"),quantity=3,serials=Array.Empty<string>()}}});if(Ui.Text(final,"status")!="received")throw new InvalidOperationException("Purchase not marked received after exact quantities.");checks.Add(new{test="final-partial-receipt",pass=true,purchaseId,receiptId=Ui.Text(final,"receiptId")});
            var serialLot=first.GetProperty("lotIds")[0].GetString()!;lookups=await owner.GetAsync<JsonElement>("api/admin/lookups",token);var unit=Ui.Rows(lookups,"inventoryUnits").First(u=>Ui.Text(u,"lotId")==serialLot);var unitId=Ui.Text(unit,"id");
            var adjustment=await Call(warehouse,"request_stock_adjustment",new{lotId=serialLot,quantityDelta=-1,reason="WinForms integration test: isolate counted serial"});var adjustmentId=Ui.Text(adjustment,"adjustmentId");
            var approval=new{adjustmentId,approve=true,inventoryUnitIds=new[]{unitId},reason="WinForms integration test: owner confirms discrepancy"};await Rejected("warehouse-cannot-approve-adjustment",warehouse,"approve_stock_adjustment",approval,403);var approveKey=Guid.NewGuid().ToString();await Call(owner,"approve_stock_adjustment",approval,approveKey);await Call(owner,"approve_stock_adjustment",approval,approveKey);await Rejected("adjustment-second-application-rejected",owner,"approve_stock_adjustment",approval,409);
            var restore=await Call(warehouse,"request_stock_adjustment",new{lotId=serialLot,quantityDelta=1,reason="WinForms integration test: counted serial recovered"});await Call(owner,"approve_stock_adjustment",new{adjustmentId=Ui.Text(restore,"adjustmentId"),approve=true,inventoryUnitIds=new[]{unitId},reason="WinForms integration test: restore proven stock"});lookups=await owner.GetAsync<JsonElement>("api/admin/lookups",token);var restored=Ui.Rows(lookups,"inventoryUnits").First(u=>Ui.Text(u,"id")==unitId);if(Ui.Text(restored,"custody_status")!="available")throw new InvalidOperationException("Restored serial not available.");checks.Add(new{test="serialized-adjustment-and-provenance-recovery",pass=true,lotId=serialLot,unitId});
            var inventory=await owner.GetAsync<JsonElement>("api/admin/data/inventory?pageSize=100&q="+Uri.EscapeDataString(Ui.Text(quantity,"sku")),token);var finalLots=final.GetProperty("lotIds").EnumerateArray().Select(x=>x.GetString()).ToHashSet();var quantityLot=Ui.Rows(inventory).First(l=>finalLots.Contains(Ui.Text(l,"id"))&&Ui.Text(l,"variant_id")==Ui.Text(quantity,"id"));var quantityLotId=Ui.Text(quantityLot,"id");
            var below=await Call(warehouse,"request_stock_adjustment",new{lotId=quantityLotId,quantityDelta=-9999,reason="WinForms integration test: reject negative stock"});await Rejected("quantity-negative-stock-rejected",owner,"approve_stock_adjustment",new{adjustmentId=Ui.Text(below,"adjustmentId"),approve=true,reason="WinForms integration test should be rejected"},409);
            await Call(owner,"approve_stock_adjustment",new{adjustmentId=Ui.Text(below,"adjustmentId"),approve=false,reason="WinForms integration test invalid count rejected"});
            checks.Add(new{test="fixture-provenance",pass=true,marker,purchaseId,additionalUnreceivedPurchaseId=otherId,note="Separate API-created test POs remain on the clone for audit; no reset/delete/direct SQL."});
            return checks;
        }
        finally{await warehouse.LogoutAsync(token);}
    }
}
