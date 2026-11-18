using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class TradeInItem
{
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }

    public Guid ProductId { get; set; }

    public Guid? VariantId { get; set; }

    public int Qty { get; set; }

    public string? Identifier { get; set; }

    public string InternalCode { get; set; } = null!;

    public string ConditionSnapshot { get; set; } = null!;

    public string State { get; set; } = null!;

    public Guid? InventoryUnitId { get; set; }

    public virtual InventoryUnit? InventoryUnit { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ProductVariant? ProductVariant { get; set; }

    public virtual TradeInRequest Request { get; set; } = null!;

    public virtual StoreCreditEntry? StoreCreditEntry { get; set; }

    public virtual ICollection<TradeInInspection> TradeInInspections { get; set; } = new List<TradeInInspection>();

    public virtual ICollection<TradeInQuoteItem> TradeInQuoteItems { get; set; } = new List<TradeInQuoteItem>();
}
