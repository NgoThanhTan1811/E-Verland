using System;

namespace SharedKernel.Events;

public sealed record PaymentStatusChanged(
    Guid PaymentId,
    Guid OrderId,
    string NewStatus,
    string? PreviousStatus,
    DateTime OccurredAt
);
