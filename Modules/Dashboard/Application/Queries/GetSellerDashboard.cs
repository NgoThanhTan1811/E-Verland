using MediatR;
using Modules.Dashboard.Application.Contracts;
using Modules.Dashboard.Application.DTOs;

namespace Modules.Dashboard.Application.Queries;

public sealed record GetSellerDashboardQuery(Guid SellerId) : IRequest<SellerDashboardDto>;

public sealed class GetSellerDashboardHandler : IRequestHandler<GetSellerDashboardQuery, SellerDashboardDto>
{
    private readonly IDashboardMetricsCache _metricsCache;

    public GetSellerDashboardHandler(IDashboardMetricsCache metricsCache)
    {
        _metricsCache = metricsCache;
    }

    public async Task<SellerDashboardDto> Handle(GetSellerDashboardQuery request, CancellationToken ct)
    {
        return await _metricsCache.GetSellerSnapshotAsync(request.SellerId, ct);
    }
}
