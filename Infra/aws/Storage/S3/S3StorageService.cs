using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Infra.AWS.S3;

/// <summary>
/// AWS S3 storage service implementation
/// </summary>
public sealed class S3StorageService : IS3StorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _options;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(
        IAmazonS3 s3Client,
        IOptions<S3Options> options,
        ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string key, string contentType, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Uploading file to S3: {Key}", key);

            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.Private // Files are private by default
            };

            var response = await _s3Client.PutObjectAsync(putRequest, ct);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Failed to upload file to S3. Status: {response.HttpStatusCode}");
            }

            var url = $"{_options.BaseUrl.TrimEnd('/')}/{key}";

            _logger.LogInformation("File uploaded successfully to S3: {Key}, URL: {Url}", key, url);

            return url;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error while uploading file: {Key}", key);
            throw new Exception($"S3 upload failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while uploading file to S3: {Key}", key);
            throw;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Deleting file from S3: {Key}", key);

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, ct);

            _logger.LogInformation("File deleted successfully from S3: {Key}", key);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error while deleting file: {Key}", key);
            throw new Exception($"S3 delete failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting file from S3: {Key}", key);
            throw;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string key, int expirationMinutes = 60, CancellationToken ct = default)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };

            var url = await Task.FromResult(_s3Client.GetPreSignedURL(request));

            _logger.LogDebug("Generated pre-signed URL for {Key}, expires in {Minutes} minutes", key, expirationMinutes);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-signed URL for: {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists in S3: {Key}", key);
            throw;
        }
    }

    public async Task<S3FileMetadata?> GetMetadataAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, ct);

            return new S3FileMetadata(
                key,
                response.ContentLength,
                response.Headers.ContentType,
                response.LastModified ?? DateTime.UtcNow
            );
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for file in S3: {Key}", key);
            throw;
        }
    }
}
