using System;
using System.Collections.Generic;

namespace SharedKernel.Events;

public sealed record StockReserveRequested(
    Guid PaymentId,
    Guid OrderId,
    List<(Guid SkuId, int Quantity)> Items,
    DateTime OccurredAt
);
