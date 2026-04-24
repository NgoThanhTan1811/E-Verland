namespace Modules.Dashboard.Application.DTOs;

public sealed record SellerDashboardDto(
    Guid SellerId,
    IReadOnlyDictionary<string, int> TotalOrdersByStatus,
    decimal TotalRevenue,
    IReadOnlyList<TopProductMetricDto> TopProducts,
    DateTime GeneratedAtUtc
);
