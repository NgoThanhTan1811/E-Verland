using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infra.AWS.SNS;

/// <summary>
/// AWS SNS service implementation
/// </summary>
public sealed class SNSService : ISNSService
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly SNSOptions _options;
    private readonly ILogger<SNSService> _logger;

    public SNSService(
        IAmazonSimpleNotificationService snsClient,
        IOptions<SNSOptions> options,
        ILogger<SNSService> logger)
    {
        _snsClient = snsClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> PublishAsync<T>(string topicArn, T message, string? subject = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(message);

            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = messageBody,
                Subject = subject
            };

            var response = await _snsClient.PublishAsync(request, ct);

            _logger.LogInformation(
                "Published message to SNS topic {TopicArn}. MessageId: {MessageId}",
                topicArn, response.MessageId);

            return response.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to SNS topic {TopicArn}", topicArn);
            throw;
        }
    }

    public async Task<string> SendSMSAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        try
        {
            var request = new PublishRequest
            {
                PhoneNumber = phoneNumber,
                Message = message,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["AWS.SNS.SMS.SMSType"] = new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = _options.SMSType
                    }
                }
            };

            var response = await _snsClient.PublishAsync(request, ct);

            _logger.LogInformation(
                "SMS sent to {PhoneNumber}. MessageId: {MessageId}",
                phoneNumber, response.MessageId);

            return response.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<string> SubscribeAsync(string topicArn, string protocol, string endpoint, CancellationToken ct = default)
    {
        try
        {
            var request = new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = protocol,
                Endpoint = endpoint
            };

            var response = await _snsClient.SubscribeAsync(request, ct);

            _logger.LogInformation(
                "Subscribed {Endpoint} to topic {TopicArn}. SubscriptionArn: {SubscriptionArn}",
                endpoint, topicArn, response.SubscriptionArn);

            return response.SubscriptionArn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe {Endpoint} to topic {TopicArn}", endpoint, topicArn);
            throw;
        }
    }

    public async Task UnsubscribeAsync(string subscriptionArn, CancellationToken ct = default)
    {
        try
        {
            var request = new UnsubscribeRequest
            {
                SubscriptionArn = subscriptionArn
            };

            await _snsClient.UnsubscribeAsync(request, ct);

            _logger.LogInformation("Unsubscribed {SubscriptionArn}", subscriptionArn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe {SubscriptionArn}", subscriptionArn);
            throw;
        }
    }

    public async Task<string> CreateTopicAsync(string topicName, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateTopicRequest
            {
                Name = topicName
            };

            var response = await _snsClient.CreateTopicAsync(request, ct);

            _logger.LogInformation("Created SNS topic {TopicName}. ARN: {TopicArn}", topicName, response.TopicArn);

            return response.TopicArn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SNS topic {TopicName}", topicName);
            throw;
        }
    }
}
