using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Shipment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string TrackingNo { get; set; } = null!;

    public string Status { get; set; } = null!;

    public Guid? RetryOf { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();

    public virtual ICollection<Shipment> InverseRetryOfNavigation { get; set; } = new List<Shipment>();

    public virtual Order Order { get; set; } = null!;

    public virtual Shipment? RetryOfNavigation { get; set; }

    public virtual ICollection<ShipmentEvent> ShipmentEvents { get; set; } = new List<ShipmentEvent>();
}
