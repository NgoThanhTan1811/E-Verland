using Modules.Payment.Domain;

namespace Modules.Payment.Application.DTOs.Request;

public sealed record InitiatePaymentRequestDto(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    PaymentMethod Method,
    List<OrderItemRequestDto> Items
);

public sealed record OrderItemRequestDto(Guid SkuId, int Quantity);

public sealed record UpdatePaymentStatusRequestDto(
    PaymentStatus Status
);
