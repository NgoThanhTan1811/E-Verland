using Microsoft.AspNetCore.Authentication.Google;

namespace EVerland.Extentions;

public static class OAuthGmailExtension
{
    public const string GoogleScheme = "Google";
    public const string GoogleCookieScheme = "GoogleCookies";

    public static void AddGoogleOAuth(this WebApplicationBuilder builder)
    {
        var clientId = Environment.GetEnvironmentVariable("Client_Id")
            ?? builder.Configuration["Authentication:Google:ClientId"];

        var clientSecret = Environment.GetEnvironmentVariable("Client_Secret")
            ?? builder.Configuration["Authentication:Google:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("Google OAuth credentials are not configured.");
        }

        builder.Services
        .AddAuthentication()
        .AddCookie(GoogleCookieScheme, options =>
        {
            options.Cookie.Name = "everland.google.auth";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            options.SlidingExpiration = false;
        })
        .AddGoogle(GoogleScheme, options =>
        {
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.SignInScheme = GoogleCookieScheme;
            options.CallbackPath = "/api/auth/google/callback";
            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.SaveTokens = false;
        });
    }
}