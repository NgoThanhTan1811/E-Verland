using Modules.Payment.Domain;

namespace Modules.Payment.Application.DTOs.Response;

public sealed record PaymentResponseDto(
    Guid Id,
    string Code,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record CreatePaymentResponseDto(
    Guid Id,
    string Code,
    PaymentStatus Status
);

public sealed record PaymentOverviewResponseDto(
    Guid Id,
    string Code,
    Guid OrderId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime CreatedAt
);
