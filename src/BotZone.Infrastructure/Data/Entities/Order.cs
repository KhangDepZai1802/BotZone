using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Order
{
    public Guid Id { get; set; }

    public string OrderNo { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string? ResponsibleStaffId { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string AddressSnapshot { get; set; } = null!;

    public long TotalVnd { get; set; }

    public string Currency { get; set; } = null!;

    public decimal ExchangeRateVnd { get; set; }

    public string PolicySnapshot { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public virtual ICollection<BenefitHold> BenefitHolds { get; set; } = new List<BenefitHold>();

    public virtual User Customer { get; set; } = null!;

    public virtual Invoice? InvoiceOrder { get; set; }

    public virtual ICollection<Invoice> InvoiceOrderNavigations { get; set; } = new List<Invoice>();

    public virtual ICollection<LoyaltyEntry> LoyaltyEntries { get; set; } = new List<LoyaltyEntry>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();

    public virtual ICollection<PaymentReceipt> PaymentReceipts { get; set; } = new List<PaymentReceipt>();

    public virtual ICollection<PromotionRedemption> PromotionRedemptions { get; set; } = new List<PromotionRedemption>();

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual StaffProfile? ResponsibleStaff { get; set; }

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    public virtual ICollection<StoreCreditEntry> StoreCreditEntries { get; set; } = new List<StoreCreditEntry>();
}
