namespace Modules.Media.Domain;

/// <summary>
/// Represents a media file uploaded to the system (images, videos)
/// </summary>
public class MediaFile : SharedKernel.Entities.BaseEntity
{
    public string FileName { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = default!;
    public MediaType MediaType { get; set; }

    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public MediaFileStatus Status { get; set; } = MediaFileStatus.Pending;

    // Optional metadata
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationSeconds { get; set; } // For videos
}

/// <summary>
/// Type of media file
/// </summary>
public enum MediaType
{
    Image,
    Video
}

public enum MediaFileStatus
{
    Pending,
    Confirmed,
    Orphan
}
