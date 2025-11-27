using api_aggregator.Models;
using api_aggregator.Services.Models;

namespace api_aggregator.Abstractions;

/// <summary>
/// Service for tracking and retrieving API performance statistics
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Record a successful API call
    /// </summary>
    /// <param name="apiName">Name of the API</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <param name="fromCache">Whether the data was served from cache</param>
    VoidApiResult<ApiErrorCode> RecordSuccess(string apiName, long responseTimeMs, bool fromCache = false);

    /// <summary>
    /// Record a failed API call
    /// </summary>
    /// <param name="apiName">Name of the API</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    VoidApiResult<ApiErrorCode> RecordFailure(string apiName, long responseTimeMs);

    /// <summary>
    /// Get statistics for a specific API
    /// </summary>
    /// <param name="apiName">Name of the API</param>
    /// <returns>API statistics or null if no data exists</returns>
    ServiceResult<ApiStatistics?> GetStatistics(string apiName);

    /// <summary>
    /// Get statistics for all APIs
    /// </summary>
    /// <returns>Statistics response with all API metrics</returns>
    ServiceResult<StatisticsResponse> GetAllStatistics();

    /// <summary>
    /// Reset all statistics
    /// </summary>
    VoidApiResult<ApiErrorCode> ResetStatistics();
}
