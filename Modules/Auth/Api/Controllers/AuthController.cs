using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.Auth.Application.DTOs.Request;
using Modules.Auth.Application.DTOs.Response;
using Modules.Auth.Application.Services;

namespace Modules.Auth.Api.Controllers
{
    [ApiController]
    [EnableRateLimiting("auth")]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IConfiguration configuration)
        {
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                if (!result.Success)
                    return BadRequest(result);

                // Requirement 8: Set JWT tokens in HttpOnly cookies instead of returning in body
                SetTokenCookies(result.Token!, result.RefreshToken!);

                // Requirement 8.3: Do NOT return tokens in JSON response body after re-implementation
                var response = new LoginResponseDto
                {
                    Success = true,
                    Message = result.Message,
                    Token = null,  // Explicitly null - tokens are in cookies
                    RefreshToken = null,  // Explicitly null
                    AccessTokenExpiresAt = result.AccessTokenExpiresAt,
                    RefreshTokenExpiresAt = result.RefreshTokenExpiresAt,
                    User = result.User
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new LoginResponseDto
                {
                    Success = false,
                    Message = "Loi khi dang nhap"
                });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<RefreshTokenResponseDto>> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            try
            {
                // Requirement 8.5: Use refresh_token cookie to issue new access token
                var refreshTokenFromCookie = Request.Cookies["refresh_token"];
                if (string.IsNullOrEmpty(refreshTokenFromCookie))
                {
                    // Requirement 8.8: Return 401 if refresh_token is missing, clear both cookies
                    ClearTokenCookies();
                    return Unauthorized(new RefreshTokenResponseDto
                    {
                        Success = false,
                        Message = "Refresh token không hợp lệ hoặc đã hết hạn"
                    });
                }

                // Create a modified request with the cookie token
                var modifiedDto = new RefreshTokenRequestDto
                {
                    Email = dto.Email,
                    RefreshToken = refreshTokenFromCookie
                };

                var result = await _authService.RefreshAsync(modifiedDto);
                if (!result.Success)
                {
                    // Clear cookies on failed refresh
                    ClearTokenCookies();
                    return Unauthorized(result);
                }

                // Set new token in cookies
                SetTokenCookies(result.Token!, result.RefreshToken!);

                // Don't return tokens in body
                var response = new RefreshTokenResponseDto
                {
                    Success = true,
                    Message = result.Message,
                    Token = null,  // Explicitly null
                    RefreshToken = null,  // Explicitly null
                    AccessTokenExpiresAt = result.AccessTokenExpiresAt,
                    RefreshTokenExpiresAt = result.RefreshTokenExpiresAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refresh token");
                return StatusCode(500, new RefreshTokenResponseDto
                {
                    Success = false,
                    Message = "Loi khi refresh token"
                });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public ActionResult Logout()
        {
            try
            {
                // Requirement 8.6: Clear both access_token and refresh_token cookies by setting them with expired date
                ClearTokenCookies();

                return Ok(new { Success = true, Message = "Logout thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { Success = false, Message = "Loi khi dang xuat" });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult<ChangePasswordResponseDto>> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new ChangePasswordResponseDto
                    {
                        Success = false,
                        Message = "Chua dang nhap"
                    });
                }

                var result = await _authService.ChangePasswordAsync(userId.Value, dto);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during change password");
                return StatusCode(500, new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Loi khi doi mat khau"
                });
            }
        }

        [HttpPost("forgot-password/request-otp")]
        public async Task<ActionResult<PasswordResetResponseDto>> RequestForgotPasswordOtp([FromBody] RequestPasswordResetOtpDto dto)
        {
            try
            {
                var result = await _authService.RequestPasswordResetOtpAsync(dto);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting reset OTP");
                return StatusCode(500, new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Loi khi gui OTP"
                });
            }
        }

        [HttpPost("forgot-password/verify-otp")]
        public async Task<ActionResult<PasswordResetResponseDto>> VerifyForgotPasswordOtp([FromBody] VerifyPasswordResetOtpDto dto)
        {
            try
            {
                var result = await _authService.VerifyPasswordResetOtpAsync(dto);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying reset OTP");
                return StatusCode(500, new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Loi khi xac minh OTP"
                });
            }
        }

        [HttpPost("forgot-password/reset")]
        public async Task<ActionResult<PasswordResetResponseDto>> ResetForgotPassword([FromBody] ResetPasswordRequestDto dto)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(dto);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Loi khi dat lai mat khau"
                });
            }
        }

        /// <summary>
        /// Requirement 8: Set JWT tokens in HttpOnly cookies with configured options
        /// </summary>
        private void SetTokenCookies(string accessToken, string refreshToken)
        {
            var cookieOptions = GetCookieOptions();

            // Requirement 8.1: Set access token in HttpOnly, Secure, SameSite=Lax cookie
            Response.Cookies.Append("access_token", accessToken, cookieOptions);

            // Requirement 8.2: Set refresh token in separate HttpOnly, Secure, SameSite=Lax cookie
            Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
        }

        /// <summary>
        /// Requirement 8.6: Clear both cookies by setting them with expired date
        /// </summary>
        private void ClearTokenCookies()
        {
            var expiredCookieOptions = GetCookieOptions();
            expiredCookieOptions.Expires = DateTime.UtcNow.AddDays(-1);

            Response.Cookies.Append("access_token", string.Empty, expiredCookieOptions);
            Response.Cookies.Append("refresh_token", string.Empty, expiredCookieOptions);
        }

        /// <summary>
        /// Requirement 8.7: Configure cookie options from appsettings.json - not hard-coded
        /// </summary>
        private CookieOptions GetCookieOptions()
        {
            var isHttpsRequest = HttpContext.Request.IsHttps;
            var cookieSecure = GetCookieSecure() && isHttpsRequest;

            return new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(
                    int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var minutes) ? minutes : 10),
                HttpOnly = true,  // Required for security
                Secure = cookieSecure,
                SameSite = SameSiteMode.Lax,
                Domain = ShouldSetCookieDomain() ? GetCookieDomain() : null,
                Path = "/"
            };
        }

        private bool ShouldSetCookieDomain()
        {
            var cookieDomain = GetCookieDomain();
            if (string.IsNullOrWhiteSpace(cookieDomain))
            {
                return false;
            }

            var requestHost = HttpContext.Request.Host.Host;
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
            // In production (HTTPS), Secure should be true. In development, it can be false.
            var secureStr = _configuration["Cookie:Secure"];
            return bool.TryParse(secureStr, out var secure) ? secure : !HttpContext.Request.IsHttps;
        }

        private string GetCookieDomain()
        {
            // Requirement 8.10: Set cookie Domain to e-verland.site (configurable)
            return _configuration["Cookie:Domain"] ?? "e-verland.site";
        }

        private Guid? GetUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
