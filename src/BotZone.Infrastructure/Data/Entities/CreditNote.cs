using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class CreditNote
{
    public Guid Id { get; set; }

    public string CreditNoteNo { get; set; } = null!;

    public Guid InvoiceId { get; set; }

    public Guid OrderId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? IssuedAt { get; set; }

    public Guid? ReturnRequestId { get; set; }

    public Guid? WarrantyClaimId { get; set; }

    public Guid? ShipmentId { get; set; }

    public string Reason { get; set; } = null!;

    public long TotalVnd { get; set; }

    public string ApprovedByUserId { get; set; } = null!;

    public string EventKey { get; set; } = null!;

    public virtual User ApprovedByUser { get; set; } = null!;

    public virtual ICollection<CreditNoteItem> CreditNoteItems { get; set; } = new List<CreditNoteItem>();

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual ReturnRequest? ReturnRequest { get; set; }

    public virtual Shipment? Shipment { get; set; }

    public virtual WarrantyClaim? WarrantyClaim { get; set; }
}
