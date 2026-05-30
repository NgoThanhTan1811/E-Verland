namespace Modules.Product.Application.Contracts;

public interface IUrlResolver
{
    Task<string?> ResolveAsync(string? path, string? size = null, CancellationToken ct = default);
}