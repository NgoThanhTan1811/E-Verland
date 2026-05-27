namespace Modules.Media.Application.Interfaces;

using Modules.Media.Domain;

/// <summary>
/// Interface for media storage operations (wraps S3 or other storage providers)
/// </summary>
public interface IMediaStorageService
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Upload a file to storage by explicit resource type.
    /// </summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, MediaResourceType resourceType, CancellationToken ct = default);

    /// <summary>
    /// Upload a file to an explicit relative path.
    /// </summary>
    Task<string> UploadAtPathAsync(Stream fileStream, string filePath, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task DeleteAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Get a pre-signed URL for accessing a file
    /// </summary>
    Task<string> GetPresignedUrlAsync(string filePath, int expirationMinutes = 60, CancellationToken ct = default);

    /// <summary>
    /// Check if a file exists in storage
    /// </summary>
    Task<bool> ExistsAsync(string filePath, CancellationToken ct = default);
}
