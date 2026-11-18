using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Notification
{
    public Guid Id { get; set; }

    public string RecipientUserId { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Message { get; set; } = null!;

    public Guid? EntityId { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User RecipientUser { get; set; } = null!;
}
