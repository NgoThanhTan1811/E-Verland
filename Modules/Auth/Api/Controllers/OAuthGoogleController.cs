using System.Security.Claims;
using System.Text;
using EVerland.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Modules.Auth.Application.DTOs.Response;
using Modules.Auth.Application.Services;

namespace Modules.Auth.Api.Controllers
{
    [ApiController]
    [Route("api/auth/google")]
    public class OAuthGoogleController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthGoogleController> _logger;

        public OAuthGoogleController(
            IAuthService authService,
            IConfiguration configuration,
            ILogger<OAuthGoogleController> logger)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("login")]
        public IActionResult LoginGoogle([FromQuery] string? returnUrl = null)
        {
            return Challenge(BuildGoogleAuthProperties(returnUrl), OAuthGmailExtensions.GoogleScheme);
        }

        [HttpGet("register")]
        public IActionResult RegisterGoogle([FromQuery] string? returnUrl = null)
        {
            return Challenge(BuildGoogleAuthProperties(returnUrl), OAuthGmailExtensions.GoogleScheme);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> CallbackGoogle([FromQuery] string? returnUrl = null)
        {
            AuthenticateResult authenticateResult;

            try
            {
                authenticateResult = await HttpContext.AuthenticateAsync(OAuthGmailExtensions.GoogleCookieScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google OAuth callback authentication failed");
                return HandleFailure(returnUrl, "Xác thực Google thất bại");
            }

            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                _logger.LogWarning("Google OAuth callback did not produce a valid principal");
                return HandleFailure(returnUrl, "Không lấy được thông tin tài khoản Google");
            }

            var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email)
                ?? authenticateResult.Principal.FindFirstValue("email");
            var displayName = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name)
                ?? authenticateResult.Principal.FindFirstValue("name");

            var loginResult = await _authService.LoginWithGoogleAsync(email ?? string.Empty, displayName);

            await HttpContext.SignOutAsync(OAuthGmailExtensions.GoogleCookieScheme);

            if (!loginResult.Success)
            {
                return HandleFailure(returnUrl, loginResult.Message);
            }

            var resolvedReturnUrl = ResolveReturnUrl(returnUrl, authenticateResult.Properties);
            if (!string.IsNullOrWhiteSpace(resolvedReturnUrl))
            {
                return Redirect(BuildSuccessRedirectUrl(resolvedReturnUrl, loginResult));
            }

            return Ok(loginResult);
        }

        private AuthenticationProperties BuildGoogleAuthProperties(string? returnUrl)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "https://e-verland-czf8bbhqfyd3ecfb.southeastasia-01.azurewebsites.net/api/auth/google/callback"
            };

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                properties.Items["returnUrl"] = returnUrl;
            }

            return properties;
        }

        private IActionResult HandleFailure(string? returnUrl, string errorMessage)
        {
            var resolvedReturnUrl = ResolveReturnUrl(returnUrl, null);
            if (!string.IsNullOrWhiteSpace(resolvedReturnUrl))
            {
                return Redirect(BuildErrorRedirectUrl(resolvedReturnUrl, errorMessage));
            }

            return BadRequest(new LoginResponseDto
            {
                Success = false,
                Message = errorMessage
            });
        }

        private string? ResolveReturnUrl(string? returnUrl, AuthenticationProperties? properties)
        {
            var configuredFrontend = _configuration["App:FrontendUrl"]
                ?? Environment.GetEnvironmentVariable("FRONTEND_URL");

            string? candidate = returnUrl;

            if (string.IsNullOrWhiteSpace(candidate)
                && properties?.Items != null
                && properties.Items.TryGetValue("returnUrl", out var propertyReturnUrl))
            {
                candidate = propertyReturnUrl;
            }

            if (string.IsNullOrWhiteSpace(candidate))
            {
                candidate = configuredFrontend;
            }

            if (string.IsNullOrWhiteSpace(candidate))
            {
                return null;
            }

            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var candidateUri))
            {
                if (string.IsNullOrWhiteSpace(configuredFrontend) || !Uri.TryCreate(configuredFrontend, UriKind.Absolute, out var frontendUri))
                {
                    return null;
                }

                var combinedUrl = $"{frontendUri.ToString().TrimEnd('/')}/{candidate.TrimStart('/')}";
                candidateUri = new Uri(combinedUrl, UriKind.Absolute);
            }

            if (string.IsNullOrWhiteSpace(configuredFrontend) || !Uri.TryCreate(configuredFrontend, UriKind.Absolute, out var configuredFrontendUri))
            {
                return null;
            }

            if (!Uri.Compare(candidateUri, configuredFrontendUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase).Equals(0))
            {
                _logger.LogWarning("Rejected Google OAuth returnUrl because it does not match configured frontend origin: {ReturnUrl}", candidateUri);
                return configuredFrontendUri.ToString();
            }

            return candidateUri.ToString();
        }

        private static string BuildSuccessRedirectUrl(string baseUrl, LoginResponseDto result)
        {
            var values = new Dictionary<string, string?>
            {
                ["success"] = "true",
                ["message"] = result.Message,
                ["token"] = result.Token,
                ["refreshToken"] = result.RefreshToken,
                ["accessTokenExpiresAt"] = result.AccessTokenExpiresAt?.ToString("O"),
                ["refreshTokenExpiresAt"] = result.RefreshTokenExpiresAt?.ToString("O"),
                ["userId"] = result.User?.Id.ToString(),
                ["email"] = result.User?.Email,
                ["username"] = result.User?.Username,
                ["role"] = result.User?.Role
            };

            return BuildRedirectUrl(baseUrl, values);
        }

        private static string BuildErrorRedirectUrl(string baseUrl, string errorMessage)
        {
            var values = new Dictionary<string, string?>
            {
                ["success"] = "false",
                ["error"] = errorMessage
            };

            return BuildRedirectUrl(baseUrl, values);
        }

        private static string BuildRedirectUrl(string baseUrl, IDictionary<string, string?> values)
        {
            var builder = new StringBuilder();
            var first = true;

            foreach (var entry in values)
            {
                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append('&');
                }

                builder
                    .Append(Uri.EscapeDataString(entry.Key))
                    .Append('=')
                    .Append(Uri.EscapeDataString(entry.Value));

                first = false;
            }

            return string.IsNullOrWhiteSpace(builder.ToString())
                ? baseUrl
                : $"{baseUrl}#{builder}";

        }
    }
}