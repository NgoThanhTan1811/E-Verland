namespace Infra.AWS.OpenSearch;

/// <summary>
/// AWS OpenSearch configuration options
/// </summary>
public sealed class OpenSearchOptions
{
    public const string SectionName = "AWS:OpenSearch";

    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = "ap-southeast-1";

    // Credentials
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Indices
    public string ProductsIndex { get; set; } = "products";
    public string OrdersIndex { get; set; } = "orders";
    public string UsersIndex { get; set; } = "users";

    // Search settings
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
}
