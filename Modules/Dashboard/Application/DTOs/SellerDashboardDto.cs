namespace Modules.Dashboard.Application.DTOs;

public sealed record SellerDashboardDto
{
    // Shop Information
    public ShopStatsDto? MyShopStats { get; init; }

    // Sales Overview
    public int MyProductCount { get; init; }
    public decimal MyTotalRevenue { get; init; }
    public int MyTotalOrders { get; init; }

    // Order Status Breakdown
    public int PendingOrders { get; init; }
    public int CompletedOrders { get; init; }
    public int CanceledOrders { get; init; }
    public int ShippingOrders { get; init; }

    // Top Performing Products
    public List<TopSellingProductDto> TopSellingProducts { get; init; } = [];

    // Recent Orders
    public List<SellerRecentOrderDto> RecentOrders { get; init; } = [];

    // Sales Chart (Last 30 Days)
    public List<ChartDataPoint> SalesChart { get; init; } = [];
}

public sealed record ShopStatsDto
{
    public Guid ShopId { get; init; }
    public string ShopName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public string? LogoUrl { get; init; }
    public string? Description { get; init; }
}

public sealed record TopSellingProductDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public int SoldCount { get; init; }
    public decimal Revenue { get; init; }
    public int StockRemaining { get; init; }
    public string? ImageUrl { get; init; }
    public decimal? Rating { get; init; }
}

public sealed record SellerRecentOrderDto
{
    public Guid OrderId { get; init; }
    public string OrderCode { get; init; } = default!;
    public decimal GrandTotal { get; init; }
    public string Status { get; init; } = default!;
    public string PaymentStatus { get; init; } = default!;
    public DateTime OrderDate { get; init; }
    public string CustomerName { get; init; } = default!;
    public int ItemCount { get; init; }
}
