

namespace Modules.User.Application.Interfaces.Repositories
{
    public interface IUserDbContext
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}