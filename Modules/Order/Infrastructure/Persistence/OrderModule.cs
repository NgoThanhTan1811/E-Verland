using Modules.Order.Application;
using Modules.Order.Infrastructure.Repositories;
using Modules.Order.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Modules.Order.Infrastructure.Persistence;
using SharedKernel.Persistence;
using Modules.Order.Infrastructure.Services;

namespace Modules.Order;

public static class OrderModuleExtension
{
    public static IServiceCollection AddOrderModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var conn = configuration["ConnectionStrings:OrderDb"]
                ?? throw new InvalidOperationException("Missing ConnectionStrings:OrderDb");

        services.AddDbContext<OrderDbContext>(options =>
            options.ConfigureNpgsql(conn, typeof(OrderDbContext).Assembly.GetName().Name!, readHeavy: true));

        // Add Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Add Application Services
        services.AddScoped<IOrderDbContext>(provider => provider.GetRequiredService<OrderDbContext>());

        // Add Product Service for Order
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderPaymentSyncService, OrderPaymentSyncService>();

        services.AddHostedService<Modules.Order.Infrastructure.Consumers.PaymentStatusConsumer>();
        services.AddHostedService<Modules.Order.Infrastructure.Consumers.ShippingStatusConsumer>();

        // Add MediatR
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(OrderApplicationMarker).Assembly));

        return services;
    }
}
