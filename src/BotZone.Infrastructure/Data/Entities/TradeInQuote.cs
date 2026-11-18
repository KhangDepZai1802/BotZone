using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class TradeInQuote
{
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }

    public int VersionNo { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string QuotedBy { get; set; } = null!;

    public string? ApprovedBy { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User QuotedByNavigation { get; set; } = null!;

    public virtual TradeInRequest Request { get; set; } = null!;

    public virtual ICollection<TradeInQuoteItem> TradeInQuoteItems { get; set; } = new List<TradeInQuoteItem>();
}
