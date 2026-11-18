using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class StockAdjustment
{
    public Guid Id { get; set; }

    public Guid LotId { get; set; }

    public int QuantityDelta { get; set; }

    public string Reason { get; set; } = null!;

    public string RequestedBy { get; set; } = null!;

    public string? ApprovedBy { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual User RequestedByNavigation { get; set; } = null!;
}
