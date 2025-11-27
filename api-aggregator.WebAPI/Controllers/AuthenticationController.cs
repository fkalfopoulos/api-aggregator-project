using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_aggregator.WebAPI.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Login with username and password to get JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    /// <remarks>
    /// Demo users:
    /// - Username: admin, Password: admin123, Roles: Admin, User
    /// - Username: user, Password: user123, Roles: User
    /// - Username: readonly, Password: readonly123, Roles: ReadOnly
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        var result = await _authenticationService.AuthenticateAsync(request.Username, request.Password);

        if (!result.Success)
        {
            _logger.LogWarning("Login failed for user: {Username}. Reason: {Error}", 
                request.Username, result.Error?.Message);
            
            return result.Error?.Code switch
            {
                ApiErrorCode.ValidationError => Unauthorized(new { message = result.Error.Message }),
                _ => BadRequest(new { message = result.Error?.Message ?? "Authentication failed" })
            };
        }

        _logger.LogInformation("Login successful for user: {Username}", request.Username);
        return Ok(result.Value);
    }

    /// <summary>
    /// Test endpoint to verify authentication is working
    /// </summary>
    /// <returns>User information from token claims</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var username = User.Identity?.Name;
        var roles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            username,
            roles,
            authenticated = User.Identity?.IsAuthenticated ?? false
        });
    }
}
