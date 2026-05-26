namespace Modules.Shipping.Application.DTOs.Request;

public sealed record ShippingAddressRequestDto(
    string Name,
    string Phone,
    string Address,
    int DistrictId,
    string WardCode,
    string? WardName,
    string? DistrictName,
    string? ProvinceName
);

public sealed record ShippingDimensionsRequestDto(
    int Weight,
    int Length,
    int Width,
    int Height
);

public sealed record ShippingItemRequestDto(
    string Name,
    string? Code,
    int Quantity,
    int? Price,
    int Weight,
    int? Length,
    int? Width,
    int? Height
);

public sealed record CreateShippingDraftRequestDto(
    Guid OrderId,
    Guid UserId,
    string ClientOrderCode,
    ShippingAddressRequestDto ToAddress,
    ShippingDimensionsRequestDto Dimensions,
    List<ShippingItemRequestDto> Items,
    int? ServiceId,
    int? ServiceTypeId,
    int? PaymentTypeId,
    decimal? CodAmount,
    decimal? InsuranceValue,
    string? Note,
    string? RequiredNote
);

public sealed record CalculateShippingFeeRequestDto(
    int ToDistrictId,
    string ToWardCode,
    int? FromDistrictId,
    string? FromWardCode,
    int Weight,
    int? Length,
    int? Width,
    int? Height,
    int? ServiceId,
    int? ServiceTypeId,
    decimal? InsuranceValue,
    decimal? CodValue,
    decimal? CodFailedAmount,
    string? Coupon,
    List<ShippingItemRequestDto>? Items
);
