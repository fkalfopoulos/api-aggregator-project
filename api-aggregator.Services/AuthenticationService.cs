using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.Models;
using BCrypt.Net;

namespace api_aggregator.Services;

/// <summary>
/// Service for authenticating users
/// Note: In production, this should connect to a real user database
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ITokenService _tokenService;
    
    // Demo users - In production, this would come from a database
    private readonly Dictionary<string, UserCredentials> _users;

    public AuthenticationService(ITokenService tokenService)
    {
        _tokenService = tokenService;
        
        // Initialize demo users
        _users = new Dictionary<string, UserCredentials>
        {
            ["admin"] = new UserCredentials(
                "admin",
                BCrypt.Net.BCrypt.HashPassword("admin123"),
                new List<string> { "Admin", "User" }
            ),
            ["user"] = new UserCredentials(
                "user",
                BCrypt.Net.BCrypt.HashPassword("user123"),
                new List<string> { "User" }
            ),
            ["readonly"] = new UserCredentials(
                "readonly",
                BCrypt.Net.BCrypt.HashPassword("readonly123"),
                new List<string> { "ReadOnly" }
            )
        };
    }

    public async Task<ServiceResult<LoginResponse>> AuthenticateAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new ServiceResult<LoginResponse>(
                    ApiErrorCode.ValidationError,
                    "Username and password are required");
            }

            if (!_users.TryGetValue(username, out var user))
            {
                return new ServiceResult<LoginResponse>(
                    ApiErrorCode.ValidationError,
                    "Invalid username or password");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return new ServiceResult<LoginResponse>(
                    ApiErrorCode.ValidationError,
                    "Invalid username or password");
            }

            var tokenResult = _tokenService.GenerateToken(user.Username, user.Roles);
            
            return await Task.FromResult(tokenResult);
        }
        catch (Exception ex)
        {
            return new ServiceResult<LoginResponse>(
                ApiErrorCode.GenericError,
                $"Authentication failed: {ex.Message}",
                ex);
        }
    }
}
