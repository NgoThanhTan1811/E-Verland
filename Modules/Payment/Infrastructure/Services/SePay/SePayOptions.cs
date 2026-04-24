namespace Modules.Payment.Infrastructure.Services;

public sealed class SePayOptions
{
    public const string SectionName = "Payment:SePay";

    public string? ApiKey { get; set; }
    public string? SecretKey { get; set; }
    public string BaseUrl { get; set; } = "https://my.sepay.vn/userapi";
    public int MaxRetries { get; set; } = 3;
}
