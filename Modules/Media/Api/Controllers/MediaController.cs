using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Media.Application.Commands;
using Modules.Media.Application.DTOs;
using Modules.Media.Application.Queries;
using Modules.Media.Domain;
using System.Security.Claims;

namespace Modules.Media.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload")]
    [Authorize(Policy = "SellerPolicy")]
    [RequestSizeLimit(52428800)] // 50MB
    [Consumes("multipart/form-data")] // Vẫn nên giữ kèm dòng này
    public async Task<IActionResult> UploadMedia(
         [FromForm] UploadMediaRequest request, // Gom lại thành 1 object
         CancellationToken ct)
    {
        if (request.Files == null || request.Files.Count == 0)
            return BadRequest(new { message = "No file uploaded" });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (!TryParseResourceType(request.ResourceType, out var parsedResourceType))
            return BadRequest(new { message = "Invalid resource type. Allowed: products, avatars, shops, reviews" });

        var results = new List<object>();

        foreach (var file in request.Files)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "One or more files are empty" });

            if (!TryParseMediaType(request.MediaType, file.ContentType, out var parsedMediaType))
                return BadRequest(new { message = "Invalid media type" });

            using var stream = file.OpenReadStream();

            try
            {
                var command = new UploadMediaCommand(
                    stream,
                    file.FileName,
                    file.ContentType,
                    parsedMediaType,
                    parsedResourceType,
                    userId);

                var result = await _mediator.Send(command, ct);
                results.Add(new
                {
                    id = result.MediaId,
                    path = result.FilePath,
                    size = result.FileSize,
                    fileName = file.FileName
                });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { message = ex.Message });
            }
        }

        return CreatedAtAction(nameof(GetMediaUrl), new { id = results.Count == 1 ? ((dynamic)results[0]).id : Guid.Empty }, results);
    }

    private static bool TryParseMediaType(string? value, string fileContentType, out MediaType mediaType)
    {
        mediaType = MediaType.Image;

        if (!string.IsNullOrWhiteSpace(value))
        {
            // Try enum name first
            if (Enum.TryParse<MediaType>(value, true, out var parsed))
            {
                mediaType = parsed;
                return true;
            }

            // Accept full content-type strings like "image/jpeg" or "video/mp4"
            if (value.Contains('/'))
            {
                if (value.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    mediaType = MediaType.Image;
                    return true;
                }
                if (value.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                {
                    mediaType = MediaType.Video;
                    return true;
                }
            }
        }

        // Fall back to uploaded file's ContentType
        if (!string.IsNullOrWhiteSpace(fileContentType))
        {
            if (fileContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                mediaType = MediaType.Image;
                return true;
            }
            if (fileContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                mediaType = MediaType.Video;
                return true;
            }
        }

        return false;
    }

    [HttpPost("presigned-upload")]
    [Authorize(Policy = "SellerPolicy")]
    public async Task<IActionResult> GeneratePresignedUploadUrl(
        [FromBody] GeneratePresignedUploadUrlRequest request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (!TryParseResourceType(request.ResourceType, out var parsedResourceType))
            return UnprocessableEntity(new { message = "Invalid resource type. Allowed: products, avatars, shops, reviews" });

        try
        {
            var command = new GeneratePresignedUploadUrlCommand(
                parsedResourceType,
                request.ObjectId,
                request.FileName,
                request.ContentType,
                request.MediaType,
                userId);

            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}/url")]
    public async Task<IActionResult> GetMediaUrl(Guid id, [FromQuery] string? size, CancellationToken ct)
    {
        var query = new GetMediaUrlQuery(id, size);
        var url = await _mediator.Send(query, ct);

        if (url == null)
            return NotFound(new { message = "Media not found" });

        return Ok(new { url });
    }

    [AllowAnonymous]
    [HttpGet("url")]
    public async Task<IActionResult> GetMediaUrlByPath([FromQuery] string path, [FromQuery] string? size, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { message = "Path is required" });

        var query = new GetMediaUrlByPathQuery(path, size);
        var url = await _mediator.Send(query, ct);

        if (url == null)
            return NotFound(new { message = "Media not found" });

        return Ok(new { url });
    }

    [Authorize(Policy = "AdminOrSeller")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedia(Guid id, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var command = new DeleteMediaCommand(id, userId);
            var result = await _mediator.Send(command, ct);

            if (!result)
                return NotFound(new { message = "Media not found" });

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    private static bool TryParseResourceType(string? value, out MediaResourceType resourceType)
    {
        resourceType = MediaResourceType.Products;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return Enum.TryParse(value, true, out resourceType);
    }
}

public sealed record GeneratePresignedUploadUrlRequest(
    string ResourceType,
    string ObjectId,
    string FileName,
    string ContentType,
    MediaType MediaType
);
