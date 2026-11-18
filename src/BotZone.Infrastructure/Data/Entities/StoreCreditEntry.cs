using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class StoreCreditEntry
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public long DeltaVnd { get; set; }

    public string Kind { get; set; } = null!;

    public Guid? TradeInItemId { get; set; }

    public Guid? QuoteItemId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? RefundId { get; set; }

    public string EventKey { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual TradeInQuoteItem? QuoteItem { get; set; }

    public virtual Refund? Refund { get; set; }

    public virtual TradeInItem? TradeInItem { get; set; }

    public virtual StoreCreditAccount User { get; set; } = null!;
}
