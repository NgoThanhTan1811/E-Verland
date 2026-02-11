namespace SharedKernel.Interfaces.Service;

public interface IService<TDto>
{
    Task<IReadOnlyCollection<TDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task CreateAsync(TDto dto, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, TDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
