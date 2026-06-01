namespace Modules.Media.Application.DTOs;

public sealed record UploadMediaRequest(
    List<IFormFile> Files,
    string MediaType,
    string? ResourceType
);