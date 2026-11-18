using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class TradeInRequest
{
    public Guid Id { get; set; }

    public string CustomerId { get; set; } = null!;

    public string ReceptionMethod { get; set; } = null!;

    public string State { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual ICollection<TradeInItem> TradeInItems { get; set; } = new List<TradeInItem>();

    public virtual ICollection<TradeInQuote> TradeInQuotes { get; set; } = new List<TradeInQuote>();

    public virtual ICollection<TradeInShipment> TradeInShipments { get; set; } = new List<TradeInShipment>();
}
