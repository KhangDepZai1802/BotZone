using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class VariantStock
{
    public Guid? VariantId { get; set; }

    public long? Available { get; set; }
}
