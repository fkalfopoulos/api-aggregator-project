namespace api_aggregator.Abstractions;

/// <summary>
/// Service for tracking and analyzing performance metrics over time
/// </summary>
public interface IPerformanceAnalyticsService
{
    /// <summary>
    /// Record a performance metric for analysis
    /// </summary>
    /// <param name="apiName">API name</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    void RecordMetric(string apiName, long responseTimeMs);

    /// <summary>
    /// Get the average performance for an API over a time window
    /// </summary>
    /// <param name="apiName">API name</param>
    /// <param name="timeWindowMinutes">Time window in minutes</param>
    /// <returns>Average response time in milliseconds, or null if no data</returns>
    double? GetAveragePerformance(string apiName, int timeWindowMinutes);

    /// <summary>
    /// Get the overall average performance for an API (all time)
    /// </summary>
    /// <param name="apiName">API name</param>
    /// <returns>Average response time in milliseconds, or null if no data</returns>
    double? GetOverallAveragePerformance(string apiName);

    /// <summary>
    /// Get all API names that have recorded metrics
    /// </summary>
    /// <returns>List of API names</returns>
    IEnumerable<string> GetTrackedApis();
}
