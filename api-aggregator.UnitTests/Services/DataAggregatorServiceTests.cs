using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services;
using api_aggregator.Services.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace api_aggregator.UnitTests.Services;

public class DataAggregatorServiceTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IStatisticsService> _mockStatisticsService;
    private readonly Mock<ILogger<DataAggregatorService>> _mockLogger;
    private readonly AggregationOptions _options;

    public DataAggregatorServiceTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockStatisticsService = new Mock<IStatisticsService>();
        _mockLogger = new Mock<ILogger<DataAggregatorService>>();
        _options = new AggregationOptions
        {
            RequireAllApis = false,
            CacheDurationMinutes = 5
        };

        // Setup default mock responses for cache and statistics
        _mockCacheService.Setup(x => x.Get<List<DataItem>>(It.IsAny<string>()))
            .Returns(new ServiceResult<List<DataItem>?>(default(List<DataItem>)));
        
        _mockCacheService.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<List<DataItem>>(), It.IsAny<int>()))
            .Returns(new VoidApiResult<ApiErrorCode>());

        _mockStatisticsService.Setup(x => x.RecordSuccess(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<bool>()))
            .Returns(new VoidApiResult<ApiErrorCode>());

        _mockStatisticsService.Setup(x => x.RecordFailure(It.IsAny<string>(), It.IsAny<long>()))
            .Returns(new VoidApiResult<ApiErrorCode>());
    }

    [Fact]
    public async Task AggregateDataAsync_WithSuccessfulApis_ShouldReturn_AggregatedData()
    {
        // Arrange
        var mockApi1 = CreateMockApiService("Api1", new List<DataItem>
        {
            new() { Id = "1", Title = "Item1", Source = "Api1", Timestamp = DateTime.UtcNow }
        });

        var mockApi2 = CreateMockApiService("Api2", new List<DataItem>
        {
            new() { Id = "2", Title = "Item2", Source = "Api2", Timestamp = DateTime.UtcNow }
        });

        var apiServices = new List<IExternalApiService> { mockApi1.Object, mockApi2.Object };
        var service = new DataAggregatorService(
            apiServices,
            _mockCacheService.Object,
            _mockStatisticsService.Object,
            _mockLogger.Object,
            Options.Create(_options));

        var request = new AggregationRequest();

        var result = await service.AggregateDataAsync(request);

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Metadata.SuccessfulApis.Should().Contain(new[] { "Api1", "Api2" });
        result.Value.Metadata.FailedApis.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregateDataAsync_WithFailedApi_AndRequireAllFalse_ShouldReturn_PartialData()
    {
        var mockSuccessApi = CreateMockApiService("SuccessApi", new List<DataItem>
        {
            new() { Id = "1", Title = "Item1", Source = "SuccessApi" }
        });

        var mockFailedApi = CreateMockFailedApiService("FailedApi");

        var apiServices = new List<IExternalApiService> { mockSuccessApi.Object, mockFailedApi.Object };
        var service = new DataAggregatorService(
            apiServices,
            _mockCacheService.Object,
            _mockStatisticsService.Object,
            _mockLogger.Object,
            Options.Create(_options));

        var request = new AggregationRequest();

        var result = await service.AggregateDataAsync(request);

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Metadata.SuccessfulApis.Should().Contain("SuccessApi");
        result.Value.Metadata.FailedApis.Should().Contain("FailedApi");
    }

    [Fact]
    public async Task AggregateDataAsync_WithFailedApi_AndRequireAllTrue_ShouldReturnFailure()
    {
        _options.RequireAllApis = true;
        
        var mockSuccessApi = CreateMockApiService("SuccessApi", new List<DataItem>
        {
            new() { Id = "1", Title = "Item1", Source = "SuccessApi" }
        });

        var mockFailedApi = CreateMockFailedApiService("FailedApi");

        var apiServices = new List<IExternalApiService> { mockSuccessApi.Object, mockFailedApi.Object };
        var service = new DataAggregatorService(
            apiServices,
            _mockCacheService.Object,
            _mockStatisticsService.Object,
            _mockLogger.Object,
            Options.Create(_options));

        var request = new AggregationRequest();

        var result = await service.AggregateDataAsync(request);

        result.Failed.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("required APIs failed");
    }

    [Fact]
    public async Task AggregateDataAsync_WithSourceFilter_ShouldQuery_OnlySpecifiedApis()
    {
        var mockApi1 = CreateMockApiService("Api1", new List<DataItem>
        {
            new() { Id = "1", Title = "Item1", Source = "Api1" }
        });

        var mockApi2 = CreateMockApiService("Api2", new List<DataItem>
        {
            new() { Id = "2", Title = "Item2", Source = "Api2" }
        });

        var apiServices = new List<IExternalApiService> { mockApi1.Object, mockApi2.Object };
        var service = new DataAggregatorService(
            apiServices,
            _mockCacheService.Object,
            _mockStatisticsService.Object,
            _mockLogger.Object,
            Options.Create(_options));

        var request = new AggregationRequest
        {
            Sources = new List<string> { "Api1" }
        };

        var result = await service.AggregateDataAsync(request);

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Source.Should().Be("Api1");
        result.Value.Metadata.SuccessfulApis.Should().Contain("Api1");
        result.Value.Metadata.SuccessfulApis.Should().NotContain("Api2");
    }

    [Fact]
    public async Task AggregateDataAsync_WithCategoryFilter_ShouldReturn_FilteredItems()
    {
        var mockApi = CreateMockApiService("Api1", new List<DataItem>
        {
            new() { Id = "1", Title = "Item1", Source = "Api1", Category = "News" },
            new() { Id = "2", Title = "Item2", Source = "Api1", Category = "Weather" }
        });

        var apiServices = new List<IExternalApiService> { mockApi.Object };
        var service = new DataAggregatorService(
            apiServices,
            _mockCacheService.Object,
            _mockStatisticsService.Object,
            _mockLogger.Object,
            Options.Create(_options));

        var request = new AggregationRequest
        {
            Category = "News"
        };

       
        var result = await service.AggregateDataAsync(request);

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Category.Should().Be("News");
    }

    [Fact]
    public async Task AggregateDataAsync_WithSorting_ShouldReturn_SortedItems()
    {
        var mockApi = CreateMockApiService("Api1", new List<DataItem>
        {
            new() { Id = "1", Title = "B", Source = "Api1", RelevanceScore = 50 },
            new() { Id = "2", Title = "A", Source = "Api1", RelevanceScore = 90 },
            new() { Id = "3", Title = "C", Source = "Api1", RelevanceScore = 70 }
        });

        var apiServices = new List<IExternalApiService> { mockApi.Object };
        var service = new DataAggregatorService(
            apiServices,
            _mockCacheService.Object,
            _mockStatisticsService.Object,
            _mockLogger.Object,
            Options.Create(_options));

        var request = new AggregationRequest
        {
            SortBy = "relevance",
            SortDirection = "desc"
        };

        var result = await service.AggregateDataAsync(request);

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items[0].RelevanceScore.Should().Be(90);
        result.Value.Items[1].RelevanceScore.Should().Be(70);
        result.Value.Items[2].RelevanceScore.Should().Be(50);
    }

    [Fact]
    public async Task AggregateDataAsync_WithMaxItems_ShouldReturn_LimitedItems()
    {
        var mockApi = CreateMockApiService("Api1", new List<DataItem>
        {
            new() { Id = "1", Title = "Item1", Source = "Api1" },
            new() { Id = "2", Title = "Item2", Source = "Api1" },
            new() { Id = "3", Title = "Item3", Source = "Api1" }
        });

        var apiServices = new List<IExternalApiService> { mockApi.Object };
        var service = new DataAggregatorService(
            apiServices,
            _mockCacheService.Object,
            _mockStatisticsService.Object,
            _mockLogger.Object,
            Options.Create(_options));

        var request = new AggregationRequest
        {
            MaxItems = 2
        };

        var result = await service.AggregateDataAsync(request);

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
    }

    private Mock<IExternalApiService> CreateMockApiService(string serviceName, List<DataItem> items)
    {
        var mock = new Mock<IExternalApiService>();
        mock.Setup(x => x.ServiceName).Returns(serviceName);
        mock.Setup(x => x.FetchDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceResult<List<DataItem>>(items));
        return mock;
    }

    private Mock<IExternalApiService> CreateMockFailedApiService(string serviceName)
    {
        var mock = new Mock<IExternalApiService>();
        mock.Setup(x => x.ServiceName).Returns(serviceName);
        mock.Setup(x => x.FetchDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceResult<List<DataItem>>(ApiErrorCode.GenericError, "Network error"));
        return mock;
    }
}
