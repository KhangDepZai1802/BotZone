using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class ShipmentEvent
{
    public Guid Id { get; set; }

    public Guid ShipmentId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime OccurredAt { get; set; }

    public string EventKey { get; set; } = null!;

    public virtual Shipment Shipment { get; set; } = null!;
}
