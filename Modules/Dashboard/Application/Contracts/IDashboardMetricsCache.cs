using Modules.Dashboard.Application.DTOs;

namespace Modules.Dashboard.Application.Contracts;

public interface IDashboardMetricsCache
{
    Task<AdminDashboardDto> GetAdminSnapshotAsync(CancellationToken ct = default);
    Task<SellerDashboardDto> GetSellerSnapshotAsync(Guid sellerId, CancellationToken ct = default);
    Task RefreshSnapshotsAsync(CancellationToken ct = default);
}
