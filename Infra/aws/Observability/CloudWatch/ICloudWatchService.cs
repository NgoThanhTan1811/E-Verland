namespace Infra.AWS.CloudWatch;

/// <summary>
/// Interface for AWS CloudWatch operations
/// </summary>
public interface ICloudWatchService
{
    /// <summary>
    /// Put a custom metric to CloudWatch
    /// </summary>
    Task PutMetricAsync(string metricName, double value, string unit = "Count", Dictionary<string, string>? dimensions = null, CancellationToken ct = default);

    /// <summary>
    /// Put multiple metrics in a batch
    /// </summary>
    Task PutMetricsBatchAsync(List<CloudWatchMetric> metrics, CancellationToken ct = default);

    /// <summary>
    /// Create a log group
    /// </summary>
    Task CreateLogGroupAsync(string logGroupName, int retentionDays, CancellationToken ct = default);

    /// <summary>
    /// Put log events
    /// </summary>
    Task PutLogEventsAsync(string logGroupName, string logStreamName, List<string> messages, CancellationToken ct = default);
}

/// <summary>
/// CloudWatch metric wrapper
/// </summary>
public sealed record CloudWatchMetric(
    string MetricName,
    double Value,
    string Unit = "Count",
    Dictionary<string, string>? Dimensions = null,
    DateTime? Timestamp = null
);
