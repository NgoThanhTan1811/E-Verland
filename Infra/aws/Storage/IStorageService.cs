namespace Infra.AWS.Storage;

public interface IStorageService
{
    Task<string> UploadAsync(Stream fileStream, string key, string contentType, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);

    Task<string> GetPresignedUrlAsync(string key, int expirationMinutes = 60, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
