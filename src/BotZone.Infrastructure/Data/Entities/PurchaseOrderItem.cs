using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class PurchaseOrderItem
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public Guid VariantId { get; set; }

    public int Quantity { get; set; }

    public long UnitCostVnd { get; set; }

    public virtual ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
