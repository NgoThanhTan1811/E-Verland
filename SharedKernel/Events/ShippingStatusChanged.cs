using System.Text.Json.Serialization;

namespace SharedKernel.Events;

public sealed record ShippingStatusChanged(
    [property: JsonPropertyName("orderId")] Guid OrderId,
    [property: JsonPropertyName("providerOrderCode")] string? ProviderOrderCode,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("occurredAtUtc")] DateTime OccurredAtUtc,
    [property: JsonPropertyName("eventType")] string EventType = "ShippingStatusChanged"
);
