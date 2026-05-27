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
        // 1. Thay 'file' bằng 'request.File'
        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 2. Thay 'mediaType' bằng 'request.MediaType'
        if (!Enum.TryParse<MediaType>(request.MediaType, out var parsedMediaType))
            return BadRequest(new { message = "Invalid media type" });

        // 3. Thay 'resourceType' bằng 'request.ResourceType'
        if (!TryParseResourceType(request.ResourceType, out var parsedResourceType))
            return BadRequest(new { message = "Invalid resource type. Allowed: products, avatars, shops, reviews" });

        // 4. Mở stream từ request.File
        using var stream = request.File.OpenReadStream();

        UploadMediaResult result;
        try
        {
            var command = new UploadMediaCommand(
                stream,
                request.File.FileName,     // Thay file -> request.File
                request.File.ContentType,  // Thay file -> request.File
                parsedMediaType,
                parsedResourceType,
                userId);

            result = await _mediator.Send(command, ct);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }

        return CreatedAtAction(
            nameof(GetMediaUrl),
            new { id = result.MediaId },
            new
            {
                id = result.MediaId,
                path = result.FilePath,
                size = result.FileSize
            });
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

    [HttpGet("{id}/url")]
    public async Task<IActionResult> GetMediaUrl(Guid id, [FromQuery] string? size, CancellationToken ct)
    {
        var query = new GetMediaUrlQuery(id, size);
        var url = await _mediator.Send(query, ct);

        if (url == null)
            return NotFound(new { message = "Media not found" });

        return Ok(new { url });
    }

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
