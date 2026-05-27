using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.External;

namespace Modules.Shipping.Infrastructure.Services.Ghn;

public sealed class GhnClient : IGhnClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ILogger<GhnClient> _logger;
    private readonly string _baseUrl;
    private readonly int _maxRetries;

    public GhnClient(HttpClient http, ILogger<GhnClient> logger, IConfiguration configuration, IOptions<GhnOptions> options)
    {
        _http = http;
        _logger = logger;

        var token = configuration["GHN:Token"]
            ?? Environment.GetEnvironmentVariable("GHN_TOKEN")
            ?? options.Value.Token;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Missing GHN token (GHN:Token or GHN_TOKEN)");
        }

        var shopId = configuration["GHN:ShopId"]
            ?? Environment.GetEnvironmentVariable("GHN_SHOP_ID");

        if (!int.TryParse(shopId, out var parsedShopId))
        {
            parsedShopId = options.Value.ShopId;
        }

        if (parsedShopId <= 0)
        {
            throw new InvalidOperationException("Missing GHN shop id (GHN:ShopId or GHN_SHOP_ID)");
        }

        _baseUrl = ResolveBaseUrl(configuration, options.Value);
        _maxRetries = options.Value.MaxRetries <= 0 ? 3 : options.Value.MaxRetries;

        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Token", token);
        _http.DefaultRequestHeaders.Add("ShopId", parsedShopId.ToString());
    }

    public Task<GhnApiResponse<GhnCreateOrderResponse>> CreateOrderAsync(GhnCreateOrderRequest request, CancellationToken ct = default)
    {
        return PostAsync<GhnCreateOrderResponse>("shipping-order/create", request, ct);
    }

    public Task<GhnApiResponse<GhnFeeResponse>> CalculateFeeAsync(GhnFeeRequest request, CancellationToken ct = default)
    {
        return PostAsync<GhnFeeResponse>("shipping-order/fee", request, ct);
    }

    public Task<GhnApiResponse<List<GhnServiceResponse>>> GetAvailableServicesAsync(GhnServiceRequest request, CancellationToken ct = default)
    {
        return PostAsync<List<GhnServiceResponse>>("shipping-order/available-services", request, ct);
    }

    public Task<GhnApiResponse<List<GhnCancelResult>>> CancelOrderAsync(GhnCancelRequest request, CancellationToken ct = default)
    {
        return PostAsync<List<GhnCancelResult>>("switch-status/cancel", request, ct);
    }

    public Task<GhnApiResponse<GhnOrderInfoResponse>> GetOrderInfoAsync(string orderCode, CancellationToken ct = default)
    {
        var payload = new { order_code = orderCode };
        return PostAsync<GhnOrderInfoResponse>("shipping-order/detail", payload, ct);
    }

    private async Task<GhnApiResponse<T>> PostAsync<T>(string path, object payload, CancellationToken ct)
    {
        var url = BuildUrl(path);

        for (var attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(url, payload, JsonOptions, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("GHN API error {StatusCode}: {Error}", (int)response.StatusCode, error);

                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(Backoff(attempt), ct);
                        continue;
                    }

                    throw new GhnApiException("GHN API request failed", (int)response.StatusCode);
                }

                var result = await response.Content.ReadFromJsonAsync<GhnApiResponse<T>>(JsonOptions, ct);
                if (result is null)
                {
                    throw new GhnApiException("GHN API returned empty response", (int)response.StatusCode);
                }

                return result;
            }
            catch (GhnApiException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                _logger.LogWarning(ex, "GHN request attempt {Attempt} failed", attempt);
                await Task.Delay(Backoff(attempt), ct);
            }
        }

        throw new GhnApiException("GHN API request failed after retries", 500);
    }

    private string BuildUrl(string path)
    {
        var baseUrl = _baseUrl.TrimEnd('/');
        var relative = path.TrimStart('/');
        return $"{baseUrl}/{relative}";
    }

    private static TimeSpan Backoff(int attempt)
    {
        return TimeSpan.FromSeconds(Math.Min(8, Math.Pow(2, attempt)));
    }

    private static string ResolveBaseUrl(IConfiguration configuration, GhnOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return options.BaseUrl;
        }

        var apiUrl = configuration["GHN:ApiUrl"];
        if (!string.IsNullOrWhiteSpace(apiUrl) && Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
        {
            return $"{uri.Scheme}://{uri.Host}/shiip/public-api/v2";
        }

        return "https://dev-online-gateway.ghn.vn/shiip/public-api/v2";
    }
}
