using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class PaymentAttempt
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Method { get; set; } = null!;

    public string Status { get; set; } = null!;

    public long RequestedAmountVnd { get; set; }

    public string RequestKey { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<PaymentEvent> PaymentEvents { get; set; } = new List<PaymentEvent>();

    public virtual ICollection<PaymentReceipt> PaymentReceipts { get; set; } = new List<PaymentReceipt>();
}
