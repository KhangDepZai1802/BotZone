using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class AppSetting
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public int Version { get; set; }

    public virtual ICollection<SettingVersion> SettingVersions { get; set; } = new List<SettingVersion>();
}
