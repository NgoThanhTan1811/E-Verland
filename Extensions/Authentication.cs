using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using EVerland.Extentions;

public static class AuthenticationExtension
{
    public static IServiceCollection AddCustomJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
                        ?? throw new InvalidOperationException("Missing Jwt:Key.");

        var jwtIssuer = configuration["Jwt:Issuer"]
                        ?? throw new InvalidOperationException("Missing Jwt:Issuer.");

        var jwtAudience = configuration["Jwt:Audience"]
                          ?? throw new InvalidOperationException("Missing Jwt:Audience.");

        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            throw new InvalidOperationException("JWT Key must be at least 32 characters.");

        var key = Encoding.UTF8.GetBytes(jwtKey);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // dev only

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ClockSkew = TimeSpan.Zero
                };

                // Allow JWT in cookies for WebSocket connections
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Items.TryGetValue(AutoRefreshTokenMiddleware.RefreshedAccessTokenItemKey, out var refreshedToken)
                            && refreshedToken is string refreshedAccessToken
                            && !string.IsNullOrWhiteSpace(refreshedAccessToken))
                        {
                            context.Token = refreshedAccessToken;
                            return Task.CompletedTask;
                        }

                        var authorizationHeader = context.Request.Headers.Authorization.ToString();
                        if (!string.IsNullOrWhiteSpace(authorizationHeader)
                            && authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = authorizationHeader[7..].Trim();
                            return Task.CompletedTask;
                        }

                        var hasAccessCookie = context.Request.Cookies.TryGetValue("access_token", out var token);

                        if (hasAccessCookie)
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");

                        var subject = context.Principal?.FindFirst("sub")?.Value
                            ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? "unknown";
                        var role = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                            ?? context.Principal?.FindFirst("role")?.Value
                            ?? "unknown";


                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");

                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}