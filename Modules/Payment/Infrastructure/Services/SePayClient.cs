using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Modules.Payment.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace Modules.Payment.Infrastructure.Services
{
    public class SePayClient : ISePayClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<SePayClient> _logger;
        private const string BaseUrl = "https://my.sepay.vn/userapi";
        private const int MaxRetries = 3;

        public SePayClient(HttpClient http, ILogger<SePayClient> logger)
        {
            _http = http;
            _logger = logger;
            var apiKey = Environment.GetEnvironmentVariable("SEPAY_API")
                ?? throw new InvalidOperationException("Missing SEPAY_API env var");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Apikey", apiKey);
        }

        public async Task<string?> CreatePaymentLinkAsync(
            string paymentCode, decimal amount, string description, CancellationToken ct = default)
        {
            var payload = new { payment_code = paymentCode, amount, description };

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "SePay CreatePaymentLink attempt {Attempt}/{MaxRetries} for payment code {PaymentCode}",
                        attempt, MaxRetries, paymentCode);

                    var response = await _http.PostAsJsonAsync($"{BaseUrl}/transactions/create", payload, ct);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(ct);
                        _logger.LogError(
                            "SePay API error (HTTP {StatusCode}): {ErrorContent} for payment code {PaymentCode}",
                            response.StatusCode, errorContent, paymentCode);

                        if (attempt < MaxRetries)
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                            _logger.LogWarning("Retrying in {Delay} seconds...", delay.TotalSeconds);
                            await Task.Delay(delay, ct);
                            continue;
                        }

                        throw new SePayApiException(
                            $"Failed to create payment link after {MaxRetries} attempts. Status: {response.StatusCode}",
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
                        attempt, MaxRetries, paymentCode);

                    if (attempt < MaxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        await Task.Delay(delay, ct);
                        continue;
                    }

                    throw new SePayApiException(
                        $"Network error after {MaxRetries} attempts: {ex.Message}",
                        paymentCode);
                }
                catch (Exception ex) when (ex is not SePayApiException)
                {
                    _logger.LogError(ex,
                        "Unexpected error during SePay API call for payment code {PaymentCode}",
                        paymentCode);
                    throw new SePayApiException(
                        $"Unexpected error: {ex.Message}",
                        paymentCode,
                        null);
                }
            }

            throw new SePayApiException(
                $"Failed to create payment link after {MaxRetries} attempts",
                paymentCode);
        }

        private sealed class SePayResponse
        {
            [JsonPropertyName("payment_url")]
            public string? PaymentUrl { get; init; }
        }
    }
}
