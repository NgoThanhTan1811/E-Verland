using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Modules.Dashboard.Application.Contracts;
using Modules.Dashboard.Application.DTOs;
using Modules.Dashboard.Infrastructure.Options;
using Modules.Order.Infrastructure.Persistence;
using Modules.Payment.Domain;
using Modules.Payment.Infrastructure.Persistence;
using Modules.Product.Infrastructure.Persistence;
using Modules.Redis.Infrastructure;

namespace Modules.Dashboard.Infrastructure.Services;

public sealed class DashboardMetricsCacheService : IDashboardMetricsCache
{
    private const string AdminSnapshotKey = "dashboard:admin:snapshot:v2";
    private const string SellerSnapshotKeyPrefix = "dashboard:seller:snapshot:v2:";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICacheService _cacheService;
    private readonly DashboardOptions _options;
    private readonly ILogger<DashboardMetricsCacheService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public DashboardMetricsCacheService(
        IServiceScopeFactory scopeFactory,
        ICacheService cacheService,
        IOptions<DashboardOptions> options,
        ILogger<DashboardMetricsCacheService> logger)
    {
        _scopeFactory = scopeFactory;
        _cacheService = cacheService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AdminDashboardDto> GetAdminSnapshotAsync(CancellationToken ct = default)
    {
        var cached = await _cacheService.GetAsync<CachedSnapshot<AdminDashboardDto>>(AdminSnapshotKey);
        if (cached is not null && !IsStale(cached.GeneratedAtUtc))
        {
            return cached.Payload;
        }

        await RefreshSnapshotsAsync(ct);
        cached = await _cacheService.GetAsync<CachedSnapshot<AdminDashboardDto>>(AdminSnapshotKey);

        return cached?.Payload ?? new AdminDashboardDto(
            TotalProducts: 0,
            TotalOrdersByStatus: new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            TotalRevenue: 0,
            TopProducts: [],
            TopSellers: [],
            PlatformCash: 0,
            CustomerLiability: 0,
            SellerPending: 0,
            SellerAvailable: 0,
            GeneratedAtUtc: DateTime.UtcNow);
    }

    public async Task<SellerDashboardDto> GetSellerSnapshotAsync(Guid sellerId, CancellationToken ct = default)
    {
        var key = BuildSellerSnapshotKey(sellerId);
        var cached = await _cacheService.GetAsync<CachedSnapshot<SellerDashboardDto>>(key);
        if (cached is not null && !IsStale(cached.GeneratedAtUtc))
        {
            return cached.Payload;
        }

        await RefreshSnapshotsAsync(ct);
        cached = await _cacheService.GetAsync<CachedSnapshot<SellerDashboardDto>>(key);

        return cached?.Payload ?? new SellerDashboardDto(
            SellerId: sellerId,
            TotalProducts: 0,
            TotalOrdersByStatus: new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            TotalRevenue: 0,
            TopProducts: [],
            PendingBalance: 0,
            AvailableBalance: 0,
            GeneratedAtUtc: DateTime.UtcNow);
    }

    public async Task RefreshSnapshotsAsync(CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var orderDb = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            var productDb = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

            var generatedAtUtc = DateTime.UtcNow;
            var cacheTtl = TimeSpan.FromMinutes(Math.Max(1, _options.SnapshotTtlMinutes));

            var orderStatusRows = await orderDb.Orders
                .AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(ct);

            var totalRevenue = await paymentDb.Payments
                .AsNoTracking()
                .Where(x => x.Status == Modules.Payment.Domain.PaymentStatus.Success)
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

            var orderItemFacts = await (
                from item in orderDb.OrderItems.AsNoTracking()
                join order in orderDb.Orders.AsNoTracking() on item.OrderId equals order.Id
                select new OrderItemFact(
                    order.Id,
                    item.ProductId,
                    item.Quantity,
                    (decimal)item.UnitPrice * item.Quantity,
                    order.Status.ToString(),
                    order.PaymentStatus == Modules.Order.Domain.PaymentStatus.Success))
                .ToListAsync(ct);

            var productIds = orderItemFacts
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var productLookups = await productDb.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .Select(x => new ProductLookup(x.Id, x.Name, x.ShopId))
                .ToListAsync(ct);

            var productLookupById = productLookups
                .GroupBy(x => x.ProductId)
                .ToDictionary(x => x.Key, x => x.First());

            var paidFacts = orderItemFacts.Where(x => x.IsPaid).ToList();

            var topProducts = paidFacts
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SoldCount = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Revenue)
                })
                .OrderByDescending(x => x.Revenue)
                .ThenByDescending(x => x.SoldCount)
                .Take(10)
                .Select(x => new TopProductMetricDto(
                    x.ProductId,
                    productLookupById.TryGetValue(x.ProductId, out var product) ? product.ProductName : "Unknown product",
                    x.SoldCount,
                    x.Revenue))
                .ToList();

            var topSellers = paidFacts
                .Where(x => productLookupById.TryGetValue(x.ProductId, out var product) && product.SellerId.HasValue)
                .Select(x =>
                {
                    var sellerId = productLookupById[x.ProductId].SellerId!.Value;
                    return new { sellerId, x.OrderId, x.Revenue };
                })
                .GroupBy(x => x.sellerId)
                .Select(g => new TopSellerMetricDto(
                    g.Key,
                    g.Sum(x => x.Revenue),
                    g.Select(x => x.OrderId).Distinct().Count()))
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            var ordersByStatus = orderStatusRows.ToDictionary(x => x.Status, x => x.Count, StringComparer.OrdinalIgnoreCase);

            var latestSnapshots = await paymentDb.BalanceSnapshots
                .AsNoTracking()
                .GroupBy(x => x.AccountType)
                .Select(g => g.OrderByDescending(x => x.SnapshotAtUtc).FirstOrDefault())
                .ToListAsync(ct);

            var platformCash = latestSnapshots.FirstOrDefault(x => x?.AccountType == LedgerAccountType.PlatformCash)?.Balance ?? 0m;
            var customerLiability = latestSnapshots.FirstOrDefault(x => x?.AccountType == LedgerAccountType.CustomerLiability)?.Balance ?? 0m;
            var sellerPending = latestSnapshots.FirstOrDefault(x => x?.AccountType == LedgerAccountType.SellerPending)?.Balance ?? 0m;
            var sellerAvailable = latestSnapshots.FirstOrDefault(x => x?.AccountType == LedgerAccountType.SellerAvailable)?.Balance ?? 0m;

            var totalProductsAdmin = await productDb.Products.CountAsync(ct);

            var adminSnapshot = new AdminDashboardDto(totalProductsAdmin, ordersByStatus, totalRevenue, topProducts, topSellers, platformCash, customerLiability, sellerPending, sellerAvailable, generatedAtUtc);

            await _cacheService.SetAsync(
                AdminSnapshotKey,
                new CachedSnapshot<AdminDashboardDto>(adminSnapshot, generatedAtUtc),
                cacheTtl);

            var sellerBalancesMap = await paymentDb.SellerBalances
                .AsNoTracking()
                .GroupBy(x => x.SellerId)
                .Select(g => new { SellerId = g.Key, Pending = g.Sum(x => x.PendingAmount), Available = g.Sum(x => x.AvailableAmount) })
                .ToDictionaryAsync(x => x.SellerId, x => (x.Pending, x.Available), ct);

            var totalProductsBySeller = await productDb.Products
                .AsNoTracking()
                .Where(p => p.ShopId != null)
                .GroupBy(p => p.ShopId)
                .Select(g => new { SellerId = g.Key!.Value, Count = g.Count() })
                .ToDictionaryAsync(x => x.SellerId, x => x.Count, ct);

            var sellerSnapshots = BuildSellerSnapshots(orderItemFacts, productLookupById, sellerBalancesMap, totalProductsBySeller, generatedAtUtc);
            foreach (var sellerSnapshot in sellerSnapshots)
            {
                await _cacheService.SetAsync(
                    BuildSellerSnapshotKey(sellerSnapshot.SellerId),
                    new CachedSnapshot<SellerDashboardDto>(sellerSnapshot, generatedAtUtc),
                    cacheTtl);
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private List<SellerDashboardDto> BuildSellerSnapshots(
        IReadOnlyList<OrderItemFact> facts,
        IReadOnlyDictionary<Guid, ProductLookup> productLookupById,
        IReadOnlyDictionary<Guid, (decimal Pending, decimal Available)> sellerBalancesMap,
        IReadOnlyDictionary<Guid, int> totalProductsBySeller,
        DateTime generatedAtUtc)
    {
        var sellerFacts = facts
            .Where(x => productLookupById.TryGetValue(x.ProductId, out var product) && product.SellerId.HasValue)
            .Select(x => new SellerOrderFact(
                productLookupById[x.ProductId].SellerId!.Value,
                x.OrderId,
                x.ProductId,
                x.Quantity,
                x.Revenue,
                x.OrderStatus,
                x.IsPaid))
            .ToList();

        var snapshots = new List<SellerDashboardDto>();
        foreach (var sellerGroup in sellerFacts.GroupBy(x => x.SellerId))
        {
            var uniqueOrders = sellerGroup
                .GroupBy(x => x.OrderId)
                .Select(g => g.First())
                .ToList();

            var orderStatusCounts = uniqueOrders
                .GroupBy(x => x.OrderStatus)
                .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

            var paidSellerFacts = sellerGroup.Where(x => x.IsPaid).ToList();
            var totalRevenue = paidSellerFacts.Sum(x => x.Revenue);

            var topProducts = paidSellerFacts
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SoldCount = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Revenue)
                })
                .OrderByDescending(x => x.Revenue)
                .ThenByDescending(x => x.SoldCount)
                .Take(10)
                .Select(x => new TopProductMetricDto(
                    x.ProductId,
                    productLookupById.TryGetValue(x.ProductId, out var product) ? product.ProductName : "Unknown product",
                    x.SoldCount,
                    x.Revenue))
                .ToList();

            snapshots.Add(new SellerDashboardDto(
                SellerId: sellerGroup.Key,
                TotalProducts: totalProductsBySeller.TryGetValue(sellerGroup.Key, out var count) ? count : 0,
                TotalOrdersByStatus: orderStatusCounts,
                TotalRevenue: totalRevenue,
                TopProducts: topProducts,
                PendingBalance: sellerBalancesMap.TryGetValue(sellerGroup.Key, out var b) ? b.Pending : 0m,
                AvailableBalance: sellerBalancesMap.TryGetValue(sellerGroup.Key, out var b2) ? b2.Available : 0m,
                GeneratedAtUtc: generatedAtUtc));
        }

        return snapshots;
    }

    private bool IsStale(DateTime generatedAtUtc)
    {
        var staleAfter = TimeSpan.FromMinutes(Math.Max(1, _options.StaleAfterMinutes));
        return DateTime.UtcNow - generatedAtUtc > staleAfter;
    }

    private static string BuildSellerSnapshotKey(Guid sellerId)
    {
        return $"{SellerSnapshotKeyPrefix}{sellerId:N}";
    }

    private sealed record CachedSnapshot<T>(T Payload, DateTime GeneratedAtUtc) where T : class;
    private sealed record OrderItemFact(
        Guid OrderId,
        Guid ProductId,
        int Quantity,
        decimal Revenue,
        string OrderStatus,
        bool IsPaid);
    private sealed record ProductLookup(Guid ProductId, string ProductName, Guid? SellerId);
    private sealed record SellerOrderFact(
        Guid SellerId,
        Guid OrderId,
        Guid ProductId,
        int Quantity,
        decimal Revenue,
        string OrderStatus,
        bool IsPaid);
}
