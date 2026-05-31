using Modules.Media.Domain;

namespace Modules.Media.Application.Interfaces;

/// <summary>
/// Repository for MediaFile entity
/// </summary>
public interface IMediaFileRepository
{
    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MediaFile?> GetByPathAsync(string filePath, CancellationToken ct = default);
    Task<List<MediaFile>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<List<MediaFile>> GetByTypeAsync(MediaType type, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<List<MediaFile>> GetPendingOlderThanAsync(DateTime olderThanUtc, CancellationToken ct = default);
    Task<MediaFile> AddAsync(MediaFile mediaFile, CancellationToken ct = default);
    Task ConfirmByPathAsync(string filePath, CancellationToken ct = default);
    Task UpdateAsync(MediaFile mediaFile, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsByPathAsync(string path, CancellationToken ct = default);
}
