namespace Modules.Order.Application.DTOs.Events;

/// <summary>
/// Event published when an order is canceled
/// Product Module subscribes to this event to release stock reservations
/// </summary>
public sealed record OrderCanceledEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid? PaymentId { get; init; }
    public string OrderCode { get; init; } = default!;
    public decimal TotalPrice { get; init; }
    public DateTime CanceledAtUtc { get; init; }
}
