using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SRM.Api.Models.Enums;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SRM.Api.Services
{
    public class TokenService : ITokenService
    {
        public Result<string> CreateToken(Guid id, string email, UserType userType)
        {

            var envKey = Environment.GetEnvironmentVariable("SECRET_KEY")
                ?? throw new InvalidOperationException("KEY is not configured");

            var issuer = Environment.GetEnvironmentVariable("ISSUER")
                ?? throw new InvalidOperationException("ISSUER is not configured");

            var audience = Environment.GetEnvironmentVariable("AUDIENCE")
                ?? throw new InvalidOperationException("AUDIENCE is not configured");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, userType.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(envKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return Result<string>.Ok(new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}
