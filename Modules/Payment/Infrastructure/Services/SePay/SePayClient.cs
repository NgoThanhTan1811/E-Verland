using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;

namespace Modules.Payment.Infrastructure.Services
{
    public class SePayClient : ISePayClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<SePayClient> _logger;
        private readonly string _baseUrl;
        private readonly int _maxRetries;
        private readonly string _apiKey;
        private readonly string _qrImageTemplate;

        public SePayClient(HttpClient http, ILogger<SePayClient> logger, IConfiguration configuration)
        {
            _http = http;
            _logger = logger;

            _apiKey = configuration[$"{SePayOptions.SectionName}:ApiKey"]
                ?? configuration["SePay:APIKey"]
                ?? Environment.GetEnvironmentVariable("SEPAY_API_KEY")
                ?? Environment.GetEnvironmentVariable("SEPAY_API")
                ?? throw new InvalidOperationException("Missing Payment:SePay:ApiKey (or SEPAY_API_KEY environment variable).");

            // Normalize token if caller provided a Bearer prefix
            if (_apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _apiKey = _apiKey[7..].Trim();
            }

            // Ensure HttpClient has Bearer auth for SePay v2
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            _baseUrl = configuration[$"{SePayOptions.SectionName}:BaseUrl"]
                ?? configuration["SePay:BaseUrl"]
                ?? Environment.GetEnvironmentVariable("SEPAY_BASE_URL")
                ?? "https://my.sepay.vn/userapi";

            _qrImageTemplate = configuration[$"{SePayOptions.SectionName}:QrImageUrl"]
                ?? configuration["SePay:QrImageUrl"]
                ?? Environment.GetEnvironmentVariable("SEPAY_QR_IMAGE_URL")
                ?? "https://qr.sepay.vn/img?bank=VPBank&acc=239688233&template=&amount=&des=";

            _maxRetries = int.TryParse(
                configuration[$"{SePayOptions.SectionName}:MaxRetries"]
                    ?? configuration["SePay:MaxRetries"]
                    ?? Environment.GetEnvironmentVariable("SEPAY_MAX_RETRIES"),
                out var retries)
                ? Math.Max(1, retries)
                : 3;
        }

        public async Task<string?> CreatePaymentLinkAsync(
            string paymentCode, decimal amount, string description, CancellationToken ct = default)
        {
            await Task.CompletedTask;

            var transferDescription = string.IsNullOrWhiteSpace(description) ? paymentCode : description.Trim();
            var qrUrl = BuildQrImageUrl(amount, transferDescription);

            _logger.LogInformation(
                "SePay QR image URL prepared for payment code {PaymentCode}: {PaymentUrl}",
                paymentCode, qrUrl);

            return qrUrl;
        }

        public async Task<SePayTransactionsResponseDto> GetTransactionsAsync(
            string? accountNumber = null,
            DateOnly? transactionDateMin = null,
            DateOnly? transactionDateMax = null,
            long? sinceId = null,
            int? limit = null,
            string? referenceNumber = null,
            decimal? amountIn = null,
            decimal? amountOut = null,
            CancellationToken ct = default)
        {
            var query = new List<string>();

            AppendQuery(query, "account_number", accountNumber);
            AppendQuery(query, "transaction_date_min", transactionDateMin?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AppendQuery(query, "transaction_date_max", transactionDateMax?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AppendQuery(query, "since_id", sinceId?.ToString(CultureInfo.InvariantCulture));
            AppendQuery(query, "limit", limit?.ToString(CultureInfo.InvariantCulture));
            AppendQuery(query, "reference_number", referenceNumber);
            AppendQuery(query, "amount_in", amountIn?.ToString(CultureInfo.InvariantCulture));
            AppendQuery(query, "amount_out", amountOut?.ToString(CultureInfo.InvariantCulture));

            var response = await SendAsync(HttpMethod.Get, "transactions", null, ct, query);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var retryAfterHeader = response.Headers.Contains("x-sepay-userapi-retry-after")
                    ? response.Headers.GetValues("x-sepay-userapi-retry-after").FirstOrDefault()
                    : response.Headers.RetryAfter?.Delta?.TotalSeconds.ToString();

                if (!int.TryParse(retryAfterHeader, out var retrySeconds))
                {
                    retrySeconds = 3; // fallback
                }

                _logger.LogWarning("SePay rate limited when fetching transactions. Retry after {RetrySeconds}s.", retrySeconds);
                await Task.Delay(TimeSpan.FromSeconds(retrySeconds), ct);
                // After waiting once, call again (simple retry)
                response = await SendAsync(HttpMethod.Get, "transactions", null, ct, query);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "SePay API error (HTTP {StatusCode}): {ErrorContent} while fetching transactions",
                    response.StatusCode, errorContent);

                throw new SePayApiException(
                    "Failed to fetch SePay transactions.",
                    transactionId: null,
                    statusCode: (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<SePayTransactionsResponseDto>(ct);
            return result ?? new SePayTransactionsResponseDto(0, null, null, Array.Empty<SePayTransactionResponseDto>());
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string relativePath,
            object? body,
            CancellationToken ct,
            IReadOnlyCollection<string>? query = null)
        {
            var url = BuildUrl(relativePath, query);
            using var request = new HttpRequestMessage(method, url);

            // Always enforce Authorization header for every outbound SePay call.
            request.Headers.Authorization ??= _http.DefaultRequestHeaders.Authorization
                ?? new AuthenticationHeaderValue("Bearer", _apiKey);

            // Log outgoing request (mask API key)
            try
            {
                var authScheme = request.Headers.Authorization?.Scheme ?? _http.DefaultRequestHeaders.Authorization?.Scheme ?? "(none)";
                _logger.LogWarning("SePay outbound request: {Method} {Url} AuthorizationScheme:{Scheme}", method.Method, url, authScheme);
            }
            catch (Exception) { /* non-fatal logging error */ }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            var response = await _http.SendAsync(request, ct);

            // 2. Đọc nội dung phản hồi dưới dạng chuỗi để kiểm tra lỗi
            var content = await response.Content.ReadAsStringAsync(ct);

            // 3. Nếu response trả về HTML thay vì JSON -> coi như lỗi nghiêm trọng
            if (content.TrimStart().StartsWith("<"))
            {
                _logger.LogError("API SePay trả về HTML thay vì JSON. Nội dung: {Content}", content);
                throw new Exception("Lỗi: API SePay trả về trang HTML (thường do sai Token hoặc lỗi server).");
            }

            // 4. Trả về response cho caller để caller xử lý giữ nguyên status code (bao gồm 429)
            return response;
        }

        private string BuildUrl(string relativePath, IReadOnlyCollection<string>? query)
        {
            if (query is null || query.Count == 0)
            {
                return $"{_baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
            }

            return $"{_baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}?{string.Join("&", query)}";
        }

        private string BuildQrImageUrl(decimal amount, string description)
        {
            var url = ReplaceQueryParameter(_qrImageTemplate, "amount", amount.ToString(CultureInfo.InvariantCulture));
            url = ReplaceQueryParameter(url, "des", description);
            return url;
        }

        private static string ReplaceQueryParameter(string url, string name, string value)
        {
            var escapedValue = Uri.EscapeDataString(value);
            var pattern = $"([?&]){Regex.Escape(name)}=[^&]*";

            if (Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase))
            {
                return Regex.Replace(
                    url,
                    pattern,
                    match => $"{match.Groups[1].Value}{name}={escapedValue}",
                    RegexOptions.IgnoreCase);
            }

            return url.Contains('?')
                ? $"{url}&{name}={escapedValue}"
                : $"{url}?{name}={escapedValue}";
        }

        private static void AppendQuery(List<string> query, string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
        }

        private sealed class SePayResponse
        {
            [JsonPropertyName("payment_url")]
            public string? PaymentUrl { get; init; }
        }
    }
}
