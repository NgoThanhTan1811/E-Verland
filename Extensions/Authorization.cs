using Microsoft.Extensions.DependencyInjection;

namespace EVerland.Extentions;

public static class AuthorizationExtension
{
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
            options.AddPolicy("SellerOnly", policy => policy.RequireRole("Seller"));
        });

        return services;
    }
}