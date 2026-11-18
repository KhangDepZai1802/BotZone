using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class AuditEvent
{
    public Guid Id { get; set; }

    public string? ActorUserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? BeforeData { get; set; }

    public string? AfterData { get; set; }

    public string? Reason { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? CorrelationId { get; set; }

    public virtual User? ActorUser { get; set; }
}
