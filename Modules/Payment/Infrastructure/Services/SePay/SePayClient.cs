using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

        public SePayClient(HttpClient http, ILogger<SePayClient> logger, IConfiguration configuration)
        {
            _http = http;
            _logger = logger;

            _apiKey = configuration[$"{SePayOptions.SectionName}:ApiKey"]
                ?? configuration["SePay:APIKey"]
                ?? Environment.GetEnvironmentVariable("SEPAY_API_KEY")
                ?? Environment.GetEnvironmentVariable("SEPAY_API")
                ?? throw new InvalidOperationException("Missing Payment:SePay:ApiKey (or SEPAY_API_KEY environment variable).");

            if (_apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _apiKey = _apiKey[7..].Trim();
            }

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            _baseUrl = configuration[$"{SePayOptions.SectionName}:BaseUrl"]
                ?? configuration["SePay:BaseUrl"]
                ?? Environment.GetEnvironmentVariable("SEPAY_BASE_URL")
                ?? "https://my.sepay.vn/userapi";

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
            var payload = new { payment_code = paymentCode, amount, description };

            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "SePay CreatePaymentLink attempt {Attempt}/{MaxRetries} for payment code {PaymentCode}",
                        attempt, _maxRetries, paymentCode);

                    var response = await SendAsync(HttpMethod.Post, "transactions/create", payload, ct);

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

            var response = await SendAsync(HttpMethod.Get, "transactions/list", null, ct, query);

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

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            var response = await _http.SendAsync(request, ct);

            // 2. Đọc nội dung phản hồi dưới dạng chuỗi để kiểm tra lỗi
            var content = await response.Content.ReadAsStringAsync(ct);

            // 3. Nếu response không thành công (Status code != 2xx)
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Lỗi gọi API SePay: {StatusCode}. Nội dung: {Content}", response.StatusCode, content);
                throw new Exception($"API SePay lỗi {response.StatusCode}: {content}");
            }

            // 4. Kiểm tra nếu nội dung bắt đầu bằng ký tự lạ (HTML thay vì JSON)
            if (content.TrimStart().StartsWith("<"))
            {
                _logger.LogError("API SePay trả về HTML thay vì JSON. Nội dung: {Content}", content);
                throw new Exception("Lỗi: API SePay trả về trang HTML (thường do sai Token hoặc lỗi server).");
            }

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
