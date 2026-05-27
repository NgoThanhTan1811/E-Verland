using System.Text.Json.Serialization;

namespace SharedKernel.Events;

public sealed record ShippingActivationRequested(
    [property: JsonPropertyName("orderId")] Guid OrderId,
    [property: JsonPropertyName("eventType")] string EventType = "ShippingActivationRequested"
);
