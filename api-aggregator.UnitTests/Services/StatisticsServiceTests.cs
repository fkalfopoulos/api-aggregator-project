using api_aggregator.Abstractions;
using api_aggregator.Services;
using FluentAssertions;
using Moq;

namespace api_aggregator.UnitTests.Services;

public class StatisticsServiceTests
{
    private readonly Mock<IPerformanceAnalyticsService> _mockPerformanceAnalytics;

    public StatisticsServiceTests()
    {
        _mockPerformanceAnalytics = new Mock<IPerformanceAnalyticsService>();
    }

    [Fact]
    public void RecordSuccess_ShouldIncrement_TotalAndSuccessfulRequests()
    {
        // Arrange
        var service = new StatisticsService(_mockPerformanceAnalytics.Object);
        
        // Act
        var result = service.RecordSuccess("TestApi", 100);
        
        // Assert
        result.Success.Should().BeTrue();
        
        var statsResult = service.GetStatistics("TestApi");
        statsResult.Success.Should().BeTrue();
        statsResult.Value.Should().NotBeNull();
        statsResult.Value!.TotalRequests.Should().Be(1);
        statsResult.Value.SuccessfulRequests.Should().Be(1);
        statsResult.Value.FailedRequests.Should().Be(0);
        
        // Verify performance analytics was called
        _mockPerformanceAnalytics.Verify(x => x.RecordMetric("TestApi", 100), Times.Once);
    }

    [Fact]
    public void RecordFailure_ShouldIncrement_TotalAndFailedRequests()
    {
        // Arrange
        var service = new StatisticsService(_mockPerformanceAnalytics.Object);
        
        // Act
        var result = service.RecordFailure("TestApi", 500);
        
        // Assert
        result.Success.Should().BeTrue();
        
        var statsResult = service.GetStatistics("TestApi");
        statsResult.Success.Should().BeTrue();
        statsResult.Value.Should().NotBeNull();
        statsResult.Value!.TotalRequests.Should().Be(1);
        statsResult.Value.SuccessfulRequests.Should().Be(0);
        statsResult.Value.FailedRequests.Should().Be(1);
        
        // Verify performance analytics was called
        _mockPerformanceAnalytics.Verify(x => x.RecordMetric("TestApi", 500), Times.Once);
    }

    [Theory]
    [InlineData(150, 1, 0, 0)] // Fast
    [InlineData(300, 0, 1, 0)] // Average
    [InlineData(600, 0, 0, 1)] // Slow
    public void RecordSuccess_ShouldCategorize_PerformanceBuckets(
        long responseTime, 
        int expectedFast, 
        int expectedAverage, 
        int expectedSlow)
    {
        // Arrange
        var service = new StatisticsService(_mockPerformanceAnalytics.Object);
        
        // Act
        var result = service.RecordSuccess("TestApi", responseTime);
        
        // Assert
        result.Success.Should().BeTrue();
        
        var statsResult = service.GetStatistics("TestApi");
        statsResult.Success.Should().BeTrue();
        statsResult.Value.Should().NotBeNull();
        statsResult.Value!.Buckets.Fast.Should().Be(expectedFast);
        statsResult.Value.Buckets.Average.Should().Be(expectedAverage);
        statsResult.Value.Buckets.Slow.Should().Be(expectedSlow);
    }

    [Fact]
    public void RecordSuccess_WithCache_ShouldTrack_CacheHitRate()
    {
        // Arrange
        var service = new StatisticsService(_mockPerformanceAnalytics.Object);
        
        // Act
        service.RecordSuccess("TestApi", 50, fromCache: true);
        service.RecordSuccess("TestApi", 60, fromCache: false);
        
        // Assert
        var statsResult = service.GetStatistics("TestApi");
        statsResult.Success.Should().BeTrue();
        statsResult.Value.Should().NotBeNull();
        statsResult.Value!.CacheHitRate.Should().Be(50.0); // 1 out of 2
    }

    [Fact]
    public void GetStatistics_WhenNoData_ShouldReturn_Null()
    {
        // Arrange
        var service = new StatisticsService(_mockPerformanceAnalytics.Object);
        
        // Act
        var result = service.GetStatistics("NonExistentApi");
        
        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void GetAllStatistics_ShouldReturn_AllApiStats()
    {
        // Arrange
        var service = new StatisticsService(_mockPerformanceAnalytics.Object);
        service.RecordSuccess("Api1", 100);
        service.RecordSuccess("Api2", 200);
        
        // Act
        var result = service.GetAllStatistics();
        
        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ApiStats.Should().HaveCount(2);
        result.Value.ApiStats.Should().Contain(s => s.ApiName == "Api1");
        result.Value.ApiStats.Should().Contain(s => s.ApiName == "Api2");
    }

    [Fact]
    public void ResetStatistics_ShouldClear_AllData()
    {
        // Arrange
        var service = new StatisticsService(_mockPerformanceAnalytics.Object);
        service.RecordSuccess("TestApi", 100);
        
        // Act
        var resetResult = service.ResetStatistics();
        
        // Assert
        resetResult.Success.Should().BeTrue();
        
        var statsResult = service.GetStatistics("TestApi");
        statsResult.Success.Should().BeTrue();
        statsResult.Value.Should().BeNull();
    }
}
