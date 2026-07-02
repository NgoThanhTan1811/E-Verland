namespace Modules.Dashboard.Application.DTOs;

public sealed record AdminDashboardDto(
    int TotalProducts,
    IReadOnlyDictionary<string, int> TotalOrdersByStatus,
    decimal TotalRevenue,
    IReadOnlyList<TopProductMetricDto> TopProducts,
    IReadOnlyList<TopSellerMetricDto> TopSellers,
    decimal PlatformCash,
    decimal CustomerLiability,
    decimal SellerPending,
    decimal SellerAvailable,
    DateTime GeneratedAtUtc
);

public sealed record TopProductMetricDto(
    Guid ProductId,
    string ProductName,
    int SoldCount,
    decimal Revenue
);

public sealed record TopSellerMetricDto(
    Guid SellerId,
    decimal Revenue,
    int OrdersCount
);
