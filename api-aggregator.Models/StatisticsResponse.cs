namespace api_aggregator.Models;

/// <summary>
/// Statistics response containing performance metrics for all APIs
/// </summary>
public class StatisticsResponse
{
    /// <summary>
    /// Statistics per API
    /// </summary>
    public List<ApiStatistics> ApiStats { get; set; } = new();

    /// <summary>
    /// Overall statistics across all APIs
    /// </summary>
    public OverallStatistics Overall { get; set; } = new();
}

/// <summary>
/// Performance statistics for a single API
/// </summary>
public class ApiStatistics
{
    /// <summary>
    /// API name
    /// </summary>
    public string ApiName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of requests made to this API
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Number of successful requests
    /// </summary>
    public int SuccessfulRequests { get; set; }

    /// <summary>
    /// Number of failed requests
    /// </summary>
    public int FailedRequests { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Performance buckets breakdown
    /// </summary>
    public PerformanceBuckets Buckets { get; set; } = new();

    /// <summary>
    /// Cache hit rate percentage (0-100)
    /// </summary>
    public double CacheHitRate { get; set; }
}

/// <summary>
/// Performance bucket distribution
/// </summary>
public class PerformanceBuckets
{
    /// <summary>
    /// Number of fast requests (< 200ms)
    /// </summary>
    public int Fast { get; set; }

    /// <summary>
    /// Number of average requests (200-500ms)
    /// </summary>
    public int Average { get; set; }

    /// <summary>
    /// Number of slow requests (> 500ms)
    /// </summary>
    public int Slow { get; set; }
}

/// <summary>
/// Overall statistics across all APIs
/// </summary>
public class OverallStatistics
{
    /// <summary>
    /// Total requests across all APIs
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Average response time across all APIs in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Overall success rate percentage (0-100)
    /// </summary>
    public double SuccessRate { get; set; }
}
