using MediatR;
using Modules.Media.Application.Interfaces;
using Modules.Media.Domain;

namespace Modules.Media.Application.Commands;

public sealed record UploadMediaCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    MediaType MediaType,
    MediaResourceType ResourceType,
    Guid UploadedBy
) : IRequest<UploadMediaResult>;

public sealed record UploadMediaResult(
    Guid MediaId,
    string FilePath,
    long FileSize
);

public sealed class UploadMediaHandler : IRequestHandler<UploadMediaCommand, UploadMediaResult>
{
    private readonly IMediaStorageService _storageService;
    private readonly IMediaFileRepository _repository;

    public UploadMediaHandler(IMediaStorageService storageService, IMediaFileRepository repository)
    {
        _storageService = storageService;
        _repository = repository;
    }

    public async Task<UploadMediaResult> Handle(UploadMediaCommand request, CancellationToken ct)
    {
        var inferredType = InferMediaType(request.ContentType);
        if (inferredType != request.MediaType)
            throw new InvalidOperationException("Declared mediaType does not match uploaded file ContentType.");

        var filePath = await _storageService.UploadAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.ResourceType,
            ct);

        // Save metadata to database
        var mediaFile = new MediaFile
        {
            FileName = request.FileName,
            FilePath = filePath,
            FileSize = request.FileStream.Length,
            ContentType = request.ContentType,
            MediaType = inferredType,
            UploadedBy = request.UploadedBy,
            UploadedAt = DateTime.UtcNow,
            Status = MediaFileStatus.Confirmed
        };

        await _repository.AddAsync(mediaFile, ct);

        return new UploadMediaResult(
            mediaFile.Id,
            filePath,
            mediaFile.FileSize);
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
