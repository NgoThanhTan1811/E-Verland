using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infra.AWS.CloudWatch;

/// <summary>
/// AWS CloudWatch service implementation
/// </summary>
public sealed class CloudWatchService : ICloudWatchService
{
    private readonly IAmazonCloudWatch _cloudWatchClient;
    private readonly CloudWatchOptions _options;
    private readonly ILogger<CloudWatchService> _logger;

    public CloudWatchService(
        IAmazonCloudWatch cloudWatchClient,
        IOptions<CloudWatchOptions> options,
        ILogger<CloudWatchService> logger)
    {
        _cloudWatchClient = cloudWatchClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PutMetricAsync(
        string metricName,
        double value,
        string unit = "Count",
        Dictionary<string, string>? dimensions = null,
        CancellationToken ct = default)
    {
        try
        {
            var metricData = new MetricDatum
            {
                MetricName = metricName,
                Value = value,
                Unit = unit,
                Timestamp = DateTime.UtcNow
            };

            if (dimensions != null)
            {
                metricData.Dimensions = dimensions.Select(d => new Dimension
                {
                    Name = d.Key,
                    Value = d.Value
                }).ToList();
            }

            var request = new PutMetricDataRequest
            {
                Namespace = _options.MetricNamespace,
                MetricData = new List<MetricDatum> { metricData }
            };

            await _cloudWatchClient.PutMetricDataAsync(request, ct);

            _logger.LogDebug("Put metric to CloudWatch: {MetricName} = {Value} {Unit}", metricName, value, unit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put metric to CloudWatch: {MetricName}", metricName);
            throw;
        }
    }

    public async Task PutMetricsBatchAsync(List<CloudWatchMetric> metrics, CancellationToken ct = default)
    {
        try
        {
            var metricData = metrics.Select(m =>
            {
                var datum = new MetricDatum
                {
                    MetricName = m.MetricName,
                    Value = m.Value,
                    Unit = m.Unit,
                    Timestamp = m.Timestamp ?? DateTime.UtcNow
                };

                if (m.Dimensions != null)
                {
                    datum.Dimensions = m.Dimensions.Select(d => new Dimension
                    {
                        Name = d.Key,
                        Value = d.Value
                    }).ToList();
                }

                return datum;
            }).ToList();

            var request = new PutMetricDataRequest
            {
                Namespace = _options.MetricNamespace,
                MetricData = metricData
            };

            await _cloudWatchClient.PutMetricDataAsync(request, ct);

            _logger.LogInformation("Put {Count} metrics to CloudWatch", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put metrics batch to CloudWatch");
            throw;
        }
    }

    public async Task CreateLogGroupAsync(string logGroupName, int retentionDays, CancellationToken ct = default)
    {
        try
        {
            // Note: CloudWatch Logs operations would require Amazon.CloudWatchLogs package
            // This is a placeholder for the interface
            _logger.LogInformation("Create log group: {LogGroupName} with retention: {RetentionDays} days",
                logGroupName, retentionDays);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create log group: {LogGroupName}", logGroupName);
            throw;
        }
    }

    public async Task PutLogEventsAsync(string logGroupName, string logStreamName, List<string> messages, CancellationToken ct = default)
    {
        try
        {
            // Placeholder for CloudWatch Logs operations
            _logger.LogDebug("Put {Count} log events to {LogGroupName}/{LogStreamName}",
                messages.Count, logGroupName, logStreamName);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put log events to {LogGroupName}/{LogStreamName}", logGroupName, logStreamName);
            throw;
        }
    }
}
