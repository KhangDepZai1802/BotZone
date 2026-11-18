using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class OrderTenderAllocation
{
    public Guid Id { get; set; }

    public Guid OrderItemUnitAmountId { get; set; }

    public Guid ReceiptId { get; set; }

    public long AmountVnd { get; set; }

    public virtual OrderItemUnitAmount OrderItemUnitAmount { get; set; } = null!;

    public virtual PaymentReceipt Receipt { get; set; } = null!;
}
