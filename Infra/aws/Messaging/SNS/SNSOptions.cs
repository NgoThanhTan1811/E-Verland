namespace Infra.AWS.SNS;

/// <summary>
/// AWS SNS configuration options
/// </summary>
public sealed class SNSOptions
{
    public const string SectionName = "AWS:SNS";

    public string Region { get; set; } = "ap-southeast-1";

    // Topic ARNs
    public string OrderNotificationsTopicArn { get; set; } = string.Empty;
    public string PaymentNotificationsTopicArn { get; set; } = string.Empty;
    public string UserNotificationsTopicArn { get; set; } = string.Empty;

    // SMS settings
    public string DefaultSMSSenderId { get; set; } = "E-Verland";
    public string SMSType { get; set; } = "Transactional"; // or Promotional
}
