using MediatR;
using Modules.Media.Application.Queries;
using Modules.Product.Application.Contracts;

namespace Modules.Product.Infrastructure.Services;

public sealed class MediaUrlResolver(IMediator mediator) : IUrlResolver
{
    private const string DefaultImageSize = "md";
    private readonly IMediator _mediator = mediator;

    public async Task<string?> ResolveAsync(string? path, string? size = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var normalizedSize = string.IsNullOrWhiteSpace(size) ? DefaultImageSize : size.Trim().ToLowerInvariant();
            return await _mediator.Send(new GetMediaUrlByPathQuery(path, normalizedSize), ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}