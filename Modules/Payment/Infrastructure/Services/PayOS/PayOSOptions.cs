public sealed class PayOSOptions
{
    public string ClientId { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string ChecksumKey { get; set; } = default!;
    public string ReturnUrl { get; set; } = default!;
    public string CancelUrl { get; set; } = default!;
    public string WebhookUrl { get; set; } = default!;
}