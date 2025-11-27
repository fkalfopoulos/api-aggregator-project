using api_aggregator.Abstractions;
using System.Collections.Concurrent;

namespace api_aggregator.Services;

/// <summary>
/// Service for tracking performance metrics over time with time-based windowing
/// </summary>
public class PerformanceAnalyticsService : IPerformanceAnalyticsService
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<PerformanceMetric>> _metrics = new();

    public void RecordMetric(string apiName, long responseTimeMs)
    {
        var metrics = _metrics.GetOrAdd(apiName, _ => new ConcurrentBag<PerformanceMetric>());
        metrics.Add(new PerformanceMetric
        {
            Timestamp = DateTime.UtcNow,
            ResponseTimeMs = responseTimeMs
        });
    }

    public double? GetAveragePerformance(string apiName, int timeWindowMinutes)
    {
        if (!_metrics.TryGetValue(apiName, out var metrics) || metrics.IsEmpty)
        {
            return null;
        }

        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);
        var recentMetrics = metrics
            .Where(m => m.Timestamp >= cutoffTime)
            .ToList();

        if (!recentMetrics.Any())
        {
            return null;
        }

        return recentMetrics.Average(m => m.ResponseTimeMs);
    }

    public double? GetOverallAveragePerformance(string apiName)
    {
        if (!_metrics.TryGetValue(apiName, out var metrics) || metrics.IsEmpty)
        {
            return null;
        }

        return metrics.Average(m => m.ResponseTimeMs);
    }

    public IEnumerable<string> GetTrackedApis()
    {
        return _metrics.Keys;
    }

    private class PerformanceMetric
    {
        public DateTime Timestamp { get; set; }
        public long ResponseTimeMs { get; set; }
    }
}
