using System;

namespace Modules.Auth.Domain
{

    public class EmailVerificationOtp
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!; // 6-digit code
        public bool IsVerified { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public int AttemptCount { get; set; } // Số lần nhập sai
        public const int MaxAttempts = 3;
        public const int ExpirationMinutes = 10;
    }
}
