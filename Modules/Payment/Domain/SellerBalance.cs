using SharedKernel.Entities;

namespace Modules.Payment.Domain;

public enum SellerBalanceStatus
{
    Pending = 1,
    Available = 2,
    Reversed = 3
}

public sealed class SellerBalance : BaseEntity
{
    public Guid SellerId { get; set; }
    public Guid OrderId { get; set; }
    public string? PayoutId { get; set; }

    public decimal PendingAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public string Currency { get; set; } = "VND";

    public DateTime AvailableAtUtc { get; set; }
    public SellerBalanceStatus Status { get; set; } = SellerBalanceStatus.Pending;
}
