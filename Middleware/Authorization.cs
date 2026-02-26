using Microsoft.Extensions.DependencyInjection;

namespace EVerland.Middleware;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
        });

        return services;
    }
}