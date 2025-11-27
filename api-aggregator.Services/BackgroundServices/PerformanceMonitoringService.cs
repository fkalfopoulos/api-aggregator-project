using api_aggregator.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace api_aggregator.Services.BackgroundServices;

/// <summary>
/// Background service that periodically analyzes performance statistics and logs anomalies
/// </summary>
public class PerformanceMonitoringService : BackgroundService
{
    private readonly IPerformanceAnalyticsService _performanceAnalytics;
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); 
    private readonly int _timeWindowMinutes = 5; 
    private readonly double _anomalyThresholdPercentage = 50.0;

    public PerformanceMonitoringService(
        IPerformanceAnalyticsService performanceAnalytics,
        ILogger<PerformanceMonitoringService> logger)
    {
        _performanceAnalytics = performanceAnalytics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performance Monitoring Service started. Checking every {Interval} minutes.", 
            _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AnalyzePerformanceAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while analyzing performance metrics");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Performance Monitoring Service stopped.");
    }

    private async Task AnalyzePerformanceAsync()
    {
        var trackedApis = _performanceAnalytics.GetTrackedApis().ToList();

        if (!trackedApis.Any())
        {
            _logger.LogDebug("No APIs to analyze yet.");
            return;
        }

        foreach (var apiName in trackedApis)
        {
            try
            {
                var recentAverage = _performanceAnalytics.GetAveragePerformance(apiName, _timeWindowMinutes);
                var overallAverage = _performanceAnalytics.GetOverallAveragePerformance(apiName);

                if (recentAverage == null || overallAverage == null)
                {
                    _logger.LogDebug("Insufficient data for API {ApiName}", apiName);
                    continue;
                }

                // Check if recent performance is significantly worse than overall average
                var percentageIncrease = ((recentAverage.Value - overallAverage.Value) / overallAverage.Value) * 100;

                if (percentageIncrease >= _anomalyThresholdPercentage)
                {
                    _logger.LogWarning(
                        "?? PERFORMANCE ANOMALY DETECTED for API '{ApiName}': " +
                        "Recent average ({RecentAverage:F2}ms over last {TimeWindow} minutes) is {PercentageIncrease:F1}% higher " +
                        "than overall average ({OverallAverage:F2}ms). This exceeds the {Threshold}% threshold.",
                        apiName,
                        recentAverage.Value,
                        _timeWindowMinutes,
                        percentageIncrease,
                        overallAverage.Value,
                        _anomalyThresholdPercentage);
                }
                else
                {
                    _logger.LogDebug(
                        "API '{ApiName}' performance is normal: Recent={RecentAverage:F2}ms, Overall={OverallAverage:F2}ms, Change={PercentageIncrease:F1}%",
                        apiName,
                        recentAverage.Value,
                        overallAverage.Value,
                        percentageIncrease);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing performance for API {ApiName}", apiName);
            }
        }

        await Task.CompletedTask;
    }
}
