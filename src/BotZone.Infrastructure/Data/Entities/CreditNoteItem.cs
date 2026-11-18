using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class CreditNoteItem
{
    public Guid Id { get; set; }

    public Guid CreditNoteId { get; set; }

    public Guid InvoiceItemId { get; set; }

    public int? Quantity { get; set; }

    public long AmountVnd { get; set; }

    public virtual CreditNote CreditNote { get; set; } = null!;

    public virtual ICollection<CreditNoteItemUnit> CreditNoteItemUnits { get; set; } = new List<CreditNoteItemUnit>();

    public virtual InvoiceItem InvoiceItem { get; set; } = null!;
}
