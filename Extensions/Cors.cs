using Microsoft.Extensions.DependencyInjection;

namespace EVerland.Extentions;

/// <summary>
/// CORS configuration extension for Requirement 8: JWT HttpOnly Cookie with CORS support.
/// Requirement 8.9: Configure CORS with AllowCredentials = true and AllowedOrigins loaded from appsettings.json.
/// </summary>
public static class CorsExtension
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection("Cors").Get<CorsOptions>()
            ?? new CorsOptions
            {
                AllowedOrigins = ["http://localhost:3000", "http://localhost:5173"],
                AllowedMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
                AllowedHeaders = ["Content-Type", "Authorization"]
            };

        services.AddCors(options =>
        {
            options.AddPolicy("AllowCredentials", builder =>
            {
                // Requirement 8.11: Allow credentials and do NOT use wildcard origins when credentials are enabled
                builder
                    .WithOrigins(corsOptions.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials() // Required for HttpOnly cookies
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
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedMethods { get; set; } = Array.Empty<string>();
    public string[] AllowedHeaders { get; set; } = Array.Empty<string>();
    public int MaxAge { get; set; } = 3600;
}
