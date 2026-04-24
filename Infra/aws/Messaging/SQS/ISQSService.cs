namespace Infra.AWS.SQS;

/// <summary>
/// Interface for AWS SQS message queue operations
/// </summary>
public interface ISQSService
{
    /// <summary>
    /// Send a message to a queue
    /// </summary>
    Task<string> SendMessageAsync<T>(string queueUrl, T message, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Send multiple messages in a batch
    /// </summary>
    Task<List<string>> SendMessageBatchAsync<T>(string queueUrl, List<T> messages, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Receive messages from a queue
    /// </summary>
    Task<List<SQSMessage<T>>> ReceiveMessagesAsync<T>(string queueUrl, int maxMessages = 10, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Delete a message after processing
    /// </summary>
    Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken ct = default);

    /// <summary>
    /// Delete multiple messages in a batch
    /// </summary>
    Task DeleteMessageBatchAsync(string queueUrl, List<string> receiptHandles, CancellationToken ct = default);
}

/// <summary>
/// SQS message wrapper
/// </summary>
public sealed record SQSMessage<T>(
    string MessageId,
    string ReceiptHandle,
    T Body,
    int ReceiveCount,
    DateTime SentTimestamp
) where T : class;
