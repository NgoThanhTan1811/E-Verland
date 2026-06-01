namespace Modules.Payment.Infrastructure.Services;

public sealed class SePayOptions
{
    public const string SectionName = "SePay";

    public string? ApiKey { get; set; }
    public string? SecretKey { get; set; }
    public string[] AllowedIps { get; set; } = [];
    public string BaseUrl { get; set; } = "";
    public int MaxRetries { get; set; } = 3;
}
