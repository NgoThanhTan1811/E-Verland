using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

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
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");

                        var hasAuthHeader = context.Request.Headers.ContainsKey("Authorization");
                        var hasAccessCookie = context.Request.Cookies.TryGetValue("access_token", out var token);

                        logger.LogDebug(
                            "JWT OnMessageReceived path={Path} method={Method} origin={Origin} authHeader={HasAuthHeader} accessCookie={HasAccessCookie} cookieLength={CookieLength}",
                            context.Request.Path.Value,
                            context.Request.Method,
                            context.Request.Headers.Origin.ToString(),
                            hasAuthHeader,
                            hasAccessCookie,
                            hasAccessCookie ? token?.Length ?? 0 : 0);

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

                        logger.LogDebug(
                            "JWT validated path={Path} subject={Subject} role={Role}",
                            context.Request.Path.Value,
                            subject,
                            role);

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");

                        logger.LogWarning(context.Exception, "JWT authentication failed path={Path}", context.Request.Path.Value);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");

                        logger.LogWarning(
                            "JWT challenge path={Path} error={Error} description={Description} authHeader={HasAuthHeader} accessCookie={HasAccessCookie}",
                            context.Request.Path.Value,
                            context.Error,
                            context.ErrorDescription,
                            context.Request.Headers.ContainsKey("Authorization"),
                            context.Request.Cookies.ContainsKey("access_token"));

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}