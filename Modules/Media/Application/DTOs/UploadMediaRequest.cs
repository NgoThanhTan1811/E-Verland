namespace Modules.Media.Application.DTOs;

public sealed record UploadMediaRequest(
    IFormFile File,
    string MediaType,
    string? ResourceType
);