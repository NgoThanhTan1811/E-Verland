namespace Infra.AWS.CloudWatch;

/// <summary>
/// AWS CloudWatch configuration options
/// </summary>
public sealed class CloudWatchOptions
{
    public const string SectionName = "AWS:CloudWatch";

    public string Region { get; set; } = "us-east-1";

    // Centralized logging via Serilog sink
    public bool Enabled { get; set; } = false;
    public string LogGroupName { get; set; } = "/aws/e-verland/application";
    public string LogStreamPrefix { get; set; } = "e-verland";
    public bool CreateLogGroup { get; set; } = true;
    public int BatchSizeLimit { get; set; } = 100;
    public int QueueSizeLimit { get; set; } = 10000;
    public int PeriodSeconds { get; set; } = 10;
    public int RetryAttempts { get; set; } = 5;

    // Log Groups
    public string ApplicationLogGroup { get; set; } = "/aws/e-verland/application";
    public string ErrorLogGroup { get; set; } = "/aws/e-verland/errors";
    public string PerformanceLogGroup { get; set; } = "/aws/e-verland/performance";

    // Metrics
    public string MetricNamespace { get; set; } = "E-Verland";

    // Retention
    public int LogRetentionDays { get; set; } = 30;
}
