using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class CreditNoteItemUnit
{
    public Guid CreditNoteItemId { get; set; }

    public Guid OrderItemUnitAmountId { get; set; }

    public long AmountVnd { get; set; }

    public virtual CreditNoteItem CreditNoteItem { get; set; } = null!;

    public virtual OrderItemUnitAmount OrderItemUnitAmount { get; set; } = null!;
}
