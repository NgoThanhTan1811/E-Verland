using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infra.AWS.Resilience;

namespace Infra.AWS.Storage.MinIO;

public sealed class MinIOStorageService : IStorageService
{
    private readonly IAmazonS3 _client;
    private readonly MinIOOptions _options;
    private readonly ILogger<MinIOStorageService> _logger;

    public MinIOStorageService(IOptions<MinIOOptions> options, ILogger<MinIOStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.Endpoint,
            ForcePathStyle = true,
            UseHttp = !_options.UseSsl
        };

        var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
        _client = new AmazonS3Client(credentials, config);
    }

    public async Task<string> UploadAsync(Stream fileStream, string key, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            CannedACL = S3CannedACL.Private
        };

        await AwsRetryPolicy.ExecuteAsync(() => _client.PutObjectAsync(request, ct), 3, ct);
        _logger.LogInformation("Uploaded object to MinIO. {Key}", key);
        return key;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key
        };

        await AwsRetryPolicy.ExecuteAsync(() => _client.DeleteObjectAsync(request, ct), 3, ct);
    }

    public Task<string> GetPresignedUrlAsync(string key, int expirationMinutes = 60, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };

        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(url);
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

            await AwsRetryPolicy.ExecuteAsync(() => _client.GetObjectMetadataAsync(request, ct), 3, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
