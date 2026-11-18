using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class TradeInQuoteItem
{
    public Guid Id { get; set; }

    public Guid QuoteId { get; set; }

    public Guid RequestId { get; set; }

    public Guid ItemId { get; set; }

    public long OfferedVnd { get; set; }

    public string Decision { get; set; } = null!;

    public DateTime? DecidedAt { get; set; }

    public virtual ICollection<StoreCreditEntry> StoreCreditEntries { get; set; } = new List<StoreCreditEntry>();

    public virtual TradeInItem TradeInItem { get; set; } = null!;

    public virtual TradeInQuote TradeInQuote { get; set; } = null!;
}
