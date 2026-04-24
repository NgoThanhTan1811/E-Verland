namespace Infra.AWS.SQS;

/// <summary>
/// AWS SQS configuration options
/// </summary>
public sealed class SQSOptions
{
    public const string SectionName = "AWS:SQS";

    public string Region { get; set; } = "us-east-1";

    // Queue URLs
    public string OrderEventsQueueUrl { get; set; } = string.Empty;
    public string PaymentNotificationsQueueUrl { get; set; } = string.Empty;
    public string EmailQueueUrl { get; set; } = string.Empty;

    // Polling settings
    public int MaxNumberOfMessages { get; set; } = 10;
    public int WaitTimeSeconds { get; set; } = 20; // Long polling
    public int VisibilityTimeout { get; set; } = 30;

    // Retry settings
    public int MaxReceiveCount { get; set; } = 3;
}
