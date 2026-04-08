using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Infra.AWS.OpenSearch;

/// <summary>
/// AWS OpenSearch service implementation
/// </summary>
public sealed class OpenSearchService : IOpenSearchService
{
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchOptions _options;
    private readonly ILogger<OpenSearchService> _logger;

    public OpenSearchService(
        IOptions<OpenSearchOptions> options,
        ILogger<OpenSearchService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var settings = new ConnectionSettings(new Uri(_options.Endpoint))
            .BasicAuthentication(_options.Username, _options.Password)
            .DisableDirectStreaming()
            .PrettyJson();

        _client = new OpenSearchClient(settings);
    }

    public async Task<string> IndexDocumentAsync<T>(string index, string documentId, T document, CancellationToken ct = default) where T : class
    {
        try
        {
            var response = await _client.IndexAsync(document, i => i
                .Index(index)
                .Id(documentId), ct);

            if (!response.IsValid)
            {
                throw new Exception($"Failed to index document: {response.DebugInformation}");
            }

            _logger.LogInformation("Indexed document {DocumentId} in index {Index}", documentId, index);

            return response.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document {DocumentId} in {Index}", documentId, index);
            throw;
        }
    }

    public async Task<int> BulkIndexAsync<T>(string index, List<(string id, T document)> documents, CancellationToken ct = default) where T : class
    {
        try
        {
            var bulkDescriptor = new BulkDescriptor();

            foreach (var (id, document) in documents)
            {
                bulkDescriptor.Index<T>(i => i
                    .Index(index)
                    .Id(id)
                    .Document(document));
            }

            var response = await _client.BulkAsync(bulkDescriptor, ct);

            if (!response.IsValid)
            {
                _logger.LogError("Bulk index failed: {DebugInfo}", response.DebugInformation);
            }

            var successCount = documents.Count - response.ItemsWithErrors.Count();

            _logger.LogInformation(
                "Bulk indexed {SuccessCount}/{TotalCount} documents in index {Index}",
                successCount, documents.Count, index);

            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk index documents in {Index}", index);
            throw;
        }
    }

    public async Task<SearchResult<T>> SearchAsync<T>(string index, SearchQuery query, CancellationToken ct = default) where T : class
    {
        try
        {
            var from = (query.Page - 1) * query.PageSize;

            var searchResponse = await _client.SearchAsync<T>(s => s
                .Index(index)
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(query.QueryText)
                        .Fields(f => f.Fields(query.Fields?.Select(field => (Field)field).ToArray() ?? [new Field("*")]))))
                .From(from)
                .Size(query.PageSize), ct);

            if (!searchResponse.IsValid)
            {
                throw new Exception($"Search failed: {searchResponse.DebugInformation}");
            }

            var items = searchResponse.Documents.ToList();
            var total = searchResponse.Total;

            _logger.LogInformation(
                "Search in {Index} returned {Count}/{Total} results",
                index, items.Count, total);

            return new SearchResult<T>(
                items,
                total,
                query.Page,
                query.PageSize,
                TimeSpan.FromMilliseconds(searchResponse.Took));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search in index {Index}", index);
            throw;
        }
    }

    public async Task DeleteDocumentAsync(string index, string documentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.DeleteAsync<object>(documentId, d => d
                .Index(index), ct);

            if (!response.IsValid && response.Result != Result.NotFound)
            {
                throw new Exception($"Failed to delete document: {response.DebugInformation}");
            }

            _logger.LogInformation("Deleted document {DocumentId} from index {Index}", documentId, index);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocumentId} from {Index}", documentId, index);
            throw;
        }
    }

    public async Task UpdateDocumentAsync<T>(string index, string documentId, T document, CancellationToken ct = default) where T : class
    {
        try
        {
            var response = await _client.UpdateAsync<T, T>(documentId, u => u
                .Index(index)
                .Doc(document), ct);

            if (!response.IsValid)
            {
                throw new Exception($"Failed to update document: {response.DebugInformation}");
            }

            _logger.LogInformation("Updated document {DocumentId} in index {Index}", documentId, index);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update document {DocumentId} in {Index}", documentId, index);
            throw;
        }
    }
}
