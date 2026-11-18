using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class PaymentEvent
{
    public Guid Id { get; set; }

    public Guid PaymentAttemptId { get; set; }

    public string EventKey { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string PayloadDigest { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual PaymentAttempt PaymentAttempt { get; set; } = null!;
}
