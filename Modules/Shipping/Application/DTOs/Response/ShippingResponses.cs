using Modules.Shipping.Domain;

namespace Modules.Shipping.Application.DTOs.Response;

public sealed record ShippingOrderResponseDto(
    Guid Id,
    Guid OrderId,
    string Provider,
    string? ProviderOrderCode,
    string? ClientOrderCode,
    ShippingStatus Status,
    string? ProviderStatus,
    decimal TotalFee,
    DateTime? ExpectedDeliveryTime,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record ShippingFeeResponseDto(
    decimal Total,
    decimal ServiceFee,
    decimal InsuranceFee,
    decimal PickStationFee,
    decimal CouponValue,
    decimal R2SFee,
    decimal DocumentReturn,
    decimal DoubleCheck,
    decimal CodFee,
    decimal PickRemoteAreasFee,
    decimal DeliverRemoteAreasFee,
    decimal CodFailedFee
);

public sealed record ShippingServiceResponseDto(
    int ServiceId,
    string? ShortName,
    int ServiceTypeId
);
