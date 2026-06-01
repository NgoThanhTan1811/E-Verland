using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Modules.Auth.Application.Services;
using Modules.Redis.Services;
using Modules.User.Domain.Enums;

namespace EVerland.Extentions;

public sealed class AutoRefreshTokenMiddleware
{
    public const string RefreshedAccessTokenItemKey = "RefreshedAccessToken";

    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutoRefreshTokenMiddleware> _logger;

    public AutoRefreshTokenMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AutoRefreshTokenMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var accessToken = context.Request.Cookies["access_token"];
        var refreshToken = context.Request.Cookies["refresh_token"];

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            await _next(context);
            return;
        }

        var principal = TryValidateExpiredAccessToken(accessToken);
        if (principal == null)
        {
            await _next(context);
            return;
        }

        if (TryGetExpiry(principal, out var expiresAtUtc) && expiresAtUtc > DateTime.UtcNow)
        {
            await _next(context);
            return;
        }

        var userId = TryGetUserId(principal);
        if (userId == null)
        {
            await _next(context);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountRepository = scope.ServiceProvider.GetRequiredService<Modules.User.Application.Interfaces.Repositories.IAccountRepository>();
            var jwtCacheService = scope.ServiceProvider.GetRequiredService<IJwtCacheService>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

            var account = await accountRepository.GetByIdAsync(userId.Value, context.RequestAborted);
            if (account == null || account.Status != StatusUser.Active)
            {
                ClearTokenCookies(context);
                await _next(context);
                return;
            }

            var cachedRefreshToken = await jwtCacheService.GetTokenAsync(userId.Value.ToString());
            if (!string.Equals(cachedRefreshToken, refreshToken, StringComparison.Ordinal))
            {
                ClearTokenCookies(context);
                await _next(context);
                return;
            }

            var newAccessToken = tokenService.GenerateAccessToken(account);
            context.Items[RefreshedAccessTokenItemKey] = newAccessToken;
            SetAccessTokenCookie(context, newAccessToken);

            _logger.LogInformation("Auto-refreshed access token for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto refresh token failed");
        }

        await _next(context);
    }

    private bool ShouldSkip(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/metrics", StringComparison.OrdinalIgnoreCase);
    }

    private ClaimsPrincipal? TryValidateExpiredAccessToken(string token)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey) || string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return null;
            }

            if (!string.Equals(jwtToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static Guid? TryGetUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static bool TryGetExpiry(ClaimsPrincipal principal, out DateTime expiresAtUtc)
    {
        expiresAtUtc = DateTime.MinValue;

        var expValue = principal.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (string.IsNullOrWhiteSpace(expValue) || !long.TryParse(expValue, out var seconds))
        {
            return false;
        }

        expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        return true;
    }

    private void SetAccessTokenCookie(HttpContext context, string accessToken)
    {
        var options = CreateAccessTokenCookieOptions(context);
        context.Response.Cookies.Append("access_token", accessToken, options);
    }

    private void ClearTokenCookies(HttpContext context)
    {
        var expiredOptions = CreateAccessTokenCookieOptions(context);
        expiredOptions.Expires = DateTime.UtcNow.AddDays(-1);

        context.Response.Cookies.Append("access_token", string.Empty, expiredOptions);
        context.Response.Cookies.Append("refresh_token", string.Empty, expiredOptions);
    }

    private CookieOptions CreateAccessTokenCookieOptions(HttpContext context)
    {
        var isHttpsRequest = context.Request.IsHttps;
        var cookieSecure = GetCookieSecure() && isHttpsRequest;
        var accessTokenMinutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var minutes) ? minutes : 30;

        return new CookieOptions
        {
            Expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            HttpOnly = true,
            Secure = cookieSecure,
            SameSite = SameSiteMode.Lax,
            Domain = ShouldSetCookieDomain(context) ? GetCookieDomain() : null,
            Path = "/"
        };
    }

    private bool ShouldSetCookieDomain(HttpContext context)
    {
        var cookieDomain = GetCookieDomain();
        if (string.IsNullOrWhiteSpace(cookieDomain))
        {
            return false;
        }

        var requestHost = context.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(requestHost))
        {
            return false;
        }

        return !requestHost.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && !requestHost.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            && !requestHost.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    private bool GetCookieSecure()
    {
        var secureStr = _configuration["Cookie:Secure"];
        return bool.TryParse(secureStr, out var secure) ? secure : true;
    }

    private string GetCookieDomain()
    {
        return _configuration["Cookie:Domain"] ?? "e-verland.site";
    }
}