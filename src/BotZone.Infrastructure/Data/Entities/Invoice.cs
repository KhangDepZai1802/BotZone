using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Invoice
{
    public Guid Id { get; set; }

    public string? InvoiceNo { get; set; }

    public Guid OrderId { get; set; }

    public string Lifecycle { get; set; } = null!;

    public DateTime? IssuedAt { get; set; }

    public string CustomerId { get; set; } = null!;

    public string? ResponsibleStaffId { get; set; }

    public string? IssuedByUserId { get; set; }

    public string Currency { get; set; } = null!;

    public long SubtotalVnd { get; set; }

    public long DiscountTotalVnd { get; set; }

    public long GrandTotalVnd { get; set; }

    public string CustomerSnapshot { get; set; } = null!;

    public string StaffSnapshot { get; set; } = null!;

    public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();

    public virtual User Customer { get; set; } = null!;

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual User? IssuedByUser { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Order OrderNavigation { get; set; } = null!;

    public virtual StaffProfile? ResponsibleStaff { get; set; }
}
