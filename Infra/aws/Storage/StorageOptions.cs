namespace Infra.AWS.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "S3";
    public string BaseUrl { get; set; } = string.Empty;

    public string ProductsPrefix { get; set; } = "products";
    public string AvatarsPrefix { get; set; } = "avatars";
    public string ShopsPrefix { get; set; } = "shops";
    public string ReviewsPrefix { get; set; } = "reviews";
}
