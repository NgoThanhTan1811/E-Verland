using System;

namespace SharedKernel.Events;

public sealed record StockConfirmRequested(
    Guid PaymentId,
    Guid OrderId,
    DateTime OccurredAt
);
