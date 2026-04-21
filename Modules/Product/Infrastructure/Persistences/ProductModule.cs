using Modules.Product.Application.Services;
using Modules.Product.Application;
using Modules.Product.Infrastructure.Repositories;
using Modules.Product.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Modules.Product.Infrastructure.Persistence;
using Modules.Product.Infrastructure.Services;
using SharedKernel.Persistence;

namespace Modules.Product;

public static class ProductModuleExtension
{
    public static IServiceCollection AddProductModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var conn = configuration.GetConnectionString("ProductDb")
                ?? Environment.GetEnvironmentVariable("ProductDb")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:ProductDb");

        services.AddDbContext<ProductDbContext>(options =>
            options.ConfigureNpgsql(conn, typeof(ProductDbContext).Assembly.GetName().Name!, readHeavy: true));
        // Add Repositories
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISkuRepository, SkuRepository>();

        // Add Application Services
        services.AddScoped<IProductDbContext>(provider => provider.GetRequiredService<ProductDbContext>());
        services.AddScoped<SKUGeneratorService>();
        services.AddScoped<IProductReservationService, ProductReservationService>();
        services.AddHostedService<StockReservationExpiryService>();

        // Add Infrastructure Services
        services.AddScoped<IProductSyncPublisher, ProductSyncPublisher>();
        services.AddScoped<IProductModerationAuditLog, ProductModerationAuditLog>();
        services.AddHostedService<OpenSearchConsumer>();

        // Add MediatR
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(ProductApplicationMarker).Assembly));

        return services;
    }
}
