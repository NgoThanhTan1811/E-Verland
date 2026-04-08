namespace Infra.AWS.CloudWatch;

/// <summary>
/// AWS CloudWatch configuration options
/// </summary>
public sealed class CloudWatchOptions
{
    public const string SectionName = "AWS:CloudWatch";

    public string Region { get; set; } = "us-east-1";

    // Log Groups
    public string ApplicationLogGroup { get; set; } = "/aws/e-verland/application";
    public string ErrorLogGroup { get; set; } = "/aws/e-verland/errors";
    public string PerformanceLogGroup { get; set; } = "/aws/e-verland/performance";

    // Metrics
    public string MetricNamespace { get; set; } = "E-Verland";

    // Retention
    public int LogRetentionDays { get; set; } = 30;
}
