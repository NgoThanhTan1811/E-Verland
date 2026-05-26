using Modules.Order.Domain;

namespace Modules.Order.Application.DTOs.Request;

public sealed record CreateOrderRequestDto(
    ShippingAddressRequestDto ShippingAddress,
    ReceiverRequestDto Receiver,
    int Weight,
    int Length,
    int Width,
    int Height,
    int? ServiceId,
    int? ServiceTypeId,
    decimal? InsuranceValue,
    string? Note,
    string? RequiredNote,
    PaymentMethod PaymentMethod,
    decimal? VoucherCode,
    List<CreateOrderItemRequestDto> Items
);

public sealed record ShippingAddressRequestDto(
    string Address,
    int DistrictId,
    string WardCode,
    string? WardName,
    string? DistrictName,
    string? ProvinceName
);

public sealed record FilterOrdersAdminRequestDto(
    Guid? UserId,
    OrderStatus? Status,
    PaymentStatus? PaymentStatus,
    PaymentMethod? PaymentMethod,
    DateTime? FromDate,
    DateTime? ToDate,
    int? Page,
    int? Limit
);

public sealed record FilterOrdersUserRequestDto(
    OrderStatus? Status,
    PaymentStatus? PaymentStatus,
    DateTime? FromDate,
    DateTime? ToDate,
    int? Page,
    int? Limit
);