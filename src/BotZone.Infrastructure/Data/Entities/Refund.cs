using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Refund
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid? CreditNoteId { get; set; }

    public string Status { get; set; } = null!;

    public long AmountVnd { get; set; }

    public string ApprovedBy { get; set; } = null!;

    public DateTime? CompletedAt { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    public virtual User ApprovedByNavigation { get; set; } = null!;

    public virtual CreditNote? CreditNote { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<RefundAllocation> RefundAllocations { get; set; } = new List<RefundAllocation>();

    public virtual ICollection<StoreCreditEntry> StoreCreditEntries { get; set; } = new List<StoreCreditEntry>();
}
