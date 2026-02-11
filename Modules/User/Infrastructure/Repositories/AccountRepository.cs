using Microsoft.EntityFrameworkCore;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.DTOs.Request;
using Modules.User.Domain.Entities;
using Modules.User.Infrastructure.Persistence;

namespace Modules.User.Infrastructure;

public class AccountRepository : IAccountRepository
{
    private readonly UserDbContext _db;
    public AccountRepository(UserDbContext db) => _db = db;


    public async Task CreateAsync(Account entity, CancellationToken cancellationToken = default)
    {
        await _db.Accounts.AddAsync(entity, cancellationToken);

    }

    public async Task<PageResult<Account>> SearchAsync(AccountFilter filter, CancellationToken ct)
    {
        var query = _db.Accounts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim().ToUpperInvariant();
            query = query.Where(a =>
                a.NormalizedUsername.Contains(keyword) ||
                a.NormalizedEmail.Contains(keyword)
            );
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == filter.Status.Value);
        }

        if (filter.Role.HasValue)
        {
            query = query.Where(a => a.Role == filter.Role.Value);
        }

        var totalItems = await query.CountAsync(ct);

        var page = filter.Page.GetValueOrDefault(1);
        var limit = filter.Limit.GetValueOrDefault(20);
        var skip = (page - 1) * limit;

        var items = await query
            .OrderBy(a => a.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync(ct);

        return new PageResult<Account>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = page,
            Limit = limit
        };
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToUpperInvariant();

        return await _db.Accounts.AnyAsync(a => a.NormalizedEmail == normalized, ct);

    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalized = username.Trim().ToUpperInvariant();

        return await _db.Accounts.AnyAsync(a => a.NormalizedUsername == normalized, ct);
    }

    public async Task<IReadOnlyCollection<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _db.Accounts.AsNoTracking().ToListAsync(cancellationToken);
        return accounts;
    }

    public async Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.NormalizedEmail == normalized, ct);
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Account?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalized = username.Trim().ToUpperInvariant();
        return await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.NormalizedUsername == normalized, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid byId, CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account == null) return false;

        return true;
    }

    public Task UpdateAsync(Account entity, CancellationToken cancellationToken = default)
    {
        _db.Accounts.Update(entity);
        return Task.CompletedTask;
    }
}
