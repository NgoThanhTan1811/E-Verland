using Modules.Payment.Domain;

namespace Modules.Payment.Application.DTOs.Request;

public sealed record CreatePaymentRequestDto(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    PaymentMethod Method
);

public sealed record UpdatePaymentStatusRequestDto(
    PaymentStatus Status
);

public sealed record ProcessPaymentRequestDto(
    Guid OrderId,
    PaymentMethod Method
);
