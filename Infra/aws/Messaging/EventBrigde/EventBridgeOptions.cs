namespace Infra.AWS.EventBridge;

/// <summary>
/// AWS EventBridge configuration options
/// </summary>
public sealed class EventBridgeOptions
{
    public const string SectionName = "AWS:EventBridge";

    public string Region { get; set; } = "us-east-1";

    // Event Bus
    public string EventBusName { get; set; } = "e-verland-events";

    // Event sources
    public string OrderEventSource { get; set; } = "e-verland.orders";
    public string PaymentEventSource { get; set; } = "e-verland.payments";
    public string ProductEventSource { get; set; } = "e-verland.products";
    public string UserEventSource { get; set; } = "e-verland.users";
}
