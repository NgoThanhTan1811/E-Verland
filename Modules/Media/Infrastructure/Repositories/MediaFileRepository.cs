using Microsoft.EntityFrameworkCore;
using Modules.Media.Application.Interfaces;
using Modules.Media.Domain;
using Modules.Media.Infrastructure.Persistence;

namespace Modules.Media.Infrastructure.Repositories;

public class MediaFileRepository : IMediaFileRepository
{
    private readonly MediaDbContext _context;

    public MediaFileRepository(MediaDbContext context)
    {
        _context = context;
    }

    public async Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.MediaFiles
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
    }

    public async Task<MediaFile?> GetByPathAsync(string filePath, CancellationToken ct = default)
    {
        return await _context.MediaFiles
            .FirstOrDefaultAsync(m => m.FilePath == filePath && !m.IsDeleted, ct);
    }

    public async Task<List<MediaFile>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        return await _context.MediaFiles
            .Where(m => m.UploadedBy == userId && !m.IsDeleted)
            .OrderByDescending(m => m.UploadedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<List<MediaFile>> GetByTypeAsync(MediaType type, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        return await _context.MediaFiles
            .Where(m => m.MediaType == type && !m.IsDeleted)
            .OrderByDescending(m => m.UploadedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<List<MediaFile>> GetPendingOlderThanAsync(DateTime olderThanUtc, CancellationToken ct = default)
    {
        return await _context.MediaFiles
            .Where(m => !m.IsDeleted && m.Status == MediaFileStatus.Pending && m.UploadedAt <= olderThanUtc)
            .OrderBy(m => m.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task<MediaFile> AddAsync(MediaFile mediaFile, CancellationToken ct = default)
    {
        await _context.MediaFiles.AddAsync(mediaFile, ct);
        await _context.SaveChangesAsync(ct);
        return mediaFile;
    }

    public async Task ConfirmByPathAsync(string filePath, CancellationToken ct = default)
    {
        var mediaFile = await _context.MediaFiles
            .FirstOrDefaultAsync(m => m.FilePath == filePath && !m.IsDeleted, ct);

        if (mediaFile == null)
        {
            return;
        }

        mediaFile.Status = MediaFileStatus.Confirmed;
        _context.Entry(mediaFile).Property(nameof(MediaFile.UpdatedAt)).CurrentValue = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(MediaFile mediaFile, CancellationToken ct = default)
    {
        _context.MediaFiles.Update(mediaFile);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var mediaFile = await GetByIdAsync(id, ct);
        if (mediaFile != null)
        {
            _context.Entry(mediaFile).Property(nameof(MediaFile.IsDeleted)).CurrentValue = true;
            _context.Entry(mediaFile).Property(nameof(MediaFile.DeletedAt)).CurrentValue = DateTime.UtcNow;
            _context.Entry(mediaFile).Property(nameof(MediaFile.UpdatedAt)).CurrentValue = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.MediaFiles
            .CountAsync(m => m.UploadedBy == userId && !m.IsDeleted, ct);
    }
}
