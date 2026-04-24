namespace Infra.AWS.S3;

/// <summary>
/// Interface for S3 storage operations
/// </summary>
public interface IS3StorageService
{
    /// <summary>
    /// Upload a file to S3
    /// </summary>
    /// <param name="fileStream">File content stream</param>
    /// <param name="key">S3 object key (path)</param>
    /// <param name="contentType">MIME type</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Relative storage path (object key) of the uploaded file</returns>
    Task<string> UploadAsync(Stream fileStream, string key, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Delete a file from S3
    /// </summary>
    /// <param name="key">S3 object key (path)</param>
    /// <param name="ct">Cancellation token</param>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Get a pre-signed URL for accessing a file
    /// </summary>
    /// <param name="key">S3 object key (path)</param>
    /// <param name="expirationMinutes">URL expiration in minutes</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Pre-signed URL</returns>
    Task<string> GetPresignedUrlAsync(string key, int expirationMinutes = 60, CancellationToken ct = default);

    /// <summary>
    /// Check if a file exists in S3
    /// </summary>
    /// <param name="key">S3 object key (path)</param>
    /// <param name="ct">Cancellation token</param>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Get file metadata
    /// </summary>
    /// <param name="key">S3 object key (path)</param>
    /// <param name="ct">Cancellation token</param>
    Task<S3FileMetadata?> GetMetadataAsync(string key, CancellationToken ct = default);
}

/// <summary>
/// S3 file metadata
/// </summary>
public sealed record S3FileMetadata(
    string Key,
    long Size,
    string ContentType,
    DateTime LastModified
);
