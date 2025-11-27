namespace api_aggregator.Models;

/// <summary>
/// Configuration for API aggregation behavior
/// </summary>
public class AggregationOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Aggregation";

    /// <summary>
    /// Whether all APIs must succeed for the request to be successful
    /// If false, partial results are returned even if some APIs fail
    /// </summary>
    public bool RequireAllApis { get; set; } = false;

    /// <summary>
    /// Timeout for each individual API call in seconds
    /// </summary>
    public int ApiTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Number of retry attempts for failed API calls
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Cache duration in minutes
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 5;
}

/// <summary>
/// Configuration for external API endpoints
/// </summary>
public class ExternalApiOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "ExternalApis";

    /// <summary>
    /// Weather API configuration
    /// </summary>
    public ApiEndpointConfig Weather { get; set; } = new();

    /// <summary>
    /// News API configuration
    /// </summary>
    public ApiEndpointConfig News { get; set; } = new();

    /// <summary>
    /// Users API configuration
    /// </summary>
    public ApiEndpointConfig Users { get; set; } = new();
}

/// <summary>
/// Configuration for a single API endpoint
/// </summary>
public class ApiEndpointConfig
{
    /// <summary>
    /// Base URL of the API
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key or token (if required)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Whether this API is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// JWT configuration options
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key for signing tokens (min 32 characters)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
