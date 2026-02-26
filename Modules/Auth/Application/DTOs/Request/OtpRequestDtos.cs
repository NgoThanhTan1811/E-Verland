namespace Modules.Auth.Application.DTOs.Request
{

    public class SendOtpRequestDto
    {
        public string Email { get; set; } = null!;
    }

    public class VerifyOtpRequestDto
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
    }

    public class RegisterWithOtpRequestDto
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginRequestDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class RefreshTokenRequestDto
    {
        public string Email { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }

    public class ChangePasswordRequestDto
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class RequestPasswordResetOtpDto
    {
        public string Email { get; set; } = null!;
    }

    public class VerifyPasswordResetOtpDto
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
    }

    public class ResetPasswordRequestDto
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
