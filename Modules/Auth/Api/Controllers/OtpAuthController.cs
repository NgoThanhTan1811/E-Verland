using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.Auth.Application.DTOs.Request;
using Modules.Auth.Application.DTOs.Response;
using Modules.Auth.Application.Services;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Domain.Entities;

namespace Modules.Auth.Api.Controllers
{
    [ApiController]
    [EnableRateLimiting("auth")]
    [Route("api/auth")]
    public class OtpAuthController : ControllerBase
    {
        private readonly IOtpService _otpService;
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<OtpAuthController> _logger;

        public OtpAuthController(
            IOtpService otpService,
            IAccountRepository accountRepository,
            ILogger<OtpAuthController> logger)
        {
            _otpService = otpService;
            _accountRepository = accountRepository;
            _logger = logger;
        }

        //Send OTP 
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email))
                    return BadRequest(new { success = false, message = "Email không được để trống" });

                // check existing email
                var existingUser = await _accountRepository.GetByEmailAsync(dto.Email);
                if (existingUser != null)
                    return BadRequest(new SendOtpResponseDto
                    {
                        Success = false,
                        Message = "Email này đã được đăng ký",
                        Email = dto.Email
                    });

                var (success, message) = await _otpService.SendOtpAsync(dto.Email);

                if (!success)
                    return BadRequest(new SendOtpResponseDto
                    {
                        Success = false,
                        Message = message,
                        Email = dto.Email
                    });

                return Ok(new SendOtpResponseDto
                {
                    Success = true,
                    Message = message,
                    Email = dto.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending OTP: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Lỗi khi xử lý yêu cầu" });
            }
        }

        // verify OTP
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email) || string.IsNullOrWhiteSpace(dto?.OtpCode))
                    return BadRequest(new { success = false, message = "Email hoặc OTP không được để trống" });

                var (success, message) = await _otpService.VerifyOtpAsync(dto.Email, dto.OtpCode);

                if (!success)
                    return BadRequest(new VerifyOtpResponseDto
                    {
                        Success = false,
                        Message = message,
                        Email = dto.Email
                    });

                return Ok(new VerifyOtpResponseDto
                {
                    Success = true,
                    Message = message,
                    Email = dto.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying OTP: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Lỗi khi xử lý yêu cầu" });
            }
        }

        // register
        [HttpPost("register-with-otp")]
        public async Task<IActionResult> RegisterWithOtp([FromBody] RegisterWithOtpRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email) || string.IsNullOrWhiteSpace(dto?.OtpCode) ||
                    string.IsNullOrWhiteSpace(dto?.Username) || string.IsNullOrWhiteSpace(dto?.Password))
                    return BadRequest(new RegisterResponseDto
                    {
                        Success = false,
                        Message = "Vui lòng điền đầy đủ thông tin"
                    });

                // check OTP is verified
                var verifiedOtp = await _otpService.GetPendingOtpAsync(dto.Email);
                if (verifiedOtp == null || verifiedOtp.OtpCode != dto.OtpCode)
                    return BadRequest(new RegisterResponseDto
                    {
                        Success = false,
                        Message = "OTP không hợp lệ hoặc đã hết hạn"
                    });

                // check existing email
                var existingUser = await _accountRepository.GetByEmailAsync(dto.Email);
                if (existingUser != null)
                    return BadRequest(new RegisterResponseDto
                    {
                        Success = false,
                        Message = "Email này đã được đăng ký"
                    });

                // check existing username
                var existingUsername = await _accountRepository.GetByUsernameAsync(dto.Username);
                if (existingUsername != null)
                    return BadRequest(new RegisterResponseDto
                    {
                        Success = false,
                        Message = "Username này đã tồn tại"
                    });

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var newAccount = new Account(dto.Email, dto.Username, passwordHash);

                await _accountRepository.CreateAsync(newAccount);

                _logger.LogInformation($"Account registered successfully with email: {dto.Email}");

                return Ok(new RegisterResponseDto
                {
                    Success = true,
                    Message = "Tài khoản đã được tạo thành công",
                    User = new UserInfoDto
                    {
                        Id = newAccount.Id,
                        Email = newAccount.Email,
                        Username = newAccount.Username,
                        Role = newAccount.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering with OTP: {ex.Message}");
                return StatusCode(500, new RegisterResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi tạo tài khoản"
                });
            }
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] SendOtpRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email))
                    return BadRequest(new { success = false, message = "Email không được để trống" });

                var (success, message) = await _otpService.SendOtpAsync(dto.Email);

                if (!success)
                    return BadRequest(new { success = false, message = message });

                return Ok(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error resending OTP: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Lỗi khi gửi lại OTP" });
            }
        }
    }
}
