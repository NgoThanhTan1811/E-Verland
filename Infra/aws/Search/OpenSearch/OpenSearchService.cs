using System.Collections.Concurrent;
using System.Text.Json;
using Infra.AWS.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infra.AWS.OpenSearch;

public sealed class OpenSearchService : IOpenSearchService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _store = new();
    private readonly OpenSearchOptions _options;
    private readonly ILogger<OpenSearchService> _logger;

    public OpenSearchService(IOptions<OpenSearchOptions> options, ILogger<OpenSearchService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> IndexDocumentAsync<T>(string index, string documentId, T document, CancellationToken ct = default) where T : class
    {
        await AwsRetryPolicy.ExecuteAsync(() =>
        {
            var indexStore = _store.GetOrAdd(index, _ => new ConcurrentDictionary<string, string>());
            indexStore[documentId] = JsonSerializer.Serialize(document);
            return Task.CompletedTask;
        }, 3, ct);

        _logger.LogInformation("Indexed document {DocumentId} into {Index}", documentId, index);
        return documentId;
    }

    public async Task<int> BulkIndexAsync<T>(string index, List<(string id, T document)> documents, CancellationToken ct = default) where T : class
    {
        var count = 0;
        foreach (var (id, document) in documents)
        {
            await IndexDocumentAsync(index, id, document, ct);
            count++;
        }

        return count;
    }

    public Task<SearchResult<T>> SearchAsync<T>(string index, SearchQuery query, CancellationToken ct = default) where T : class
    {
        var indexStore = _store.GetOrAdd(index, _ => new ConcurrentDictionary<string, string>());
        var loweredQuery = query.QueryText?.Trim().ToLowerInvariant() ?? string.Empty;

        var candidates = indexStore.Values
            .Select(json => JsonSerializer.Deserialize<T>(json))
            .Where(doc => doc != null)
            .Cast<T>()
            .ToList();

        if (!string.IsNullOrWhiteSpace(loweredQuery))
        {
            candidates = candidates.Where(doc =>
                JsonSerializer.Serialize(doc).ToLowerInvariant().Contains(loweredQuery))
                .ToList();
        }

        var total = candidates.Count;
        var pageSize = Math.Clamp(query.PageSize, 1, _options.MaxPageSize);
        var page = Math.Max(1, query.Page);
        var items = candidates.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new SearchResult<T>(items, total, page, pageSize, TimeSpan.Zero));
    }

    public async Task DeleteDocumentAsync(string index, string documentId, CancellationToken ct = default)
    {
        await AwsRetryPolicy.ExecuteAsync(() =>
        {
            if (_store.TryGetValue(index, out var indexStore))
            {
                indexStore.TryRemove(documentId, out _);
            }

            return Task.CompletedTask;
        }, 3, ct);
    }

    public Task UpdateDocumentAsync<T>(string index, string documentId, T document, CancellationToken ct = default) where T : class
    {
        return IndexDocumentAsync(index, documentId, document, ct);
    }
}
