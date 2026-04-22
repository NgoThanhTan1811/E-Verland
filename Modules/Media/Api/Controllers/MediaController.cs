using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Media.Application.Commands;
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
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMedia(
        [FromForm] IFormFile file,
        [FromForm] string mediaType,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (!Enum.TryParse<MediaType>(mediaType, out var parsedMediaType))
            return BadRequest(new { message = "Invalid media type" });

        using var stream = file.OpenReadStream();

        UploadMediaResult result;
        try
        {
            var command = new UploadMediaCommand(
                stream,
                file.FileName,
                file.ContentType,
                parsedMediaType,
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GeneratePresignedUploadUrl(
        [FromBody] GeneratePresignedUploadUrlRequest request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var command = new GeneratePresignedUploadUrlCommand(
                request.ResourceType,
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
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMediaUrl(Guid id, [FromQuery] string? size, CancellationToken ct)
    {
        var query = new GetMediaUrlQuery(id, size);
        var url = await _mediator.Send(query, ct);

        if (url == null)
            return NotFound(new { message = "Media not found" });

        return Ok(new { url });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
}

public sealed record GeneratePresignedUploadUrlRequest(
    string ResourceType,
    string ObjectId,
    string FileName,
    string ContentType,
    MediaType MediaType
);
