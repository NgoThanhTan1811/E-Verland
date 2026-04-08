using MediatR;
using Modules.Media.Application.Interfaces;

namespace Modules.Media.Application.Queries;

public sealed record GetMediaUrlQuery(Guid MediaId) : IRequest<string?>;

public sealed class GetMediaUrlHandler : IRequestHandler<GetMediaUrlQuery, string?>
{
    private readonly IMediaFileRepository _repository;
    private readonly IMediaStorageService _storageService;

    public GetMediaUrlHandler(IMediaFileRepository repository, IMediaStorageService storageService)
    {
        _repository = repository;
        _storageService = storageService;
    }

    public async Task<string?> Handle(GetMediaUrlQuery request, CancellationToken ct)
    {
        var mediaFile = await _repository.GetByIdAsync(request.MediaId, ct);

        if (mediaFile == null)
            return null;

        // Return pre-signed URL for secure access
        return await _storageService.GetPresignedUrlAsync(mediaFile.FilePath, 60, ct);
    }
}
