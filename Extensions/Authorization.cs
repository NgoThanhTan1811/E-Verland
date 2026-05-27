using Microsoft.Extensions.DependencyInjection;

namespace EVerland.Extentions;

public static class AuthorizationExtension
{
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CustomerPolicy", policy =>
                policy.RequireAssertion(context =>
                {
                    if (!context.User.Identity?.IsAuthenticated ?? false)
                        return false;

                    var roleClaim = context.User.FindFirst("role")?.Value;
                    return roleClaim == "Customer";
                }));

            options.AddPolicy("SellerPolicy", policy =>
                policy.RequireAssertion(context =>
                {
                    if (!context.User.Identity?.IsAuthenticated ?? false)
                        return false;

                    var roleClaim = context.User.FindFirst("role")?.Value;
                    return roleClaim == "Seller";
                }));

            options.AddPolicy("AdminPolicy", policy =>
                policy.RequireAssertion(context =>
                {
                    if (!context.User.Identity?.IsAuthenticated ?? false)
                        return false;

                    var roleClaim = context.User.FindFirst("role")?.Value;
                    return roleClaim == "Admin";
                }));
        });

        return services;
    }
}