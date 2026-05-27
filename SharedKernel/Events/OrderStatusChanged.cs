using System;

namespace SharedKernel.Events;

public sealed record OrderStatusChanged(
    Guid OrderId,
    string NewStatus,
    string? PreviousStatus,
    DateTime OccurredAt
);
