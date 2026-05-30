using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Modules.User.Domain.Entities;

namespace Modules.Auth.Application.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(Account account);
        string GenerateRefreshToken();
        TimeSpan AccessTokenLifetime { get; }
        TimeSpan RefreshTokenLifetime { get; }
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(GetIntSetting("Jwt:AccessTokenMinutes", 30));

        public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(GetIntSetting("Jwt:RefreshTokenDays", 7));

        public string GenerateAccessToken(Account account)
        {
            var key = GetJwtKey();
            var issuer = GetJwtIssuer();
            var audience = GetJwtAudience();

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new(ClaimTypes.Email, account.Email),
                new(ClaimTypes.Name, account.Username),
                new(ClaimTypes.Role, account.Role.ToString()),
                // Add custom "role" claim for Requirement 6 (Role-based authorization policies)
                new("role", account.Role.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.Add(AccessTokenLifetime);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes).TrimEnd('=');
        }

        private byte[] GetJwtKey()
        {
            var rawKey =  _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(rawKey))
                throw new InvalidOperationException("JWT Key is not configured.");

            return Encoding.UTF8.GetBytes(rawKey);
        }

        private string GetJwtIssuer()
        {
            return _configuration["Jwt:Issuer"]!;
        }

        private string GetJwtAudience()
        {
            return _configuration["Jwt:Audience"]!;
               
        }

        private int GetIntSetting(string key, int fallback)
        {
            var raw = _configuration[key];
            return int.TryParse(raw, out var value) ? value : fallback;
        }
    }
}
