using Modules.Order.Domain;

namespace Modules.Order.Application.DTOs.Response;

public sealed record CreateOrderResponseDto(
    Guid Id,
    string Code
);

public sealed record OrderOverviewResponseDto(
    Guid Id,
    string Code,
    Guid UserId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    PaymentMethod PaymentMethod,
    decimal TotalPrice,
    decimal? Discount,
    decimal GrandTotal,
    DateTime CreatedAt
);