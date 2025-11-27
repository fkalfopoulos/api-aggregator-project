using api_aggregator.Models;
using api_aggregator.Services.Models;

namespace api_aggregator.Abstractions;

/// <summary>
/// Service for authenticating users
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate a user with username and password
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Login response with JWT token if successful</returns>
    Task<ServiceResult<LoginResponse>> AuthenticateAsync(string username, string password);
}
