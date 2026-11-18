using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class StockMovement
{
    public Guid Id { get; set; }

    public Guid VariantId { get; set; }

    public Guid LotId { get; set; }

    public Guid? UnitId { get; set; }

    public int QuantityDelta { get; set; }

    public string Kind { get; set; } = null!;

    public string SourceEventKey { get; set; } = null!;

    public string? ActorUserId { get; set; }

    public string Reason { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User? ActorUser { get; set; }

    public virtual InventoryLot InventoryLot { get; set; } = null!;

    public virtual InventoryUnit? InventoryUnit { get; set; }

    public virtual ProductVariant Variant { get; set; } = null!;
}
