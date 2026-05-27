using System.Net;
using Serilog.Sinks.AwsCloudWatch;

namespace Infra.AWS.CloudWatch;

public sealed class PrefixedLogStreamNameProvider(string prefix) : ILogStreamNameProvider
{
    private const string DateTimeFormat = "yyyy-MM-dd-HH-mm-ss";
    private readonly string _prefix = string.IsNullOrWhiteSpace(prefix) ? "e-verland" : prefix.Trim();

    public string GetLogStreamName()
    {
        return $"{_prefix}/{DateTime.UtcNow.ToString(DateTimeFormat)}_{Dns.GetHostName()}_{Guid.NewGuid()}";
    }
}
