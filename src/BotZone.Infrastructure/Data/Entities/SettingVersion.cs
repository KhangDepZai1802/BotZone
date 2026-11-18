using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class SettingVersion
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public int VersionNo { get; set; }

    public string Value { get; set; } = null!;

    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual User? ChangedByNavigation { get; set; }

    public virtual AppSetting KeyNavigation { get; set; } = null!;
}
