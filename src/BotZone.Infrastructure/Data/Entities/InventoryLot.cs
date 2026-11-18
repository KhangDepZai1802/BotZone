using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class InventoryLot
{
    public Guid Id { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid VariantId { get; set; }

    public Guid ReceiptItemId { get; set; }

    public DateTime ReceivedAt { get; set; }

    public long UnitCostVnd { get; set; }

    public int QtyOnHand { get; set; }

    public int QtyReserved { get; set; }

    public virtual ICollection<InventoryAllocation> InventoryAllocations { get; set; } = new List<InventoryAllocation>();

    public virtual ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();

    public virtual GoodsReceiptItem ReceiptItem { get; set; } = null!;

    public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ProductVariant Variant { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
