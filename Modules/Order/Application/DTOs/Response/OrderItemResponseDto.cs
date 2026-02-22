using Modules.Order.Domain;

namespace Modules.Order.Application.DTOs.Response;

public sealed record OrderItemResponseDto(
    Guid Id,
    Guid ProductId,
    Guid SkuId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);

public sealed record OrderDetailResponseDto(
    Guid Id,
    string Code,
    Guid UserId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    PaymentMethod PaymentMethod,
    Guid? PaymentId,
    ReceiverResponseDto Receiver,
    decimal TotalPrice,
    decimal? Discount,
    decimal GrandTotal,
    List<OrderItemResponseDto> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);