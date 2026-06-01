namespace Modules.User.Application.Interfaces.Services;

public interface ILocationLookupService
{
    Task<string?> GetProvinceNameAsync(int provinceId, CancellationToken ct = default);
    Task<string?> GetDistrictNameAsync(int provinceId, int districtId, CancellationToken ct = default);
    Task<string?> GetWardNameAsync(int provinceId, int districtId, int wardId, CancellationToken ct = default);
}