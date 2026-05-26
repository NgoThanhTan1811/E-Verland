using Infra.AWS.CloudWatch;
using Infra.AWS.Storage;
using Microsoft.Extensions.Options;
using Modules.Media.Application.Interfaces;
using Modules.Media.Domain;
using Modules.Media.Infrastructure.Options;

namespace Modules.Media.Infrastructure.Services;

/// <summary>
/// Media storage service that wraps S3StorageService from Infra layer
/// </summary>
public class MediaStorageService : IMediaStorageService
{
    private readonly IStorageService _storageService;
    private readonly ICloudWatchService _cloudWatch;
    private readonly StorageOptions _storageOptions;

    public MediaStorageService(
        IStorageService storageService,
        ICloudWatchService cloudWatch,
        IOptions<StorageOptions> storageOptions,
        IOptions<MediaOptions> mediaOptions)
    {
        _storageService = storageService;
        _cloudWatch = cloudWatch;
        _storageOptions = storageOptions.Value;
        _ = mediaOptions.Value;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        var objectId = Guid.NewGuid().ToString("N");
        var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}{extension}";
        var pathPrefix = GetPathPrefix(contentType);
        var key = $"{pathPrefix}/{objectId}/{uniqueFileName}";

        try
        {
            var result = await _storageService.UploadAsync(fileStream, key, contentType, ct);

            await _cloudWatch.PutMetricAsync("media.upload.success", 1, "Count", ct: ct);

            var sizeBytes = fileStream.CanSeek ? fileStream.Length : 0;
            await _cloudWatch.PutMetricAsync("media.upload.size_bytes", sizeBytes, "Bytes", ct: ct);

            return result;
        }
        catch
        {
            await _cloudWatch.PutMetricAsync("media.upload.failed", 1, "Count", ct: ct);
            throw;
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, MediaResourceType resourceType, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        var objectId = Guid.NewGuid().ToString("N");
        var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}{extension}";
        var pathPrefix = GetPathPrefix(resourceType);
        var key = $"{pathPrefix}/{objectId}/{uniqueFileName}";

        try
        {
            var result = await _storageService.UploadAsync(fileStream, key, contentType, ct);
            await _cloudWatch.PutMetricAsync("media.upload.success", 1, "Count", ct: ct);
            return result;
        }
        catch
        {
            await _cloudWatch.PutMetricAsync("media.upload.failed", 1, "Count", ct: ct);
            throw;
        }
    }

    public async Task<string> UploadAtPathAsync(Stream fileStream, string filePath, string contentType, CancellationToken ct = default)
    {
        var result = await _storageService.UploadAsync(fileStream, filePath, contentType, ct);
        await _cloudWatch.PutMetricAsync("media.upload.success", 1, "Count", ct: ct);
        return result;
    }

    public async Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        await _storageService.DeleteAsync(filePath, ct);

        await _cloudWatch.PutMetricAsync("media.delete", 1, "Count", ct: ct);
    }

    public async Task<string> GetPresignedUrlAsync(string filePath, int expirationMinutes = 60, CancellationToken ct = default)
    {
        return await _storageService.GetPresignedUrlAsync(filePath, expirationMinutes, ct);
    }

    public async Task<bool> ExistsAsync(string filePath, CancellationToken ct = default)
    {
        return await _storageService.ExistsAsync(filePath, ct);
    }

    private string GetPathPrefix(string contentType)
    {
        var ct = contentType.ToLowerInvariant();

        if (ct.StartsWith("image/"))
            return _storageOptions.AvatarsPrefix;

        if (ct.StartsWith("video/"))
            return _storageOptions.ProductsPrefix;

        return _storageOptions.ReviewsPrefix;
    }

    private string GetPathPrefix(MediaResourceType resourceType)
    {
        return resourceType switch
        {
            MediaResourceType.Products => _storageOptions.ProductsPrefix,
            MediaResourceType.Avatars => _storageOptions.AvatarsPrefix,
            MediaResourceType.Shops => _storageOptions.ShopsPrefix,
            MediaResourceType.Reviews => _storageOptions.ReviewsPrefix,
            _ => _storageOptions.ProductsPrefix
        };
    }
}
