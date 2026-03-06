using Modules.Cart.Infrastructure.Repositorise;
using Modules.Cart.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Modules.Cart.Infrastructure.Persistence;
using Modules.Product.Application.Services;
using Modules.Cart.Application;

namespace Modules.Cart;

public static class CartModuleExtensions
{
    public static IServiceCollection AddCartModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var conn = configuration.GetConnectionString("CartDb")
                ?? Environment.GetEnvironmentVariable("CartDb")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:CartDb");

        services.AddDbContext<CartDbContext>(options =>
            options.UseNpgsql(conn));
        // Add Repositories
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartItemRepository, CartItemRepository>();

        // Add Application Services
        services.AddScoped<ICartDbContext>(provider => provider.GetRequiredService<CartDbContext>());

        // Add MediatR
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(CartApplicationMarker).Assembly));

        // Add AutoMapper
        services.AddAutoMapper(typeof(CartApplicationMarker).Assembly);

        return services;
    }
}
