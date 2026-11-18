from pathlib import Path
p=Path(__file__).resolve().parent.parent/'src/BotZone.Api/LocalWorker.cs'
s=p.read_text(encoding='utf-8')
start=s.index(' async Task Outbox(')
s=s[:start]+''' async Task Outbox(SqlStore sql,CancellationToken ct)
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
'''
p.write_text(s,encoding='utf-8')
