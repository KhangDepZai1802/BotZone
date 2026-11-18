using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public int LineNo { get; set; }

    public string LineType { get; set; } = null!;

    public Guid? ProductId { get; set; }

    public Guid? VariantId { get; set; }

    public Guid? ParentItemId { get; set; }

    public int Quantity { get; set; }

    public long UnitPriceVnd { get; set; }

    public long VoucherDiscountVnd { get; set; }

    public long PointsDiscountVnd { get; set; }

    public long NetAmountVnd { get; set; }

    public string Snapshot { get; set; } = null!;

    public virtual ICollection<InventoryAllocation> InventoryAllocations { get; set; } = new List<InventoryAllocation>();

    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public virtual ICollection<OrderItem> InverseOrderItemNavigation { get; set; } = new List<OrderItem>();

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<OrderAdjustment> OrderAdjustments { get; set; } = new List<OrderAdjustment>();

    public virtual OrderItem? OrderItemNavigation { get; set; }

    public virtual ICollection<OrderItemUnitAmount> OrderItemUnitAmounts { get; set; } = new List<OrderItemUnitAmount>();

    public virtual Product? Product { get; set; }

    public virtual ProductVariant? ProductVariant { get; set; }

    public virtual ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();
}
