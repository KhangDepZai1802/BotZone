using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class InventoryUnit
{
    public Guid Id { get; set; }

    public Guid LotId { get; set; }

    public string ConditionStatus { get; set; } = null!;

    public string CustodyStatus { get; set; } = null!;

    public virtual ICollection<InventoryAllocation> InventoryAllocations { get; set; } = new List<InventoryAllocation>();

    public virtual ICollection<InventoryIdentifier> InventoryIdentifiers { get; set; } = new List<InventoryIdentifier>();

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual ICollection<ReturnItemUnit> ReturnItemUnits { get; set; } = new List<ReturnItemUnit>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<TradeInItem> TradeInItems { get; set; } = new List<TradeInItem>();

    public virtual ICollection<WarrantyEntitlement> WarrantyEntitlements { get; set; } = new List<WarrantyEntitlement>();

    public virtual WarrantyReplacement? WarrantyReplacementNewUnit { get; set; }

    public virtual ICollection<WarrantyReplacement> WarrantyReplacementOldUnits { get; set; } = new List<WarrantyReplacement>();
}
