using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class PaymentReceipt
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid? PaymentAttemptId { get; set; }

    public string TenderKind { get; set; } = null!;

    public long AmountVnd { get; set; }

    public DateTime ReceivedAt { get; set; }

    public string SourceEventKey { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<OrderTenderAllocation> OrderTenderAllocations { get; set; } = new List<OrderTenderAllocation>();

    public virtual PaymentAttempt? PaymentAttempt { get; set; }

    public virtual ICollection<RefundAllocation> RefundAllocations { get; set; } = new List<RefundAllocation>();
}
