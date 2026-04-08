using Modules.Order.Application;
using Modules.Order.Infrastructure.Repositories;
using Modules.Order.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Modules.Order.Infrastructure.Persistence;
using Modules.Order.Infrastructure.Services;

namespace Modules.Order;

public static class OrderModuleExtension
{
    public static IServiceCollection AddOrderModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var conn = configuration.GetConnectionString("OrderDb")
                ?? Environment.GetEnvironmentVariable("OrderDb")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:OrderDb");

        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(OrderDbContext).Assembly.GetName().Name);
            }));

        // Add Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Add Application Services
        services.AddScoped<IOrderDbContext>(provider => provider.GetRequiredService<OrderDbContext>());

        // Add Product Service for Order
        services.AddScoped<IProductService, ProductService>();
        // Add AutoMapper
        services.AddAutoMapper(typeof(OrderApplicationMarker).Assembly);

        return services;
    }
}
