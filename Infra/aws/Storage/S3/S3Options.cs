namespace Infra.AWS.S3;

/// <summary>
/// Configuration options for AWS S3
/// </summary>
public sealed class S3Options
{
    public const string SectionName = "AWS:S3";

    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; } = true;
    public string BaseUrl { get; set; } = string.Empty; // CloudFront or S3 URL

    // Path prefixes for different media types
    public string AvatarPathPrefix { get; set; } = "avatars";
    public string ProductImagePathPrefix { get; set; } = "products/images";
    public string ProductVideoPathPrefix { get; set; } = "products/videos";
    public string ShopLogoPathPrefix { get; set; } = "shops/logos";
    public string ShopBannerPathPrefix { get; set; } = "shops/banners";

    // URL expiration
    public int PreSignedUrlExpirationMinutes { get; set; } = 60;
}
