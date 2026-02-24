using Modules.Order.Domain;

namespace Modules.Order.Application.DTOs.Request;

public sealed record CreateOrderRequestDto(
    ReceiverRequestDto Receiver,
    PaymentMethod PaymentMethod,
    decimal? VoucherCode,
    List<CreateOrderItemRequestDto> Items
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