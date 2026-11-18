using System.Data;
using BotZone.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BotZone.Infrastructure.Services;

public sealed class CustomerReadService(SqlStore sql)
{
    public async Task<object> Detail(Actor actor, string userId)
    {
        Access.Require(actor, "owner", "order_staff");
        Access.Ensure(userId.Length is >= 1 and <= 200, "VALIDATION_ERROR", "Mã khách hàng không hợp lệ.", 400);
        await using var tx = await sql.Db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, sql.Token);
        await sql.Exec("SET TRANSACTION READ ONLY");
        var customer = await sql.One("SELECT c.user_id id,c.display_name,c.phone,u.email,u.email_verified FROM customer_profiles c JOIN \"user\" u ON u.id=c.user_id WHERE c.user_id=$1", userId);
        var balances = await sql.One("""
            SELECT COALESCE(sc.balance_vnd,0)::text credit_vnd,COALESCE(la.balance_points,0) balance_points,COALESCE(la.net_spend_vnd,0)::text net_spend_vnd,
              CASE WHEN COALESCE(la.net_spend_vnd,0)>=100000000 THEN 'Platinum' WHEN COALESCE(la.net_spend_vnd,0)>=50000000 THEN 'Gold' WHEN COALESCE(la.net_spend_vnd,0)>=20000000 THEN 'Silver' ELSE 'Member' END tier,
              (SELECT COALESCE(sum(amount),0)::text FROM benefit_holds WHERE customer_id=$1 AND kind='store_credit' AND status='active') held_credit_vnd,
              (SELECT COALESCE(sum(amount),0)::int FROM benefit_holds WHERE customer_id=$1 AND kind='points' AND status='active') held_points,
              (SELECT count(*)::int FROM orders WHERE customer_id=$1 AND order_status<>'building') order_count
            FROM "user" u LEFT JOIN store_credit_accounts sc ON sc.user_id=u.id LEFT JOIN loyalty_accounts la ON la.user_id=u.id WHERE u.id=$1
            """, userId);
        var addresses = await sql.Rows("SELECT id,recipient,phone,province,ward,address_line,is_default FROM addresses WHERE user_id=$1 ORDER BY is_default DESC,id", userId);
        var orders = await sql.Rows("SELECT id,order_no,order_status,payment_method,total_vnd,created_at,responsible_staff_id FROM orders WHERE customer_id=$1 AND order_status<>'building' ORDER BY created_at DESC,id LIMIT 200", userId);
        var creditEntries = await sql.Rows("SELECT id,delta_vnd,kind,order_id,trade_in_item_id,refund_id,event_key,created_at FROM store_credit_entries WHERE user_id=$1 ORDER BY created_at DESC,id LIMIT 200", userId);
        var loyaltyEntries = await sql.Rows("SELECT id,delta,kind,order_id,unit_id,event_key,created_at FROM loyalty_entries WHERE user_id=$1 ORDER BY created_at DESC,id LIMIT 200", userId);
        var returns = await sql.Rows("SELECT id,order_id,state,reason,created_at FROM return_requests WHERE customer_id=$1 ORDER BY created_at DESC,id LIMIT 200", userId);
        var tradeIns = await sql.Rows("SELECT id,reception_method,state,created_at FROM trade_in_requests WHERE customer_id=$1 ORDER BY created_at DESC,id LIMIT 200", userId);
        var claims = await sql.Rows("""
            SELECT c.id,c.state,c.damage_kind,c.description,c.created_at,e.starts_on,e.ends_on,i.order_id,i.variant_id
            FROM warranty_claims c JOIN warranty_entitlements e ON e.id=c.entitlement_id
            JOIN order_item_unit_amounts u ON u.id=e.order_item_unit_amount_id JOIN order_items i ON i.id=u.order_item_id
            JOIN orders o ON o.id=i.order_id WHERE o.customer_id=$1 ORDER BY c.created_at DESC,c.id LIMIT 200
            """, userId);
        var readAt = (await sql.One("SELECT transaction_timestamp() read_at"))["read_at"];
        await tx.CommitAsync(sql.Token);
        return new { customer, balances, addresses, orders, creditEntries, loyaltyEntries, returns, tradeIns, claims, readAt, historyLimit = 200 };
    }
}
