namespace Modules.Order.Application.DTOs.Request;

public sealed record CreateOrderItemRequestDto(
    Guid ProductId,
    Guid SkuId,
    int Quantity
);