namespace Modules.Shipping.Domain;

public sealed record ShippingAddressSnapshot(
    string Name,
    string Phone,
    string Address,
    int? DistrictId,
    string? WardCode,
    string? WardName,
    string? DistrictName,
    string? ProvinceName
);

public sealed record ShippingItemSnapshot(
    string Name,
    string? Code,
    int Quantity,
    int Price,
    int Weight,
    int? Length,
    int? Width,
    int? Height
);

public sealed record ShippingFeeSnapshot(
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
