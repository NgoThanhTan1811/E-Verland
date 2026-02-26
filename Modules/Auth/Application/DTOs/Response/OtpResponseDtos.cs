using System;

namespace Modules.Auth.Application.DTOs.Response
{
    public class SendOtpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
    public class VerifyOtpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    public class RegisterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public UserInfoDto? User { get; set; }
    }

    public class UserInfoDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Role { get; set; } = null!;
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public UserInfoDto? User { get; set; }
    }

    public class RefreshTokenResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
    }

    public class ChangePasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
    }

    public class PasswordResetResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string? Email { get; set; }
    }
}
