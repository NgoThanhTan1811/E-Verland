using MediatR;
using Microsoft.Extensions.Options;
using Modules.Media.Application.Interfaces;
using Modules.Media.Domain;
using Modules.Media.Infrastructure.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Modules.Media.Application.Queries;

public sealed record GetMediaUrlByPathQuery(string Path, string? Size = null) : IRequest<string?>;

public sealed class GetMediaUrlByPathHandler : IRequestHandler<GetMediaUrlByPathQuery, string?>
{
    private readonly IMediaFileRepository _repository;
    private readonly IMediaStorageService _storageService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MediaOptions _mediaOptions;

    public GetMediaUrlByPathHandler(
        IMediaFileRepository repository,
        IMediaStorageService storageService,
        IHttpClientFactory httpClientFactory,
        IOptions<MediaOptions> mediaOptions)
    {
        _repository = repository;
        _storageService = storageService;
        _httpClientFactory = httpClientFactory;
        _mediaOptions = mediaOptions.Value;
    }

    public async Task<string?> Handle(GetMediaUrlByPathQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return null;

        var mediaFile = await _repository.GetByPathAsync(request.Path, ct);
        if (mediaFile == null)
            return null;

        var normalizedSize = NormalizeSize(request.Size);
        var expiresMinutes = Math.Clamp(_mediaOptions.PresignedUrlExpirationMinutes, 5, 10);

        if (mediaFile.MediaType != MediaType.Image || normalizedSize == "lg")
            return await _storageService.GetPresignedUrlAsync(mediaFile.FilePath, expiresMinutes, ct);

        var variantPath = BuildVariantPath(mediaFile.FilePath, normalizedSize);
        try
        {
            if (!await _storageService.ExistsAsync(variantPath, ct))
            {
                await GenerateVariantAsync(mediaFile, variantPath, normalizedSize, ct);
            }

            return await _storageService.GetPresignedUrlAsync(variantPath, expiresMinutes, ct);
        }
        catch
        {
            return await _storageService.GetPresignedUrlAsync(mediaFile.FilePath, expiresMinutes, ct);
        }
    }

    private async Task GenerateVariantAsync(MediaFile mediaFile, string variantPath, string size, CancellationToken ct)
    {
        var downloadUrl = await _storageService.GetPresignedUrlAsync(mediaFile.FilePath, 5, ct);

        using var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(downloadUrl, ct);
        if (!response.IsSuccessStatusCode)
            return;

        await using var input = await response.Content.ReadAsStreamAsync(ct);
        using var image = await Image.LoadAsync(input, ct);

        var targetWidth = size switch
        {
            "sm" => _mediaOptions.SmWidth,
            "md" => _mediaOptions.MdWidth,
            _ => _mediaOptions.LgWidth
        };

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(targetWidth, 0),
            Mode = ResizeMode.Max
        }));

        await using var output = new MemoryStream();
        var contentType = mediaFile.ContentType.ToLowerInvariant();
        if (contentType == "image/png")
        {
            await image.SaveAsync(output, new PngEncoder(), ct);
        }
        else if (contentType == "image/webp")
        {
            await image.SaveAsync(output, new WebpEncoder(), ct);
        }
        else
        {
            await image.SaveAsync(output, new JpegEncoder { Quality = _mediaOptions.ImageCompressionQuality }, ct);
        }

        output.Position = 0;
        await _storageService.UploadAtPathAsync(output, variantPath, mediaFile.ContentType, ct);
    }

    private static string BuildVariantPath(string originalPath, string size)
    {
        var directory = Path.GetDirectoryName(originalPath)?.Replace('\\', '/');
        var fileName = Path.GetFileName(originalPath);

        if (string.IsNullOrWhiteSpace(directory))
            return $"variants/{size}/{fileName}";

        return $"{directory}/variants/{size}/{fileName}";
    }

    private static string NormalizeSize(string? size)
    {
        var normalized = (size ?? "lg").Trim().ToLowerInvariant();
        return normalized is "sm" or "md" or "lg" ? normalized : "lg";
    }
}