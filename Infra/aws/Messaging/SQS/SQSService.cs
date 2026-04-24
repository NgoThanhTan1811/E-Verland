using Amazon.SQS;
using Amazon.SQS.Model;
using Infra.AWS.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infra.AWS.SQS;

/// <summary>
/// AWS SQS service implementation
/// </summary>
public sealed class SQSService : ISQSService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly SQSOptions _options;
    private readonly ILogger<SQSService> _logger;

    public SQSService(
        IAmazonSQS sqsClient,
        IOptions<SQSOptions> options,
        ILogger<SQSService> logger)
    {
        _sqsClient = sqsClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SendMessageAsync<T>(string queueUrl, T message, CancellationToken ct = default) where T : class
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(message);

            var request = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = messageBody
            };

            var response = await AwsRetryPolicy.ExecuteAsync(() => _sqsClient.SendMessageAsync(request, ct), 3, ct);

            _logger.LogInformation(
                "Message sent to SQS queue {QueueUrl}. MessageId: {MessageId}",
                queueUrl, response.MessageId);

            return response.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to SQS queue {QueueUrl}", queueUrl);
            throw;
        }
    }

    public async Task<List<string>> SendMessageBatchAsync<T>(string queueUrl, List<T> messages, CancellationToken ct = default) where T : class
    {
        try
        {
            var entries = messages.Select((msg, index) => new SendMessageBatchRequestEntry
            {
                Id = index.ToString(),
                MessageBody = JsonSerializer.Serialize(msg)
            }).ToList();

            var request = new SendMessageBatchRequest
            {
                QueueUrl = queueUrl,
                Entries = entries
            };

            var response = await AwsRetryPolicy.ExecuteAsync(() => _sqsClient.SendMessageBatchAsync(request, ct), 3, ct);

            _logger.LogInformation(
                "Sent {Count} messages to SQS queue {QueueUrl}. Successful: {Successful}, Failed: {Failed}",
                messages.Count, queueUrl, response.Successful.Count, response.Failed.Count);

            return response.Successful.Select(s => s.MessageId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message batch to SQS queue {QueueUrl}", queueUrl);
            throw;
        }
    }

    public async Task<List<SQSMessage<T>>> ReceiveMessagesAsync<T>(string queueUrl, int maxMessages = 10, CancellationToken ct = default) where T : class
    {
        try
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = Math.Min(maxMessages, 10),
                WaitTimeSeconds = _options.WaitTimeSeconds,
                MessageSystemAttributeNames = ["All"]
            };

            var response = await AwsRetryPolicy.ExecuteAsync(() => _sqsClient.ReceiveMessageAsync(request, ct), 3, ct);

            var messages = response.Messages.Select(msg =>
            {
                var body = JsonSerializer.Deserialize<T>(msg.Body)!;
                var receiveCount = int.TryParse(msg.Attributes.GetValueOrDefault("ApproximateReceiveCount"), out var rc) ? rc : 1;
                var sentTimestampRaw = msg.Attributes.GetValueOrDefault("SentTimestamp");
                var sentTimestamp = long.TryParse(sentTimestampRaw, out var ts)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(ts).DateTime
                    : DateTime.UtcNow;

                return new SQSMessage<T>(
                    msg.MessageId,
                    msg.ReceiptHandle,
                    body,
                    receiveCount,
                    sentTimestamp
                );
            }).ToList();

            _logger.LogDebug(
                "Received {Count} messages from SQS queue {QueueUrl}",
                messages.Count, queueUrl);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to receive messages from SQS queue {QueueUrl}", queueUrl);
            throw;
        }
    }

    public async Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken ct = default)
    {
        try
        {
            var request = new DeleteMessageRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = receiptHandle
            };

            await AwsRetryPolicy.ExecuteAsync(() => _sqsClient.DeleteMessageAsync(request, ct), 3, ct);

            _logger.LogDebug("Deleted message from SQS queue {QueueUrl}", queueUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message from SQS queue {QueueUrl}", queueUrl);
            throw;
        }
    }

    public async Task DeleteMessageBatchAsync(string queueUrl, List<string> receiptHandles, CancellationToken ct = default)
    {
        try
        {
            var entries = receiptHandles.Select((handle, index) => new DeleteMessageBatchRequestEntry
            {
                Id = index.ToString(),
                ReceiptHandle = handle
            }).ToList();

            var request = new DeleteMessageBatchRequest
            {
                QueueUrl = queueUrl,
                Entries = entries
            };

            var response = await AwsRetryPolicy.ExecuteAsync(() => _sqsClient.DeleteMessageBatchAsync(request, ct), 3, ct);

            _logger.LogInformation(
                "Deleted {Successful} messages from SQS queue {QueueUrl}. Failed: {Failed}",
                response.Successful.Count, queueUrl, response.Failed.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message batch from SQS queue {QueueUrl}", queueUrl);
            throw;
        }
    }
}
