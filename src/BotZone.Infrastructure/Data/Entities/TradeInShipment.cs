using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class TradeInShipment
{
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }

    public string Direction { get; set; } = null!;

    public string TrackingNo { get; set; } = null!;

    public long FeeVnd { get; set; }

    public string State { get; set; } = null!;

    public virtual TradeInRequest Request { get; set; } = null!;
}
