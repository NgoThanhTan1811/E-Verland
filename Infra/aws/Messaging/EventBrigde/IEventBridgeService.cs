namespace Infra.AWS.EventBridge;

/// <summary>
/// Interface for AWS EventBridge operations
/// </summary>
public interface IEventBridgeService
{
    /// <summary>
    /// Put a single event to EventBridge
    /// </summary>
    Task<string> PutEventAsync<T>(string source, string detailType, T detail, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Put multiple events in a batch
    /// </summary>
    Task<List<string>> PutEventsBatchAsync(List<EventBridgeEvent> events, CancellationToken ct = default);

    /// <summary>
    /// Create a custom event bus
    /// </summary>
    Task<string> CreateEventBusAsync(string eventBusName, CancellationToken ct = default);
}

/// <summary>
/// EventBridge event wrapper
/// </summary>
public sealed record EventBridgeEvent(
    string Source,
    string DetailType,
    object Detail
);
