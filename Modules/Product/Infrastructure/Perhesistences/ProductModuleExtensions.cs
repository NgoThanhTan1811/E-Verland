using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Modules.Product.Application.Services;
using Modules.Product.Application;
using Modules.Product.Infrastructure.Repositories;
using Modules.Product.Application.Abtracsts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Modules.Product.Infrastructure.Perhesistences;

public static class ProductModuleExtensions
{
    public static IServiceCollection AddProductModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ProductDbContext>(options =>
            options.UseNpgsql(connectionString ?? throw new InvalidOperationException("ConnectionString not found")));

        // Add Repositories
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISkuRepository, SkuRepository>();

        // Add Application Services
        services.AddScoped<IProductDbContext>(provider => provider.GetRequiredService<ProductDbContext>());
        services.AddScoped<SKUGeneratorService>();

        // Add MediatR
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(ProductApplicationMarker).Assembly));

        return services;
    }
}
