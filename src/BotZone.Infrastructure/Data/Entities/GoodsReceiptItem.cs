using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class GoodsReceiptItem
{
    public Guid Id { get; set; }

    public Guid ReceiptId { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public Guid PurchaseOrderItemId { get; set; }

    public int ReceivedQty { get; set; }

    public long UnitCostVnd { get; set; }

    public virtual GoodsReceipt GoodsReceipt { get; set; } = null!;

    public virtual InventoryLot? InventoryLot { get; set; }

    public virtual PurchaseOrderItem PurchaseOrderItem { get; set; } = null!;
}
