using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class OrderItemUnitAmount
{
    public Guid Id { get; set; }

    public Guid OrderItemId { get; set; }

    public int UnitOrdinal { get; set; }

    public Guid? ParentUnitAmountId { get; set; }

    public long GrossVnd { get; set; }

    public long VoucherDiscountVnd { get; set; }

    public int RedeemedPoints { get; set; }

    public long PointValueVnd {get;set;} = 1000;

    public long PointsDiscountVnd { get; set; }

    public long NetAmountVnd { get; set; }

    public long CreditPlannedVnd { get; set; }

    public long CashPlannedVnd { get; set; }

    public virtual ICollection<CreditNoteItemUnit> CreditNoteItemUnits { get; set; } = new List<CreditNoteItemUnit>();

    public virtual ICollection<InventoryAllocation> InventoryAllocations { get; set; } = new List<InventoryAllocation>();

    public virtual ICollection<OrderItemUnitAmount> InverseParentUnitAmount { get; set; } = new List<OrderItemUnitAmount>();

    public virtual ICollection<LoyaltyEntry> LoyaltyEntries { get; set; } = new List<LoyaltyEntry>();

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual ICollection<OrderTenderAllocation> OrderTenderAllocations { get; set; } = new List<OrderTenderAllocation>();

    public virtual OrderItemUnitAmount? ParentUnitAmount { get; set; }

    public virtual ICollection<RefundAllocation> RefundAllocations { get; set; } = new List<RefundAllocation>();

    public virtual ICollection<ReturnItemUnit> ReturnItemUnits { get; set; } = new List<ReturnItemUnit>();

    public virtual WarrantyEntitlement? WarrantyEntitlement { get; set; }
}
