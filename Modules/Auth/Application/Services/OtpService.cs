using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Auth.Domain;
using Modules.Auth.Infrastructure.Persistence;
using Modules.Auth.Infrastructure.Services;

namespace Modules.Auth.Application.Services
{
    public interface IOtpService
    {
        Task<(bool success, string message)> SendOtpAsync(string email);
        Task<(bool success, string message)> VerifyOtpAsync(string email, string otpCode);
        Task<EmailVerificationOtp?> GetPendingOtpAsync(string email);
        Task InvalidateOtpAsync(string email);
    }

    public class OtpService : IOtpService
    {
        private readonly AuthDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly ILogger<OtpService> _logger;

        public OtpService(
            AuthDbContext dbContext,
            IEmailService emailService,
            ILogger<OtpService> logger)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _logger = logger;
        }


        public async Task<(bool success, string message)> SendOtpAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email không hợp lệ");

                // Kiểm tra xem đã gửi OTP chưa (trong 1 phút)
                var existingOtp = await _dbContext.EmailVerificationOtps
                    .Where(o => o.Email == email && !o.IsVerified && o.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingOtp != null && (DateTime.UtcNow - existingOtp.CreatedAt).TotalSeconds < 60)
                {
                    return (false, "Vui lòng đợi 60 giây trước khi gửi lại OTP");
                }

                // Tạo OTP code 6 số
                var otpCode = GenerateOtpCode();

                var verificationOtp = new EmailVerificationOtp
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    OtpCode = otpCode,
                    IsVerified = false,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(EmailVerificationOtp.ExpirationMinutes),
                    CreatedAt = DateTime.UtcNow,
                    AttemptCount = 0
                };

                // Xóa các OTP cũ (chưa verify) cho email này
                var oldOtps = await _dbContext.EmailVerificationOtps
                    .Where(o => o.Email == email && !o.IsVerified)
                    .ToListAsync();

                _dbContext.EmailVerificationOtps.RemoveRange(oldOtps);
                _dbContext.EmailVerificationOtps.Add(verificationOtp);
                await _dbContext.SaveChangesAsync();

                // Gửi OTP qua email
                var emailSent = await _emailService.SendOtpEmailAsync(email, otpCode);

                if (!emailSent)
                {
                    _logger.LogWarning($"Failed to send OTP email to {email}");
                    return (false, "Không thể gửi email. Vui lòng thử lại sau");
                }

                _logger.LogInformation($"OTP sent successfully to {email}");
                return (true, "OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending OTP: {ex.Message}");
                return (false, "Lỗi khi gửi OTP. Vui lòng thử lại sau");
            }
        }

        /// Verify OTP code
        public async Task<(bool success, string message)> VerifyOtpAsync(string email, string otpCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
                    return (false, "Email hoặc mã OTP không hợp lệ");

                var otp = await _dbContext.EmailVerificationOtps
                    .FirstOrDefaultAsync(o => o.Email == email && !o.IsVerified && o.ExpiresAt > DateTime.UtcNow);

                if (otp == null)
                    return (false, "Không tìm thấy OTP hoặc OTP đã hết hạn");

                // Kiểm tra số lần nhập sai
                if (otp.AttemptCount >= EmailVerificationOtp.MaxAttempts)
                    return (false, "Bạn đã nhập sai quá nhiều lần. Vui lòng yêu cầu gửi lại OTP");

                // Verify OTP code
                if (otp.OtpCode != otpCode)
                {
                    otp.AttemptCount++;
                    _dbContext.EmailVerificationOtps.Update(otp);
                    await _dbContext.SaveChangesAsync();

                    var attemptsLeft = EmailVerificationOtp.MaxAttempts - otp.AttemptCount;
                    return (false, $"Mã OTP không đúng. Bạn còn {attemptsLeft} lần thử");
                }

                // Mark OTP as verified
                otp.IsVerified = true;
                otp.VerifiedAt = DateTime.UtcNow;
                _dbContext.EmailVerificationOtps.Update(otp);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"OTP verified successfully for {email}");
                return (true, "Mã OTP xác minh thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying OTP: {ex.Message}");
                return (false, "Lỗi khi xác minh OTP. Vui lòng thử lại sau");
            }
        }

        public async Task<EmailVerificationOtp?> GetPendingOtpAsync(string email)
        {
            return await _dbContext.EmailVerificationOtps
                .FirstOrDefaultAsync(o => o.Email == email && o.IsVerified && o.ExpiresAt > DateTime.UtcNow);
        }

        public async Task InvalidateOtpAsync(string email)
        {
            var otps = await _dbContext.EmailVerificationOtps
                .Where(o => o.Email == email)
                .ToListAsync();

            if (otps.Count == 0)
                return;

            _dbContext.EmailVerificationOtps.RemoveRange(otps);
            await _dbContext.SaveChangesAsync();
        }

        private string GenerateOtpCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
