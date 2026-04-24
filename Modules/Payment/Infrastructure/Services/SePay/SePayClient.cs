using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Modules.Payment.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Modules.Payment.Infrastructure.Services
{
    public class SePayClient : ISePayClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<SePayClient> _logger;
        private readonly string _baseUrl;
        private readonly int _maxRetries;

        public SePayClient(HttpClient http, ILogger<SePayClient> logger, IConfiguration configuration)
        {
            _http = http;
            _logger = logger;

            var apiKey = configuration["Payment:SePay:ApiKey"]
                ?? Environment.GetEnvironmentVariable("SEPAY_API_KEY")
                ?? Environment.GetEnvironmentVariable("SEPAY_API")
                ?? throw new InvalidOperationException("Missing Payment:SePay:ApiKey (or SEPAY_API_KEY environment variable).");

            _baseUrl = configuration["Payment:SePay:BaseUrl"]
                ?? Environment.GetEnvironmentVariable("SEPAY_BASE_URL")
                ?? "https://my.sepay.vn/userapi";

            _maxRetries = int.TryParse(
                configuration["Payment:SePay:MaxRetries"] ?? Environment.GetEnvironmentVariable("SEPAY_MAX_RETRIES"),
                out var retries)
                ? Math.Max(1, retries)
                : 3;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Apikey", apiKey);
        }

        public async Task<string?> CreatePaymentLinkAsync(
            string paymentCode, decimal amount, string description, CancellationToken ct = default)
        {
            var payload = new { payment_code = paymentCode, amount, description };

            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "SePay CreatePaymentLink attempt {Attempt}/{MaxRetries} for payment code {PaymentCode}",
                        attempt, _maxRetries, paymentCode);

                    var response = await _http.PostAsJsonAsync($"{_baseUrl}/transactions/create", payload, ct);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(ct);
                        _logger.LogError(
                            "SePay API error (HTTP {StatusCode}): {ErrorContent} for payment code {PaymentCode}",
                            response.StatusCode, errorContent, paymentCode);

                        if (attempt < _maxRetries)
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                            _logger.LogWarning("Retrying in {Delay} seconds...", delay.TotalSeconds);
                            await Task.Delay(delay, ct);
                            continue;
                        }

                        throw new SePayApiException(
                            "Failed to create payment link after retries.",
                            paymentCode,
                            (int)response.StatusCode);
                    }

                    var result = await response.Content.ReadFromJsonAsync<SePayResponse>(ct);

                    _logger.LogInformation(
                        "SePay payment link created successfully for payment code {PaymentCode}: {PaymentUrl}",
                        paymentCode, result?.PaymentUrl);

                    return result?.PaymentUrl;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex,
                        "Network error during SePay API call (attempt {Attempt}/{MaxRetries}) for payment code {PaymentCode}",
                        attempt, _maxRetries, paymentCode);

                    if (attempt < _maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        await Task.Delay(delay, ct);
                        continue;
                    }

                    throw new SePayApiException(
                        "Network error while calling SePay.",
                        paymentCode);
                }
                catch (Exception ex) when (ex is not SePayApiException)
                {
                    _logger.LogError(ex,
                        "Unexpected error during SePay API call for payment code {PaymentCode}",
                        paymentCode);
                    throw new SePayApiException(
                        "Unexpected error while calling SePay.",
                        paymentCode,
                        null);
                }
            }

            throw new SePayApiException(
                "Failed to create payment link after retries.",
                paymentCode);
        }

        private sealed class SePayResponse
        {
            [JsonPropertyName("payment_url")]
            public string? PaymentUrl { get; init; }
        }
    }
}
