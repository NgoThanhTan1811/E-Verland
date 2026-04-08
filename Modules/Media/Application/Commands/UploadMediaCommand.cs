using MediatR;
using Modules.Media.Application.Interfaces;
using Modules.Media.Domain;

namespace Modules.Media.Application.Commands;

public sealed record UploadMediaCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    MediaType MediaType,
    Guid UploadedBy
) : IRequest<UploadMediaResult>;

public sealed record UploadMediaResult(
    Guid MediaId,
    string FileUrl,
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
        // Upload to S3
        var fileUrl = await _storageService.UploadAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            ct);

        // Save metadata to database
        var mediaFile = new MediaFile
        {
            FileName = request.FileName,
            FilePath = fileUrl,
            FileSize = request.FileStream.Length,
            ContentType = request.ContentType,
            MediaType = request.MediaType,
            UploadedBy = request.UploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(mediaFile, ct);

        return new UploadMediaResult(
            mediaFile.Id,
            fileUrl,
            mediaFile.FileSize);
    }
}
