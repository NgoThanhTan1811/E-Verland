using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Infra.Meilisearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infra.Meilisearch;

public sealed class MeilisearchService(ILogger<MeilisearchService> logger, IHttpClientFactory httpClientFactory, IOptions<MeilisearchOptions> options) : IMeilisearchService
{
    private readonly ILogger<MeilisearchService> _logger = logger;
    private readonly IHttpClientFactory _httpFactory = httpClientFactory;
    private readonly MeilisearchOptions _options = options.Value;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private HttpClient CreateClient()
    {
        var client = _httpFactory.CreateClient("meilisearch");
        client.BaseAddress = new Uri(_options.Endpoint);
        if (!string.IsNullOrWhiteSpace(_options.MasterKey))
            client.DefaultRequestHeaders.Add("X-Meili-API-Key", _options.MasterKey);
        return client;
    }

    public async Task<string> IndexDocumentAsync<T>(string index, string documentId, T document, CancellationToken ct = default) where T : class
    {
        ct.ThrowIfCancellationRequested();
        var client = CreateClient();

        var payload = new[] { document };
        var res = await client.PostAsJsonAsync($"/indexes/{index}/documents", payload, _jsonOptions, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Meilisearch index failed ({Status}): {Body}", res.StatusCode, body);
            throw new InvalidOperationException($"Meilisearch index failed: {res.StatusCode}");
        }

        _logger.LogInformation("Indexed document {DocumentId} into meilisearch index {Index}", documentId, index);
        return documentId;
    }

    public async Task<int> BulkIndexAsync<T>(string index, List<(string id, T document)> documents, CancellationToken ct = default) where T : class
    {
        ct.ThrowIfCancellationRequested();
        var client = CreateClient();
        var payload = documents.Select(d => d.document).ToList();
        var res = await client.PostAsJsonAsync($"/indexes/{index}/documents", payload, _jsonOptions, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Meilisearch bulk index failed ({Status}): {Body}", res.StatusCode, body);
            throw new InvalidOperationException($"Meilisearch bulk index failed: {res.StatusCode}");
        }

        return documents.Count;
    }

    public async Task<SearchResult<T>> SearchAsync<T>(string index, SearchQuery query, CancellationToken ct = default) where T : class
    {
        ct.ThrowIfCancellationRequested();
        var client = CreateClient();

        var request = new
        {
            q = query.QueryText ?? string.Empty,
            limit = query.PageSize,
            offset = (Math.Max(1, query.Page) - 1) * Math.Max(1, query.PageSize)
        };

        var res = await client.PostAsJsonAsync($"/indexes/{index}/search", request, _jsonOptions, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Meilisearch search failed ({Status}): {Body}", res.StatusCode, body);
            throw new InvalidOperationException($"Meilisearch search failed: {res.StatusCode}");
        }

        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var hits = doc.RootElement.GetProperty("hits");
        var total = doc.RootElement.GetProperty("estimatedTotalHits").GetInt64();

        var items = new List<T>();
        foreach (var hit in hits.EnumerateArray())
        {
            var item = JsonSerializer.Deserialize<T>(hit.GetRawText(), _jsonOptions);
            if (item != null) items.Add(item);
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        return new SearchResult<T>(items, total, page, pageSize, TimeSpan.Zero);
    }

    public async Task DeleteDocumentAsync(string index, string documentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var client = CreateClient();

        var payload = new { ids = new[] { documentId } };
        var res = await client.PostAsJsonAsync($"/indexes/{index}/documents/delete-batch", payload, _jsonOptions, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Meilisearch delete failed ({Status}): {Body}", res.StatusCode, body);
            throw new InvalidOperationException($"Meilisearch delete failed: {res.StatusCode}");
        }
    }

    public Task UpdateDocumentAsync<T>(string index, string documentId, T document, CancellationToken ct = default) where T : class
    {
        // Meilisearch treats indexing as upsert — reuse IndexDocumentAsync
        return IndexDocumentAsync(index, documentId, document, ct);
    }
}
