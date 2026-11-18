using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class PurchaseOrder
{
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public string Code { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public string? ApprovedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

    public virtual Supplier Supplier { get; set; } = null!;
}
