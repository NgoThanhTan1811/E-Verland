using MediatR;
using Microsoft.Extensions.Options;
using Modules.Media.Application.Interfaces;
using Modules.Media.Domain;
using Modules.Media.Infrastructure.Options;

namespace Modules.Media.Application.Commands;

public sealed record GeneratePresignedUploadUrlCommand(
    string ResourceType,
    string ObjectId,
    string FileName,
    string ContentType,
    MediaType MediaType,
    Guid UploadedBy
) : IRequest<GeneratePresignedUploadUrlResult>;

public sealed record GeneratePresignedUploadUrlResult(
    Guid MediaId,
    string FilePath,
    string PresignedUrl,
    int ExpiresInMinutes
);

public sealed class GeneratePresignedUploadUrlHandler : IRequestHandler<GeneratePresignedUploadUrlCommand, GeneratePresignedUploadUrlResult>
{
    private static readonly HashSet<string> AllowedResourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "products",
        "avatars",
        "shops",
        "reviews"
    };

    private readonly IMediaStorageService _storageService;
    private readonly IMediaFileRepository _repository;
    private readonly MediaOptions _mediaOptions;

    public GeneratePresignedUploadUrlHandler(
        IMediaStorageService storageService,
        IMediaFileRepository repository,
        IOptions<MediaOptions> mediaOptions)
    {
        _storageService = storageService;
        _repository = repository;
        _mediaOptions = mediaOptions.Value;
    }

    public async Task<GeneratePresignedUploadUrlResult> Handle(GeneratePresignedUploadUrlCommand request, CancellationToken ct)
    {
        if (!AllowedResourceTypes.Contains(request.ResourceType))
            throw new InvalidOperationException("Invalid resource type. Allowed: products, avatars, shops, reviews.");

        var inferredType = InferMediaType(request.ContentType);
        if (inferredType != request.MediaType)
            throw new InvalidOperationException("Declared mediaType does not match ContentType.");

        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            throw new InvalidOperationException("FileName must include extension.");

        var key = $"{request.ResourceType}/{request.ObjectId}/{DateTime.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}{extension}";
        var expiresMinutes = Math.Clamp(_mediaOptions.PresignedUrlExpirationMinutes, 5, 10);
        var url = await _storageService.GetPresignedUrlAsync(key, expiresMinutes, ct);

        var pendingUpload = new MediaFile
        {
            FileName = request.FileName,
            FilePath = key,
            FileSize = 0,
            ContentType = request.ContentType,
            MediaType = inferredType,
            UploadedBy = request.UploadedBy,
            UploadedAt = DateTime.UtcNow,
            Status = MediaFileStatus.Pending
        };

        await _repository.AddAsync(pendingUpload, ct);

        return new GeneratePresignedUploadUrlResult(
            pendingUpload.Id,
            key,
            url,
            expiresMinutes);
    }

    private static MediaType InferMediaType(string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return MediaType.Image;

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            return MediaType.Video;

        throw new InvalidOperationException("Unsupported content type. Only image/* or video/* are accepted.");
    }
}
