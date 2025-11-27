using api_aggregator.Models;
using api_aggregator.Services.Models;

namespace api_aggregator.Abstractions;

/// <summary>
/// Service for JWT token generation and validation
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate a JWT token for a user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="roles">User roles</param>
    /// <returns>JWT token and expiration info</returns>
    ServiceResult<LoginResponse> GenerateToken(string username, List<string> roles);
}
