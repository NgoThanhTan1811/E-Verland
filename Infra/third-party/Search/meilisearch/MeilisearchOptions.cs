namespace Infra.Meilisearch;

/// <summary>
/// Configuration options for Meilisearch
/// </summary>
public sealed class MeilisearchOptions
{
    public const string SectionName = "AWS:Meilisearch";

    /// <summary>
    /// Meilisearch API endpoint (e.g., http://localhost:7700 or https://your-instance.meilisearch.com)
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:7700";

    /// <summary>
    /// Meilisearch master key for API authentication
    /// </summary>
    public string MasterKey { get; set; } = string.Empty;

    /// <summary>
    /// Default index name for products
    /// </summary>
    public string IndexName { get; set; } = "products";

    /// <summary>
    /// Timeout in seconds for API requests
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}
