using MediatR;
using Infra.AWS.S3;
using Microsoft.Extensions.Options;

namespace Modules.Media.Application.Queries;

public sealed record GetMediaUrlByPathQuery(string Path, string? Size = null) : IRequest<string?>;

public sealed class GetMediaUrlByPathHandler : IRequestHandler<GetMediaUrlByPathQuery, string?>
{
    private readonly string _publicBaseUrl;
    private readonly string _productImagePrefix;

    public GetMediaUrlByPathHandler(
        IOptions<S3Options> s3Options)
    {
        var s3 = s3Options.Value;
        _publicBaseUrl = string.IsNullOrWhiteSpace(s3.BaseUrl)
            ? "https://media.e-verland.site"
            : s3.BaseUrl;
        _productImagePrefix = string.IsNullOrWhiteSpace(s3.ProductImagePathPrefix)
            ? "products/image"
            : s3.ProductImagePathPrefix.Trim('/');
    }

    // public async Task<string?> Handle(GetMediaUrlByPathQuery request, CancellationToken ct)
    // {
    //     if (string.IsNullOrWhiteSpace(request.Path))
    //         return null;

    //     var mediaFile = await _repository.GetByPathAsync(request.Path, ct);
    //     if (mediaFile == null)
    //         return null;

    //     var normalizedSize = NormalizeSize(request.Size);
    //     var expiresMinutes = Math.Clamp(_mediaOptions.PresignedUrlExpirationMinutes, 5, 10);

    //     if (mediaFile.MediaType != MediaType.Image || normalizedSize == "lg")
    //         return await _storageService.GetPresignedUrlAsync(mediaFile.FilePath, expiresMinutes, ct);

    //     var variantPath = BuildVariantPath(mediaFile.FilePath, normalizedSize);
    //     try
    //     {
    //         if (!await _storageService.ExistsAsync(variantPath, ct))
    //         {
    //             await GenerateVariantAsync(mediaFile, variantPath, normalizedSize, ct);
    //         }

    //         return await _storageService.GetPresignedUrlAsync(variantPath, expiresMinutes, ct);
    //     }
    //     catch
    //     {
    //         return await _storageService.GetPresignedUrlAsync(mediaFile.FilePath, expiresMinutes, ct);
    //     }
    // }

    public Task<string?> Handle(GetMediaUrlByPathQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return Task.FromResult<string?>(null);

        return Task.FromResult<string?>(BuildPublicUrl(NormalizePublicPath(request.Path)));
    }

    private string NormalizePublicPath(string path)
    {
        var normalizedPath = path.Trim().Replace('\\', '/').TrimStart('/');

        if (Uri.TryCreate(normalizedPath, UriKind.Absolute, out var absoluteUri))
        {
            normalizedPath = absoluteUri.AbsolutePath.TrimStart('/');
        }

        if (normalizedPath.StartsWith($"{_productImagePrefix}/", StringComparison.OrdinalIgnoreCase))
            return normalizedPath;

        return $"{_productImagePrefix}/{normalizedPath}";
    }

    private string BuildPublicUrl(string path)
    {
        return $"{_publicBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

}