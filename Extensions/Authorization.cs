using Microsoft.Extensions.DependencyInjection;

namespace EVerland.Extentions;

public static class AuthorizationExtension
{
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Requirement 6: Role-based authorization policies
            // CustomerPolicy: requires JWT role claim == "Customer"
            options.AddPolicy("CustomerPolicy", policy =>
                policy.RequireAssertion(context =>
                {
                    if (!context.User.Identity?.IsAuthenticated ?? false)
                        return false;

                    var roleClaim = context.User.FindFirst("role")?.Value;
                    return roleClaim == "Customer";
                }));

            // SellerPolicy: requires JWT role claim == "Seller"
            options.AddPolicy("SellerPolicy", policy =>
                policy.RequireAssertion(context =>
                {
                    if (!context.User.Identity?.IsAuthenticated ?? false)
                        return false;

                    var roleClaim = context.User.FindFirst("role")?.Value;
                    return roleClaim == "Seller";
                }));

            // AdminPolicy: requires JWT role claim == "Admin"
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