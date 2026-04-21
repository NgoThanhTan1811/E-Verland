namespace Infra.AWS.CloudWatch;

/// <summary>
/// Interface for CloudWatch observability operations.
/// Metrics are emitted as Serilog EMF log events (no PutMetricData API calls).
/// </summary>
public interface ICloudWatchService
{
    /// <summary>
    /// Emit a custom metric via Serilog EMF log event.
    /// </summary>
    Task PutMetricAsync(string metricName, double value, string unit = "Count", Dictionary<string, string>? dimensions = null, CancellationToken ct = default);

    /// <summary>
    /// Emit multiple metrics in one or more EMF log events.
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
