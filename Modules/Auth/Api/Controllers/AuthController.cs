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

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
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
                var result = await _authService.RefreshAsync(dto);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
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

        private Guid? GetUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
