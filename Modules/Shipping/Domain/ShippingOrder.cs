using SharedKernel.Entities;

namespace Modules.Shipping.Domain;

public sealed class ShippingOrder : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }

    public string Provider { get; set; } = "GHN";
    public string? ProviderOrderCode { get; set; }
    public string? ClientOrderCode { get; set; }

    public ShippingStatus Status { get; set; } = ShippingStatus.Draft;
    public string? ProviderStatus { get; set; }

    public int? ServiceId { get; set; }
    public int? ServiceTypeId { get; set; }
    public int? PaymentTypeId { get; set; }

    public decimal CodAmount { get; set; }
    public decimal InsuranceValue { get; set; }

    public int Weight { get; set; }
    public int Length { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public DateTime? ExpectedDeliveryTime { get; set; }
    public decimal TotalFee { get; set; }
    public ShippingFeeSnapshot? FeeSnapshot { get; set; }

    public ShippingAddressSnapshot ToAddress { get; set; } = default!;
    public ShippingAddressSnapshot? FromAddress { get; set; }

    public List<ShippingItemSnapshot> Items { get; set; } = [];

    public string? Note { get; set; }
    public string? RequiredNote { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}
