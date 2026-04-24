using MediatR;
using Modules.Dashboard.Application.Contracts;
using Modules.Dashboard.Application.DTOs;

namespace Modules.Dashboard.Application.Queries;

public sealed record GetAdminDashboardQuery : IRequest<AdminDashboardDto>;

public sealed class GetAdminDashboardHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IDashboardMetricsCache _metricsCache;

    public GetAdminDashboardHandler(IDashboardMetricsCache metricsCache)
    {
        _metricsCache = metricsCache;
    }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken ct)
    {
        return await _metricsCache.GetAdminSnapshotAsync(ct);
    }
}
