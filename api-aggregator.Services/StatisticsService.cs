using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.Models;
using System.Collections.Concurrent;

namespace api_aggregator.Services;

/// <summary>
/// In-memory statistics tracking service
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly ConcurrentDictionary<string, ApiMetrics> _metrics = new();
    private readonly IPerformanceAnalyticsService _performanceAnalytics;

    public StatisticsService(IPerformanceAnalyticsService performanceAnalytics)
    {
        _performanceAnalytics = performanceAnalytics;
    }

    public VoidApiResult<ApiErrorCode> RecordSuccess(string apiName, long responseTimeMs, bool fromCache = false)
    {
        try
        {
            var metrics = _metrics.GetOrAdd(apiName, _ => new ApiMetrics());
            
            metrics.TotalRequests++;
            metrics.SuccessfulRequests++;
            metrics.ResponseTimes.Add(responseTimeMs);
            
            if (fromCache)
            {
                metrics.CacheHits++;
            }

            UpdatePerformanceBucket(metrics, responseTimeMs);
            
            _performanceAnalytics.RecordMetric(apiName, responseTimeMs);
            
            return new VoidApiResult<ApiErrorCode>();
        }
        catch (Exception ex)
        {
            return new VoidApiResult<ApiErrorCode>(ApiErrorCode.GenericError, $"Error recording success: {ex.Message}", ex);
        }
    }

    public VoidApiResult<ApiErrorCode> RecordFailure(string apiName, long responseTimeMs)
    {
        try
        {
            var metrics = _metrics.GetOrAdd(apiName, _ => new ApiMetrics());
            
            metrics.TotalRequests++;
            metrics.FailedRequests++;
            metrics.ResponseTimes.Add(responseTimeMs);
            
            UpdatePerformanceBucket(metrics, responseTimeMs);
            
            _performanceAnalytics.RecordMetric(apiName, responseTimeMs);
            
            return new VoidApiResult<ApiErrorCode>();
        }
        catch (Exception ex)
        {
            return new VoidApiResult<ApiErrorCode>(ApiErrorCode.GenericError, $"Error recording failure: {ex.Message}", ex);
        }
    }

    public ServiceResult<ApiStatistics?> GetStatistics(string apiName)
    {
        try
        {
            if (!_metrics.TryGetValue(apiName, out var metrics))
            {
                return (ApiStatistics?)null;
            }

            return BuildStatistics(apiName, metrics);
        }
        catch (Exception ex)
        {
            return new ServiceResult<ApiStatistics?>(ApiErrorCode.GenericError, $"Error getting statistics: {ex.Message}", ex);
        }
    }

    public ServiceResult<StatisticsResponse> GetAllStatistics()
    {
        try
        {
            var response = new StatisticsResponse();

            foreach (var kvp in _metrics)
            {
                var stats = BuildStatistics(kvp.Key, kvp.Value);
                response.ApiStats.Add(stats);
            }

            if (response.ApiStats.Any())
            {
                response.Overall.TotalRequests = response.ApiStats.Sum(s => s.TotalRequests);
                response.Overall.AverageResponseTimeMs = response.ApiStats.Average(s => s.AverageResponseTimeMs);
                
                var totalSuccessful = response.ApiStats.Sum(s => s.SuccessfulRequests);
                var totalRequests = response.Overall.TotalRequests;
                response.Overall.SuccessRate = totalRequests > 0 
                    ? Math.Round((double)totalSuccessful / totalRequests * 100, 2) 
                    : 0;
            }

            return response;
        }
        catch (Exception ex)
        {
            return new ServiceResult<StatisticsResponse>(ApiErrorCode.GenericError, $"Error getting all statistics: {ex.Message}", ex);
        }
    }

    public VoidApiResult<ApiErrorCode> ResetStatistics()
    {
        try
        {
            _metrics.Clear();
            return new VoidApiResult<ApiErrorCode>();
        }
        catch (Exception ex)
        {
            return new VoidApiResult<ApiErrorCode>(ApiErrorCode.GenericError, $"Error resetting statistics: {ex.Message}", ex);
        }
    }

    private static void UpdatePerformanceBucket(ApiMetrics metrics, long responseTimeMs)
    {
        if (responseTimeMs < 200)
        {
            metrics.FastRequests++;
        }
        else if (responseTimeMs <= 500)
        {
            metrics.AverageRequests++;
        }
        else
        {
            metrics.SlowRequests++;
        }
    }

    private static ApiStatistics BuildStatistics(string apiName, ApiMetrics metrics)
    {
        var stats = new ApiStatistics
        {
            ApiName = apiName,
            TotalRequests = metrics.TotalRequests,
            SuccessfulRequests = metrics.SuccessfulRequests,
            FailedRequests = metrics.FailedRequests,
            AverageResponseTimeMs = metrics.ResponseTimes.Any() 
                ? Math.Round(metrics.ResponseTimes.Average(), 2) 
                : 0,
            Buckets = new PerformanceBuckets
            {
                Fast = metrics.FastRequests,
                Average = metrics.AverageRequests,
                Slow = metrics.SlowRequests
            },
            CacheHitRate = metrics.TotalRequests > 0 
                ? Math.Round((double)metrics.CacheHits / metrics.TotalRequests * 100, 2) 
                : 0
        };

        return stats;
    }

    private class ApiMetrics
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public int CacheHits { get; set; }
        public int FastRequests { get; set; }
        public int AverageRequests { get; set; }
        public int SlowRequests { get; set; }
        public List<long> ResponseTimes { get; } = new();
    }
}
