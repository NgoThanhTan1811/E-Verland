namespace Infra.AWS.SNS;

/// <summary>
/// Interface for AWS SNS pub/sub operations
/// </summary>
public interface ISNSService
{
    /// <summary>
    /// Publish a message to a topic
    /// </summary>
    Task<string> PublishAsync<T>(string topicArn, T message, string? subject = null, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Send SMS message
    /// </summary>
    Task<string> SendSMSAsync(string phoneNumber, string message, CancellationToken ct = default);

    /// <summary>
    /// Subscribe an endpoint to a topic
    /// </summary>
    Task<string> SubscribeAsync(string topicArn, string protocol, string endpoint, CancellationToken ct = default);

    /// <summary>
    /// Unsubscribe from a topic
    /// </summary>
    Task UnsubscribeAsync(string subscriptionArn, CancellationToken ct = default);

    /// <summary>
    /// Create a new topic
    /// </summary>
    Task<string> CreateTopicAsync(string topicName, CancellationToken ct = default);
}
