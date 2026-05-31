using Microsoft.Extensions.DependencyInjection;

namespace EVerland.Extentions;

public static class CorsExtension
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection("Cors").Get<CorsOptions>()
            ?? new CorsOptions
            {
                AllowedOrigins = []
            };

        if (corsOptions.AllowedOrigins is null || corsOptions.AllowedOrigins.Length == 0)
        {
            corsOptions.AllowedOrigins =
            [
                "http://localhost:5173",
                "http://localhost:8080",
                "https://e-verland.site",
                "https://seller.e-verland.site",
                "https://admin.e-verland.site",
                "http://localhost:7700",
            ];
        }

        services.AddCors(options =>
        {
            options.AddPolicy("AllowCredentials", builder =>
            {
                builder
                    .WithOrigins(corsOptions.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("Content-Disposition");
            });
        });

        return services;
    }
}

/// <summary>
/// Configuration options for CORS (loaded from appsettings.json).
/// </summary>
public class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = [];
    public string[] AllowedHeaders { get; set; } = [];
    public int MaxAge { get; set; } = 3600;
}
