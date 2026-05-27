using MediatR;
using Modules.Media.Application.Interfaces;

namespace Modules.Media.Application.Commands;

public sealed record DeleteMediaCommand(Guid MediaId, Guid UserId) : IRequest<bool>;

public sealed class DeleteMediaHandler : IRequestHandler<DeleteMediaCommand, bool>
{
    private readonly IMediaFileRepository _repository;
    private readonly IMediaStorageService _storageService;

    public DeleteMediaHandler(IMediaFileRepository repository, IMediaStorageService storageService)
    {
        _repository = repository;
        _storageService = storageService;
    }

    public async Task<bool> Handle(DeleteMediaCommand request, CancellationToken ct)
    {
        var mediaFile = await _repository.GetByIdAsync(request.MediaId, ct);

        if (mediaFile == null)
            return false;

        // Check ownership
        if (mediaFile.UploadedBy != request.UserId)
            throw new UnauthorizedAccessException("You can only delete your own media files");

        // Delete from S3
        await _storageService.DeleteAsync(mediaFile.FilePath, ct);

        // Soft delete from database
        await _repository.DeleteAsync(request.MediaId, ct);

        return true;
    }
}
