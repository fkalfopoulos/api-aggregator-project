using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace api_aggregator.Services;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public ServiceResult<LoginResponse> GenerateToken(string username, List<string> roles)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new ServiceResult<LoginResponse>(
                    ApiErrorCode.ValidationError,
                    "Username cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(_jwtOptions.SecretKey) || _jwtOptions.SecretKey.Length < 32)
            {
                return new ServiceResult<LoginResponse>(
                    ApiErrorCode.GenericError,
                    "JWT secret key must be at least 32 characters");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);
            
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username),
                new(JwtRegisteredClaimNames.Sub, username),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var response = new LoginResponse
            {
                Token = tokenString,
                ExpiresAt = tokenDescriptor.Expires.Value,
                Username = username,
                Roles = roles
            };

            return response;
        }
        catch (Exception ex)
        {
            return new ServiceResult<LoginResponse>(ApiErrorCode.GenericError,$"Failed to generate token: {ex.Message}",ex);
        }
    }

}
