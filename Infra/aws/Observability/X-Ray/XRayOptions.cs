namespace Infra.AWS.XRay;

/// <summary>
/// AWS X-Ray configuration options
/// </summary>
public sealed class XRayOptions
{
    public const string SectionName = "AWS:XRay";

    public bool Enabled { get; set; } = true;
    public string ServiceName { get; set; } = "E-Verland";
    public string Region { get; set; } = "ap-southeast-1";

    // Sampling
    public double SamplingRate { get; set; } = 0.1; // 10% of requests

    // Tracing
    public bool TraceSqlQueries { get; set; } = true;
    public bool TraceHttpRequests { get; set; } = true;
}
