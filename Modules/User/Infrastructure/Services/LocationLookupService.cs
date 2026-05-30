using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Modules.User.Application.Interfaces.Services;

namespace Modules.User.Infrastructure.Services;

public sealed class LocationLookupService : ILocationLookupService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<int, ProvinceLocation> _provinces;
    private readonly Dictionary<int, DistrictLocation> _districts;
    private readonly Dictionary<int, WardLocation> _wards;

    public LocationLookupService(IHostEnvironment hostEnvironment)
    {
        var locationsPath = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "..", "SharedKernel", "Locations", "lib"));

        _provinces = LoadJson<ProvinceLocation>(Path.Combine(locationsPath, "provinces.json"))
            .ToDictionary(x => x.Id);
        _districts = LoadJson<DistrictLocation>(Path.Combine(locationsPath, "districts.json"))
            .ToDictionary(x => x.Id);
        _wards = LoadJson<WardLocation>(Path.Combine(locationsPath, "wards.json"))
            .ToDictionary(x => x.Id);
    }

    public Task<string?> GetProvinceNameAsync(int provinceId, CancellationToken ct = default)
    {
        return Task.FromResult(_provinces.TryGetValue(provinceId, out var province) ? province.Name : null);
    }

    public Task<string?> GetDistrictNameAsync(int provinceId, int districtId, CancellationToken ct = default)
    {
        if (!_districts.TryGetValue(districtId, out var district) || district.ProvinceId != provinceId)
            return Task.FromResult<string?>(null);

        return Task.FromResult<string?>(district.Name);
    }

    public Task<string?> GetWardNameAsync(int provinceId, int districtId, int wardId, CancellationToken ct = default)
    {
        if (!_wards.TryGetValue(wardId, out var ward) || ward.ProvinceId != provinceId || ward.DistrictId != districtId)
            return Task.FromResult<string?>(null);

        return Task.FromResult<string?>(ward.Name);
    }

    private static List<T> LoadJson<T>(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Location data file not found: {filePath}", filePath);

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
    }

    private sealed record ProvinceLocation(int Id, string Name);
    private sealed record DistrictLocation(int Id, string Name, int ProvinceId);
    private sealed record WardLocation(int Id, string Name, int DistrictId, int ProvinceId);
}