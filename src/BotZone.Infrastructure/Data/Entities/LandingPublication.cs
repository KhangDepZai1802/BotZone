using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class LandingPublication
{
    public Guid Id { get; set; }

    public Guid PageId { get; set; }

    public Guid VersionId { get; set; }

    public string PublishedBy { get; set; } = null!;

    public DateTime PublishedAt { get; set; }

    public virtual LandingVersion LandingVersion { get; set; } = null!;

    public virtual User PublishedByNavigation { get; set; } = null!;
}
