using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class InvoiceItem
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public Guid OrderId { get; set; }

    public Guid OrderItemId { get; set; }

    public int LineNo { get; set; }

    public string LineType { get; set; } = null!;

    public Guid? ProductId { get; set; }

    public Guid? VariantId { get; set; }

    public Guid? ParentInvoiceItemId { get; set; }

    public string SkuSnapshot { get; set; } = null!;

    public string NameSnapshot { get; set; } = null!;

    public string VariantSnapshot { get; set; } = null!;

    public int Quantity { get; set; }

    public long UnitPriceVnd { get; set; }

    public long VoucherDiscountVnd { get; set; }

    public long PointsDiscountVnd { get; set; }

    public long NetAmountVnd { get; set; }

    public virtual ICollection<CreditNoteItem> CreditNoteItems { get; set; } = new List<CreditNoteItem>();

    public virtual ICollection<InvoiceItem> InverseInvoiceItemNavigation { get; set; } = new List<InvoiceItem>();

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual InvoiceItem? InvoiceItemNavigation { get; set; }

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual Product? Product { get; set; }

    public virtual ProductVariant? ProductVariant { get; set; }
}
