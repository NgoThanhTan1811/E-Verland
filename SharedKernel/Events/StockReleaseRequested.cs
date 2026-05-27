using System;

namespace SharedKernel.Events;

public sealed record StockReleaseRequested(
    Guid PaymentId,
    Guid OrderId,
    DateTime OccurredAt
);
