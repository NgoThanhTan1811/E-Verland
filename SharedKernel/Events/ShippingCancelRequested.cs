using System.Text.Json.Serialization;

namespace SharedKernel.Events;

public sealed record ShippingCancelRequested(
    [property: JsonPropertyName("orderId")] Guid OrderId,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("eventType")] string EventType = "ShippingCancelRequested"
);
