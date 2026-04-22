namespace Modules.Media.Infrastructure.Options;

public sealed class MediaOptions
{
    public const string SectionName = "Media";

    // Image constraints
    public long MaxImageSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
    public int MaxImageWidth { get; set; } = 4096;
    public int MaxImageHeight { get; set; } = 4096;
    public string[] AllowedImageFormats { get; set; } = { "image/jpeg", "image/png", "image/webp" };
    public int ImageCompressionQuality { get; set; } = 85;

    // Video constraints
    public long MaxVideoSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB
    public int MaxVideoDurationSeconds { get; set; } = 120; // 2 minutes
    public string[] AllowedVideoFormats { get; set; } = { "video/mp4", "video/webm", "video/quicktime" };

    // Upload settings
    public int MaxFilesPerRequest { get; set; } = 5;
    public bool EnableThumbnailGeneration { get; set; } = true;
    public int PresignedUrlExpirationMinutes { get; set; } = 10;

    // Responsive image breakpoints
    public int SmWidth { get; set; } = 480;
    public int MdWidth { get; set; } = 1024;
    public int LgWidth { get; set; } = 1920;

    // Daily cleanup for orphan pending uploads
    public int OrphanGracePeriodHours { get; set; } = 24;
    public int CleanupIntervalHours { get; set; } = 24;
}
