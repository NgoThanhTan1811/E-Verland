namespace Modules.Dashboard.Application.DTOs;

public sealed record AdminDashboardDto
{
    // System Overview
    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalProducts { get; init; }
    public int TotalUsers { get; init; }
    public int TotalShops { get; init; }

    // Shop Management
    public int PendingShopsCount { get; init; }
    public int ActiveSellersCount { get; init; }

    // Recent Activity
    public List<RecentOrderDto> RecentOrders { get; init; } = [];

    // Top Products
    public List<TopProductDto> TopProducts { get; init; } = [];

    // Charts Data
    public List<ChartDataPoint> SalesChart { get; init; } = [];
    public List<ChartDataPoint> OrdersChart { get; init; } = [];
}

public sealed record RecentOrderDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public decimal GrandTotal { get; init; }
    public string Status { get; init; } = default!;
    public string PaymentStatus { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public string CustomerName { get; init; } = default!;
}

public sealed record TopProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public int SoldCount { get; init; }
    public decimal Revenue { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed record ChartDataPoint
{
    public string Label { get; init; } = default!;
    public decimal Value { get; init; }
    public DateTime Date { get; init; }
}
