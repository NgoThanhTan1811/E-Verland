namespace Modules.Dashboard.Application.DTOs;

public sealed record SellerDashboardDto(
    Guid SellerId,
    int TotalProducts,
    IReadOnlyDictionary<string, int> TotalOrdersByStatus,
    decimal TotalRevenue,
    IReadOnlyList<TopProductMetricDto> TopProducts,
    decimal PendingBalance,
    decimal AvailableBalance,
    DateTime GeneratedAtUtc
);
