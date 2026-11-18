using System.Text.Json;
namespace BotZone.Infrastructure.Services;

public sealed partial class CommandService
{
    // Audit snapshots are server-selected fields; never store passwords, cookies or preview codes.
    async Task<Dictionary<string,object?>?> BeforeCommand(string command, JsonElement input)
    {
        if (command is "save_settings" or "update_settings")
            return await sql.Optional("SELECT key,value,version FROM app_settings WHERE key=$1", input.Text("key"));
        if (command == "update_staff")
            return await sql.Optional("SELECT user_id,staff_code,role,active FROM staff_profiles WHERE user_id=$1", input.Text("userId"));
        if (command is "save_landing" or "publish_landing" or "restore_landing")
            return await sql.Optional("SELECT d.page_id,d.revision,d.sections FROM landing_drafts d JOIN landing_pages p ON p.id=d.page_id WHERE p.slug='home'");
        if (command == "update_variant")
            return await sql.Optional("SELECT id,sku,price_vnd,care_fee_vnd,standard_warranty_months,active FROM product_variants WHERE id=$1", input.Id("variantId"));
        if (command == "update_product_status")
            return await sql.Optional("SELECT id,model_code,business_status FROM products WHERE id=$1", input.Id("productId"));
        if (input.MaybeId("orderId") is Guid order)
            return await sql.Optional("SELECT id,order_no,order_status,payment_method,responsible_staff_id,delivered_at FROM orders WHERE id=$1", order);
        if (input.MaybeId("attemptId") is Guid attempt)
            return await sql.Optional("SELECT id,order_id,method,status,requested_amount_vnd FROM payment_attempts WHERE id=$1", attempt);
        if (input.MaybeId("claimId") is Guid claim)
            return await sql.Optional("SELECT id,entitlement_id,state,damage_kind,inspection_result FROM warranty_claims WHERE id=$1", claim);
        return null;
    }
}
