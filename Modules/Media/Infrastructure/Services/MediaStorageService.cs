using Infra.AWS.CloudWatch;
using Infra.AWS.S3;
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
    private readonly IS3StorageService _s3Service;
    private readonly ICloudWatchService _cloudWatch;
    private readonly S3Options _s3Options;
    private readonly MediaOptions _mediaOptions;

    public MediaStorageService(
        IS3StorageService s3Service,
        ICloudWatchService cloudWatch,
        IOptions<S3Options> s3Options,
        IOptions<MediaOptions> mediaOptions)
    {
        _s3Service = s3Service;
        _cloudWatch = cloudWatch;
        _s3Options = s3Options.Value;
        _mediaOptions = mediaOptions.Value;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        // Generate unique file path
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var pathPrefix = GetPathPrefix(contentType);
        var key = $"{pathPrefix}/{uniqueFileName}";

        try
        {
            var result = await _s3Service.UploadAsync(fileStream, key, contentType, ct);

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

    public async Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        // Extract S3 key from full URL
        var key = ExtractKeyFromUrl(filePath);
        await _s3Service.DeleteAsync(key, ct);

        await _cloudWatch.PutMetricAsync("media.delete", 1, "Count", ct: ct);
    }

    public async Task<string> GetPresignedUrlAsync(string filePath, int expirationMinutes = 60, CancellationToken ct = default)
    {
        var key = ExtractKeyFromUrl(filePath);
        return await _s3Service.GetPresignedUrlAsync(key, expirationMinutes, ct);
    }

    public async Task<bool> ExistsAsync(string filePath, CancellationToken ct = default)
    {
        var key = ExtractKeyFromUrl(filePath);
        return await _s3Service.ExistsAsync(key, ct);
    }

    private string GetPathPrefix(string contentType)
    {
        return contentType.ToLower() switch
        {
            var ct when ct.StartsWith("image/") => _s3Options.ProductImagePathPrefix,
            var ct when ct.StartsWith("video/") => _s3Options.ProductVideoPathPrefix,
            _ => "media"
        };
    }

    private string ExtractKeyFromUrl(string urlOrKey)
    {
        // If it's already a key (no http), return as is
        if (!urlOrKey.StartsWith("http"))
            return urlOrKey;

        // Extract key from full URL
        var uri = new Uri(urlOrKey);
        return uri.AbsolutePath.TrimStart('/');
    }
}
