using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Infra.AWS.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infra.AWS.EventBridge;

/// <summary>
/// AWS EventBridge service implementation
/// </summary>
public sealed class EventBridgeService : IEventBridgeService
{
    private readonly IAmazonEventBridge _eventBridgeClient;
    private readonly EventBridgeOptions _options;
    private readonly ILogger<EventBridgeService> _logger;

    public EventBridgeService(
        IAmazonEventBridge eventBridgeClient,
        IOptions<EventBridgeOptions> options,
        ILogger<EventBridgeService> logger)
    {
        _eventBridgeClient = eventBridgeClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> PutEventAsync<T>(string source, string detailType, T detail, CancellationToken ct = default) where T : class
    {
        try
        {
            var detailJson = JsonSerializer.Serialize(detail);

            var request = new PutEventsRequest
            {
                Entries =
                [
                    new() {
                        Source = source,
                        DetailType = detailType,
                        Detail = detailJson,
                        EventBusName = _options.EventBusName,
                        Time = DateTime.UtcNow
                    }
                ]
            };

            var response = await AwsRetryPolicy.ExecuteAsync(() => _eventBridgeClient.PutEventsAsync(request, ct), 3, ct);

            if (response.FailedEntryCount > 0)
            {
                var error = response.Entries.FirstOrDefault(e => e.ErrorCode != null);
                throw new Exception($"Failed to put event: {error?.ErrorCode} - {error?.ErrorMessage}");
            }

            var eventId = response.Entries.First().EventId;

            _logger.LogInformation(
                "Event published to EventBridge. Source: {Source}, DetailType: {DetailType}, EventId: {EventId}",
                source, detailType, eventId);

            return eventId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put event to EventBridge. Source: {Source}, DetailType: {DetailType}", source, detailType);
            throw;
        }
    }

    public async Task<List<string>> PutEventsBatchAsync(List<EventBridgeEvent> events, CancellationToken ct = default)
    {
        try
        {
            var entries = events.Select(e => new PutEventsRequestEntry
            {
                Source = e.Source,
                DetailType = e.DetailType,
                Detail = JsonSerializer.Serialize(e.Detail),
                EventBusName = _options.EventBusName,
                Time = DateTime.UtcNow
            }).ToList();

            var request = new PutEventsRequest
            {
                Entries = entries
            };

            var response = await AwsRetryPolicy.ExecuteAsync(() => _eventBridgeClient.PutEventsAsync(request, ct), 3, ct);

            if (response.FailedEntryCount > 0)
            {
                var failedEntries = response.Entries.Where(e => e.ErrorCode != null).ToList();
                _logger.LogWarning(
                    "Some events failed to publish. Failed: {FailedCount}, Successful: {SuccessfulCount}",
                    response.FailedEntryCount, events.Count - response.FailedEntryCount);
            }

            var eventIds = response.Entries
                .Where(e => e.EventId != null)
                .Select(e => e.EventId)
                .ToList();

            _logger.LogInformation(
                "Published {Count} events to EventBridge. Successful: {Successful}, Failed: {Failed}",
                events.Count, eventIds.Count, response.FailedEntryCount);

            return eventIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put event batch to EventBridge");
            throw;
        }
    }

    public async Task<string> CreateEventBusAsync(string eventBusName, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateEventBusRequest
            {
                Name = eventBusName
            };

            var response = await AwsRetryPolicy.ExecuteAsync(() => _eventBridgeClient.CreateEventBusAsync(request, ct), 3, ct);

            _logger.LogInformation("Created EventBridge event bus: {EventBusName}. ARN: {EventBusArn}",
                eventBusName, response.EventBusArn);

            return response.EventBusArn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create EventBridge event bus: {EventBusName}", eventBusName);
            throw;
        }
    }
}
