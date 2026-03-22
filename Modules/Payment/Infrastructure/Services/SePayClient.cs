using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Modules.Payment.Application.Contracts;

namespace Modules.Payment.Infrastructure.Services
{
    public class SePayClient : ISePayClient
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://my.sepay.vn/userapi";

        public SePayClient(HttpClient http)
        {
            _http = http;
            var apiKey = Environment.GetEnvironmentVariable("SEPAY_API")
                ?? throw new InvalidOperationException("Missing SEPAY_API env var");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Apikey", apiKey);
        }

        public async Task<string?> CreatePaymentLinkAsync(
            string paymentCode, decimal amount, string description, CancellationToken ct = default)
        {
            var payload = new { payment_code = paymentCode, amount, description };
            var response = await _http.PostAsJsonAsync($"{BaseUrl}/transactions/create", payload, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SePayResponse>(ct);
            return result?.PaymentUrl;
        }

        private sealed class SePayResponse
        {
            [JsonPropertyName("payment_url")]
            public string? PaymentUrl { get; init; }
        }
    }
}
