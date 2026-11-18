using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class GoodsReceipt
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public string ReceiptNo { get; set; } = null!;

    public string ReceivedBy { get; set; } = null!;

    public DateTime ReceivedAt { get; set; }

    public string EventKey { get; set; } = null!;

    public virtual ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    public virtual User ReceivedByNavigation { get; set; } = null!;
}
