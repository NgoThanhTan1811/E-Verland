namespace Infra.AWS.OpenSearch;

/// <summary>
/// Interface for AWS OpenSearch operations
/// </summary>
public interface IOpenSearchService
{
    /// <summary>
    /// Index a document
    /// </summary>
    Task<string> IndexDocumentAsync<T>(string index, string documentId, T document, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Bulk index documents
    /// </summary>
    Task<int> BulkIndexAsync<T>(string index, List<(string id, T document)> documents, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Search documents
    /// </summary>
    Task<SearchResult<T>> SearchAsync<T>(string index, SearchQuery query, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Delete a document
    /// </summary>
    Task DeleteDocumentAsync(string index, string documentId, CancellationToken ct = default);

    /// <summary>
    /// Update a document
    /// </summary>
    Task UpdateDocumentAsync<T>(string index, string documentId, T document, CancellationToken ct = default) where T : class;
}

/// <summary>
/// Search query builder
/// </summary>
public sealed record SearchQuery(
    string QueryText,
    int Page = 1,
    int PageSize = 20,
    List<string>? Fields = null,
    Dictionary<string, object>? Filters = null,
    string? SortBy = null,
    bool SortDescending = true
);

/// <summary>
/// Search result wrapper
/// </summary>
public sealed record SearchResult<T>(
    List<T> Items,
    long TotalCount,
    int Page,
    int PageSize,
    TimeSpan took
) where T : class;
