using Microsoft.Extensions.Options;
using Serilog;

namespace Infra.AWS.CloudWatch;

/// <summary>
/// CloudWatch metrics publisher based on Serilog EMF logs.
/// This avoids direct PutMetricData API calls to reduce request volume and cost.
/// </summary>
public sealed class CloudWatchService : ICloudWatchService
{
    private readonly CloudWatchOptions _options;
    private readonly Serilog.ILogger _logger;

    public CloudWatchService(
        IOptions<CloudWatchOptions> options)
    {
        _options = options.Value;
        _logger = Log.ForContext<CloudWatchService>();
    }

    public async Task PutMetricAsync(
        string metricName,
        double value,
        string unit = "Count",
        Dictionary<string, string>? dimensions = null,
        CancellationToken ct = default)
    {
        await PutMetricsBatchAsync(
            [new CloudWatchMetric(metricName, value, unit, dimensions)],
            ct);
    }

    public async Task PutMetricsBatchAsync(List<CloudWatchMetric> metrics, CancellationToken ct = default)
    {
        if (metrics.Count == 0)
        {
            return;
        }

        var groups = metrics.GroupBy(m => ToDimensionKey(m.Dimensions));
        foreach (var group in groups)
        {
            ct.ThrowIfCancellationRequested();

            var dimensions = group.First().Dimensions ?? new Dictionary<string, string>();
            var metricValues = group
                .GroupBy(m => m.MetricName)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.Value));
            var metricDefinitions = group
                .GroupBy(m => m.MetricName)
                .Select(g => new EmfMetricDefinition(g.Key, g.First().Unit))
                .ToArray();

            var emf = new Dictionary<string, object?>
            {
                ["_aws"] = new
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    CloudWatchMetrics = new[]
                    {
                        new
                        {
                            Namespace = _options.MetricNamespace,
                            Dimensions = new[] { dimensions.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray() },
                            Metrics = metricDefinitions
                        }
                    }
                }
            };

            foreach (var (name, metricValue) in metricValues)
            {
                emf[name] = metricValue;
            }

            foreach (var (key, value) in dimensions)
            {
                emf[key] = value;
            }

            _logger.Information("{@EmfMetric}", emf);
        }
    }

    public async Task CreateLogGroupAsync(string logGroupName, int retentionDays, CancellationToken ct = default)
    {
        _logger.Information("EMF log group setup should be managed by CloudWatch Logs agent. Requested group {LogGroupName} with retention {RetentionDays} days.",
            logGroupName, retentionDays);
        await Task.CompletedTask;
    }

    public async Task PutLogEventsAsync(string logGroupName, string logStreamName, List<string> messages, CancellationToken ct = default)
    {
        foreach (var message in messages)
        {
            _logger.Information("{LogMessage}", message);
        }

        await Task.CompletedTask;
    }

    private static string ToDimensionKey(Dictionary<string, string>? dimensions)
    {
        if (dimensions == null || dimensions.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("|", dimensions
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private sealed record EmfMetricDefinition(string Name, string Unit);
}
