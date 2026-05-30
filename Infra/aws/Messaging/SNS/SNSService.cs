using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Infra.AWS.Resilience;
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

            _logger.LogDebug("Publishing to SNS topic {TopicArn}. SNSOptions: Order={OrderTopic},Payment={PaymentTopic},User={UserTopic}",
                topicArn, _options.OrderNotificationsTopicArn, _options.PaymentNotificationsTopicArn, _options.UserNotificationsTopicArn);

            var response = await AwsRetryPolicy.ExecuteAsync(() => _snsClient.PublishAsync(request, ct), 3, ct);

            _logger.LogInformation(
                "Published message to SNS topic {TopicArn}. MessageId: {MessageId}",
                topicArn, response.MessageId);

            return response.MessageId;
        }
        catch (NotFoundException nfEx)
        {
            // SNS returns NotFound when the topic ARN does not exist in the account/region
            _logger.LogError(nfEx, "SNS topic not found: {TopicArn}. Verify the topic ARN exists in the configured AWS account and region. Configured SNSOptions Order={Order}, Payment={Payment}, User={User}",
                topicArn,
                _options.OrderNotificationsTopicArn,
                _options.PaymentNotificationsTopicArn,
                _options.UserNotificationsTopicArn);
            throw;
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

            var response = await AwsRetryPolicy.ExecuteAsync(() => _snsClient.PublishAsync(request, ct), 3, ct);

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

            var response = await AwsRetryPolicy.ExecuteAsync(() => _snsClient.SubscribeAsync(request, ct), 3, ct);

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

            await AwsRetryPolicy.ExecuteAsync(() => _snsClient.UnsubscribeAsync(request, ct), 3, ct);

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

            var response = await AwsRetryPolicy.ExecuteAsync(() => _snsClient.CreateTopicAsync(request, ct), 3, ct);

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
