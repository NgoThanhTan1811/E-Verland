using System;
using System.Threading.Tasks;
using Modules.Auth.Application.DTOs.Request;
using Modules.Auth.Application.DTOs.Response;
using Modules.Redis.Services;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Domain.Enums;

namespace Modules.Auth.Application.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<RefreshTokenResponseDto> RefreshAsync(RefreshTokenRequestDto request);
        Task<ChangePasswordResponseDto> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);
        Task<PasswordResetResponseDto> RequestPasswordResetOtpAsync(RequestPasswordResetOtpDto request);
        Task<PasswordResetResponseDto> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpDto request);
        Task<PasswordResetResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
    }

    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUserDbContext _userDbContext;
        private readonly IJwtCacheService _jwtCacheService;
        private readonly ITokenService _tokenService;
        private readonly IOtpService _otpService;

        public AuthService(
            IAccountRepository accountRepository,
            IUserDbContext userDbContext,
            IJwtCacheService jwtCacheService,
            ITokenService tokenService,
            IOtpService otpService)
        {
            _accountRepository = accountRepository;
            _userDbContext = userDbContext;
            _jwtCacheService = jwtCacheService;
            _tokenService = tokenService;
            _otpService = otpService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không được để trống"
                };
            }

            var account = await _accountRepository.GetByEmailAsync(request.Email);
            if (account == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không đúng"
                };
            }

            if (account.Status != StatusUser.Active)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Tài khoản đang bị khóa hoặc không hoạt động"
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, account.Password))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không đúng"
                };
            }

            var accessToken = _tokenService.GenerateAccessToken(account);
            var refreshToken = _tokenService.GenerateRefreshToken();

            await _jwtCacheService.CacheTokenAsync(account.Id.ToString(), refreshToken, _tokenService.RefreshTokenLifetime);

            var accessExpiresAt = DateTime.UtcNow.Add(_tokenService.AccessTokenLifetime);
            var refreshExpiresAt = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime);

            return new LoginResponseDto
            {
                Success = true,
                Message = "Login successful",
                Token = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessExpiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt,
                User = new UserInfoDto
                {
                    Id = account.Id,
                    Email = account.Email,
                    Username = account.Username,
                    Role = account.Role.ToString()
                }
            };
        }

        public async Task<RefreshTokenResponseDto> RefreshAsync(RefreshTokenRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return new RefreshTokenResponseDto
                {
                    Success = false,
                    Message = "Email hoặc refresh token không hợp lệ"
                };
            }

            var account = await _accountRepository.GetByEmailAsync(request.Email);
            if (account == null)
            {
                return new RefreshTokenResponseDto
                {
                    Success = false,
                    Message = "Email hoặc refresh token không đúng"
                };
            }

            if (account.Status != StatusUser.Active)
            {
                return new RefreshTokenResponseDto
                {
                    Success = false,
                    Message = "Tài khoản đang bị khóa hoặc không hoạt động"
                };
            }

            var cachedRefreshToken = await _jwtCacheService.GetTokenAsync(account.Id.ToString());
            if (cachedRefreshToken == null || !string.Equals(cachedRefreshToken, request.RefreshToken, StringComparison.Ordinal))
            {
                return new RefreshTokenResponseDto
                {
                    Success = false,
                    Message = "Refresh token không hợp lệ hoặc đã hết hạn"
                };
            }

            var accessToken = _tokenService.GenerateAccessToken(account);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            await _jwtCacheService.CacheTokenAsync(account.Id.ToString(), newRefreshToken, _tokenService.RefreshTokenLifetime);

            return new RefreshTokenResponseDto
            {
                Success = true,
                Message = "Refresh token thành công",
                Token = accessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.Add(_tokenService.AccessTokenLifetime),
                RefreshTokenExpiresAt = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime)
            };
        }

        public async Task<ChangePasswordResponseDto> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
        {
            var account = await _accountRepository.GetByIdAsync(userId);
            if (account == null)
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Tài khoản không tồn tại"
                };
            }

            if (account.Status != StatusUser.Active)
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Tài khoản đang bị khóa hoặc không hoạt động"
                };
            }   

            if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Vui lòng nhập đầy đủ mật khẩu"
                };
            }

            if (!IsPasswordValid(request.NewPassword))
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Mật khẩu mới phải dài hơn 3 ký tự"
                };
            }


            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, account.Password))
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Mật khẩu hiện tại không đúng"
                };
            }

            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, account.Password))
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Mật khẩu mới không được trùng với mật khẩu cũ"
                };
            }

            account.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _accountRepository.UpdateAsync(account);
            await _userDbContext.SaveChangesAsync();

            return new ChangePasswordResponseDto
            {
                Success = true,
                Message = "Đổi mật khẩu thành công"
            };
        }

        public async Task<PasswordResetResponseDto> RequestPasswordResetOtpAsync(RequestPasswordResetOtpDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Email không được để trống"
                };
            }

            var account = await _accountRepository.GetByEmailAsync(request.Email);
            if (account == null)
            {
                return new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Email không tồn tại",
                    Email = request.Email
                };
            }

            var (success, message) = await _otpService.SendOtpAsync(request.Email);
            return new PasswordResetResponseDto
            {
                Success = success,
                Message = message,
                Email = request.Email
            };
        }

        public async Task<PasswordResetResponseDto> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Email hoặc OTP không hợp lệ",
                    Email = request.Email
                };
            }

            var (success, message) = await _otpService.VerifyOtpAsync(request.Email, request.OtpCode);
            return new PasswordResetResponseDto
            {
                Success = success,
                Message = message,
                Email = request.Email
            };
        }

        public async Task<PasswordResetResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Vui lòng nhập đầy đủ thông tin",
                    Email = request.Email
                };
            }

            if (!IsPasswordValid(request.NewPassword))
            {
                return new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Mật khẩu mới phải dài hơn 3 ký tự",
                    Email = request.Email
                };
            }

            var pendingOtp = await _otpService.GetPendingOtpAsync(request.Email);
            if (pendingOtp == null || !string.Equals(pendingOtp.OtpCode, request.OtpCode, StringComparison.Ordinal))
            {
                return new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "OTP không hợp lệ hoặc đã hết hạn",
                    Email = request.Email
                };
            }

            var account = await _accountRepository.GetByEmailAsync(request.Email);
            if (account == null)
            {
                return new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Email không tồn tại",
                    Email = request.Email
                };
            }

            account.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _accountRepository.UpdateAsync(account);
            await _userDbContext.SaveChangesAsync();
            await _otpService.InvalidateOtpAsync(request.Email);

            return new PasswordResetResponseDto
            {
                Success = true,
                Message = "Đặt lại mật khẩu thành công",
                Email = request.Email
            };
        }

        private static bool IsPasswordValid(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length > 3;
        }
    }
}