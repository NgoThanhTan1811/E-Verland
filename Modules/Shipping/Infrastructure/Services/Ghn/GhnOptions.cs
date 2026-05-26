namespace Modules.Shipping.Infrastructure.Services.Ghn;

public sealed class GhnOptions
{
    public const string SectionName = "GHN";

    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api/v2";
    public string Token { get; set; } = string.Empty;
    public int ShopId { get; set; }
    public int MaxRetries { get; set; } = 3;
}
