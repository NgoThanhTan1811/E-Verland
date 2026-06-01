using Microsoft.Extensions.DependencyInjection;

namespace EVerland.Extentions;

public static class AuthorizationExtension
{
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("User"));
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
            options.AddPolicy("SellerPolicy", policy => policy.RequireRole("Seller"));
            options.AddPolicy("AdminOrSeller", policy => policy.RequireRole("Admin", "Seller"));
            options.AddPolicy("SellerOrCustomer", policy => policy.RequireRole("User", "Seller"));
        });

        return services;
    }
}