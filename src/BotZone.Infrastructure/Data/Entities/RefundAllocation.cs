using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class RefundAllocation
{
    public Guid Id { get; set; }

    public Guid RefundId { get; set; }

    public Guid PaymentReceiptId { get; set; }

    public Guid OrderItemUnitAmountId { get; set; }

    public long AmountVnd { get; set; }

    public virtual OrderItemUnitAmount OrderItemUnitAmount { get; set; } = null!;

    public virtual PaymentReceipt PaymentReceipt { get; set; } = null!;

    public virtual Refund Refund { get; set; } = null!;
}
