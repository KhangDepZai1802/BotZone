using System.Globalization;
using BotZone.Infrastructure.Services;
using MailKit.Net.Smtp;
using MailKit;
using MailKit.Security;
using MimeKit;
namespace BotZone.Api;
public sealed class LocalWorker(IServiceScopeFactory scopes,IConfiguration config,ILogger<LocalWorker> logger):BackgroundService
{
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){while(!stoppingToken.IsCancellationRequested){try{using var scope=scopes.CreateScope();var sql=scope.ServiceProvider.GetRequiredService<SqlStore>();sql.Token=stoppingToken;await sql.Transaction<object>(null,async()=>{await scope.ServiceProvider.GetRequiredService<CommerceService>().Expire();await Earn(sql);await scope.ServiceProvider.GetRequiredService<AfterSalesService>().ExpireTradeInQuotes();return new{};});await Outbox(sql,stoppingToken);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception e){logger.LogError(e,"Local worker iteration failed; committed data is retained");}try{await Task.Delay(TimeSpan.FromSeconds(10),stoppingToken);}catch(OperationCanceledException){break;}}}
 static async Task Earn(SqlStore sql){var orders=await sql.Rows("SELECT o.* FROM orders o WHERE o.order_status='completed' AND o.delivered_at<now()-make_interval(days=>COALESCE((o.policy_snapshot->>'returnDays')::int,14)) AND NOT EXISTS(SELECT 1 FROM return_requests r WHERE r.order_id=o.id AND r.state NOT IN ('closed','rejected')) AND NOT EXISTS(SELECT 1 FROM loyalty_entries e WHERE e.order_id=o.id AND e.kind='earn') AND NOT EXISTS(SELECT 1 FROM app_settings s WHERE s.key='loyalty_settled:'||o.id::text) ORDER BY o.id FOR UPDATE");foreach(var order in orders){var totals=await sql.One("SELECT (SELECT COALESCE(sum(net_amount_vnd),0) FROM order_items WHERE order_id=$1 AND line_type='product')-(SELECT COALESCE(sum(cu.amount_vnd),0) FROM credit_note_item_units cu JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id JOIN credit_notes cn ON cn.id=ci.credit_note_id JOIN invoice_items ii ON ii.id=ci.invoice_item_id WHERE cn.order_id=$1 AND cn.status='issued' AND ii.line_type='product') net",order.G("id"));var policy=order.J("policy_snapshot");decimal factor=decimal.TryParse(policy.Text("loyaltyMultiplier","1"),NumberStyles.Number,CultureInfo.InvariantCulture,out var f)?f:1;int points=checked((int)decimal.Floor(totals.L("net")*factor/long.Parse(policy.Text("pointsPerVnd","100000"),CultureInfo.InvariantCulture)));await sql.Exec("INSERT INTO loyalty_accounts(user_id) VALUES($1) ON CONFLICT DO NOTHING",order.S("customer_id"));if(points>0)await sql.Exec("INSERT INTO loyalty_entries(user_id,delta,kind,order_id,event_key) VALUES($1,$2,'earn',$3,$4)",order.S("customer_id"),points,order.G("id"),"earn:"+order.G("id"));await sql.Exec("UPDATE loyalty_accounts SET balance_points=balance_points+$2,net_spend_vnd=net_spend_vnd+$3 WHERE user_id=$1",order.S("customer_id"),points,totals.L("net"));await sql.Exec("INSERT INTO app_settings(key,value) VALUES($1,'true') ON CONFLICT DO NOTHING","loyalty_settled:"+order.G("id"));}}
 async Task Outbox(SqlStore sql,CancellationToken ct)
 {
  for(int i=0;i<10;i++)
  {
   // Commit a bounded lease before contacting SMTP. A restarted worker reclaims expired leases.
   var row=await sql.Transaction<Dictionary<string,object?>?>(null,async()=>
   {
    await sql.Exec("UPDATE outbox_events SET status='failed' WHERE status='processing' AND next_attempt_at<=now() AND attempts>=5");
    var pending=await sql.Optional("SELECT * FROM outbox_events WHERE status IN ('pending','processing') AND next_attempt_at<=now() AND attempts<5 ORDER BY created_at,id LIMIT 1 FOR UPDATE SKIP LOCKED");
    if(pending is null)return null;
    return await sql.One("UPDATE outbox_events SET status='processing',attempts=attempts+1,next_attempt_at=now()+interval '2 minutes' WHERE id=$1 RETURNING *",pending.G("id"));
   },false);
   if(row is null)break;
   var eventId=row.G("id");int attempt=row.I("attempts");var payload=row.J("payload");
   try
   {
    if(row.S("type")=="email")
    {
     var message=new MimeMessage{Subject=payload.Text("subject"),MessageId=$"{eventId:N}@botzone.local"};
     message.From.Add(new MailboxAddress("BotZone",config["Smtp:From"]??"noreply@botzone.local"));
     message.To.Add(MailboxAddress.Parse(payload.Text("to")));message.Body=new TextPart("plain"){Text=payload.Text("text")};
     using var smtp=new SmtpClient{Timeout=15000};
     await smtp.ConnectAsync(config["Smtp:Host"]??"127.0.0.1",config.GetValue("Smtp:Port",1025),SecureSocketOptions.None,ct);
     await smtp.SendAsync(message,ct);await smtp.DisconnectAsync(true,ct);
    }
    else if(row.S("type")!="notification")throw new InvalidOperationException("Unknown outbox type.");
    await sql.Transaction<object>(null,async()=>
    {
     // A lease holder may only complete its own attempt. Notification ID equals event ID.
     var active=await sql.Optional("SELECT id FROM outbox_events WHERE id=$1 AND status='processing' AND attempts=$2 FOR UPDATE",eventId,attempt);
     if(active is null)return new{stale=true};
     if(row.S("type")=="notification")await sql.Exec("INSERT INTO notifications(id,recipient_user_id,type,message,entity_id) VALUES($1,$2,'order',$3,$4) ON CONFLICT(id) DO NOTHING",eventId,payload.Text("userId"),payload.Text("message"),payload.MaybeId("entityId"));
     await sql.Exec("UPDATE outbox_events SET status='completed' WHERE id=$1",eventId);return new{completed=true};
    },false);
   }
   catch(Exception e)when(e is not OperationCanceledException)
   {
    logger.LogWarning(e,"Outbox delivery failed {EventId}; retry is retained",eventId);
    await sql.Exec("UPDATE outbox_events SET status=CASE WHEN attempts>=5 THEN 'failed' ELSE 'pending' END,next_attempt_at=now()+make_interval(secs=>LEAST(3600,30*power(2,attempts-1)::int)) WHERE id=$1 AND status='processing' AND attempts=$2",eventId,attempt);
    break;
   }
  }
 }
}
