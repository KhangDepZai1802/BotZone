using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class OutboxEvent
{
    public Guid Id { get; set; }

    public string EventKey { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int Attempts { get; set; }

    public DateTime NextAttemptAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
