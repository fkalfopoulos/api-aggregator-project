using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace api_aggregator.Services;

/// <summary>
/// Service that aggregates data from multiple external APIs
/// </summary>
/// <remarks>
/// This service coordinates data fetching from multiple external API sources,
/// applies caching strategies, handles failures gracefully, and provides
/// filtering and sorting capabilities for aggregated results.
/// </remarks>
public class DataAggregatorService : IDataAggregatorService
{
    private readonly IEnumerable<IExternalApiService> _apiServices;
    private readonly ICacheService _cacheService;
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<DataAggregatorService> _logger;
    private readonly AggregationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataAggregatorService"/> class
    /// </summary>
    /// <param name="apiServices">Collection of external API services to aggregate data from</param>
    /// <param name="cacheService">Service for caching API responses</param>
    /// <param name="statisticsService">Service for recording API call statistics</param>
    /// <param name="logger">Logger for tracking aggregation operations</param>
    /// <param name="options">Configuration options for aggregation behavior</param>
    public DataAggregatorService(
        IEnumerable<IExternalApiService> apiServices,
        ICacheService cacheService,
        IStatisticsService statisticsService, 
        ILogger<DataAggregatorService> logger,
        IOptions<AggregationOptions> options)
    {
        _apiServices = apiServices;
        _cacheService = cacheService;
        _statisticsService = statisticsService;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Aggregates data from multiple external APIs based on the provided request
    /// </summary>
    /// <param name="request">The aggregation request containing filters, sorting, and source selection</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>
    /// A <see cref="ServiceResult{T}"/> containing the aggregated data response if successful,
    /// or an error if the operation fails
    /// </returns>
    /// <remarks>
    /// The method performs the following operations:
    /// <list type="number">
    /// <item><description>Filters API services based on the request sources</description></item>
    /// <item><description>Fetches data from all selected APIs in parallel</description></item>
    /// <item><description>Applies filters (category, date range) to the combined results</description></item>
    /// <item><description>Sorts the results based on the specified criteria</description></item>
    /// <item><description>Limits the results to the maximum number of items if specified</description></item>
    /// <item><description>Records performance metrics and statistics</description></item>
    /// </list>
    /// </remarks>
    public async Task<ServiceResult<AggregatedDataResponse>> AggregateDataAsync(AggregationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var overallStopwatch = Stopwatch.StartNew();
            var response = new AggregatedDataResponse();

            // Filter services based on requested sources
            var servicesToQuery = _apiServices;
            if (request.Sources?.Any() == true)
            {
                servicesToQuery = _apiServices.Where(s => 
                    request.Sources.Contains(s.ServiceName, StringComparer.OrdinalIgnoreCase));
            }

            // Fetch data from all services in parallel
            var tasks = servicesToQuery.Select(service => FetchFromApiAsync(service, cancellationToken));
            var results = await Task.WhenAll(tasks);

            // Combine results from all APIs
            var allItems = new List<DataItem>();
            foreach (var result in results)
            {
                if (result.Success)
                {
                    allItems.AddRange(result.Items);
                    response.Metadata.SuccessfulApis.Add(result.ApiName);
                }
                else
                {
                    response.Metadata.FailedApis.Add(result.ApiName);
                    _logger.LogWarning("API {ApiName} failed: {Message}", 
                        result.ApiName, 
                        result.ErrorMessage);
                }
            }

            // Check if all APIs are required and any failed
            if (_options.RequireAllApis && response.Metadata.FailedApis.Any())
            {
                return new ServiceResult<AggregatedDataResponse>( 
                    ApiErrorCode.GenericError, 
                    $"One or more required APIs failed: {string.Join(", ", response.Metadata.FailedApis)}");
            }

            // Apply filters and sorting
            var filteredItems = ApplyFilters(allItems, request);
            var sortedItems = ApplySorting(filteredItems, request);

            // Limit results if MaxItems is specified
            if (request.MaxItems.HasValue && request.MaxItems > 0)
            {
                sortedItems = sortedItems.Take(request.MaxItems.Value).ToList();
            }

            response.Items = sortedItems;
            response.Metadata.TotalItems = sortedItems.Count;
            response.Metadata.AggregatedAt = DateTime.UtcNow;
            
            overallStopwatch.Stop();
            response.Metadata.TotalResponseTimeMs = overallStopwatch.ElapsedMilliseconds;

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during aggregation");
            return new ServiceResult<AggregatedDataResponse>(
                ApiErrorCode.GenericError,
                $"Error during aggregation: {ex.Message}", 
                ex);
        }
    }

    /// <summary>
    /// Fetches data from a single external API service with caching support
    /// </summary>
    /// <param name="service">The external API service to fetch data from</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>
    /// An <see cref="AggregatorResult"/> containing the fetched items and metadata
    /// </returns>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    /// <item><description>Checks the cache first to avoid unnecessary API calls</description></item>
    /// <item><description>Fetches data from the API if cache miss occurs</description></item>
    /// <item><description>Caches successful responses for future requests</description></item>
    /// <item><description>Records performance statistics for success and failure cases</description></item>
    /// <item><description>Handles exceptions gracefully and returns empty results on failure</description></item>
    /// </list>
    /// </remarks>
    private async Task<AggregatorResult> FetchFromApiAsync(IExternalApiService service, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = $"api_data_{service.ServiceName}";

        try
        {
            // Check cache first
            var cacheResult = _cacheService.Get<List<DataItem>>(cacheKey);
            if (cacheResult.Success && cacheResult.Value != null)
            {
                stopwatch.Stop();
                _statisticsService.RecordSuccess(service.ServiceName, stopwatch.ElapsedMilliseconds, fromCache: true);
                
                _logger.LogInformation("Cache hit for {ServiceName}", service.ServiceName);
                
                return new AggregatorResult
                {
                    ApiName = service.ServiceName,
                    Items = cacheResult.Value,
                    FromCache = true,
                    Success = true
                };
            }

            // Fetch from API
            _logger.LogInformation("Fetching data from {ServiceName}", service.ServiceName);
            
            var result = await service.FetchDataAsync(cancellationToken);
            
            stopwatch.Stop();

            if (result.Success && result.Value != null)
            {
                // Cache successful response
                _cacheService.Set(cacheKey, result.Value, _options.CacheDurationMinutes);
                
                _statisticsService.RecordSuccess(service.ServiceName, stopwatch.ElapsedMilliseconds, fromCache: false);

                return new AggregatorResult
                {
                    ApiName = service.ServiceName,
                    Items = result.Value,
                    FromCache = false,
                    Success = true
                };
            }
            else
            {
                _statisticsService.RecordFailure(service.ServiceName, stopwatch.ElapsedMilliseconds);
                
                var errorMessage = result.Error?.Message ?? "Unknown error";
                _logger.LogError("Error fetching data from {ServiceName}: {Error}", 
                    service.ServiceName, errorMessage);
                
                return new AggregatorResult
                {
                    ApiName = service.ServiceName,
                    Items = new List<DataItem>(),
                    FromCache = false,
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _statisticsService.RecordFailure(service.ServiceName, stopwatch.ElapsedMilliseconds);
            
            _logger.LogError(ex, "Unexpected error fetching data from {ServiceName}", service.ServiceName);
            
            return new AggregatorResult
            {
                ApiName = service.ServiceName,
                Items = new List<DataItem>(),
                FromCache = false,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Applies filtering logic to the aggregated data items
    /// </summary>
    /// <param name="items">The list of data items to filter</param>
    /// <param name="request">The aggregation request containing filter criteria</param>
    /// <returns>A filtered list of data items</returns>
    /// <remarks>
    /// Supports filtering by:
    /// <list type="bullet">
    /// <item><description>Category (case-insensitive)</description></item>
    /// <item><description>From date (items on or after this date)</description></item>
    /// <item><description>To date (items on or before this date)</description></item>
    /// </list>
    /// </remarks>
    private List<DataItem> ApplyFilters(List<DataItem> items, AggregationRequest request)
    {
        var filtered = items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            filtered = filtered.Where(i => 
                i.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (request.FromDate.HasValue)
        {
            filtered = filtered.Where(i => i.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            filtered = filtered.Where(i => i.Timestamp <= request.ToDate.Value);
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Applies sorting logic to the filtered data items
    /// </summary>
    /// <param name="items">The list of data items to sort</param>
    /// <param name="request">The aggregation request containing sort criteria</param>
    /// <returns>A sorted list of data items</returns>
    /// <remarks>
    /// Supports sorting by:
    /// <list type="bullet">
    /// <item><description>timestamp - Sort by the item's timestamp (default)</description></item>
    /// <item><description>relevance - Sort by the item's relevance score</description></item>
    /// <item><description>title - Sort alphabetically by the item's title</description></item>
    /// </list>
    /// Sort direction can be ascending or descending (default: descending for timestamp)
    /// </remarks>
    private List<DataItem> ApplySorting(List<DataItem> items, AggregationRequest request)
    {
        var sortBy = request.SortBy?.ToLowerInvariant() ?? "timestamp";
        var isDescending = request.SortDirection?.ToLowerInvariant() == "desc";

        IOrderedEnumerable<DataItem> sorted = sortBy switch
        {
            "timestamp" => isDescending 
                ? items.OrderByDescending(i => i.Timestamp)
                : items.OrderBy(i => i.Timestamp),
            "relevance" => isDescending 
                ? items.OrderByDescending(i => i.RelevanceScore)
                : items.OrderBy(i => i.RelevanceScore),
            "title" => isDescending 
                ? items.OrderByDescending(i => i.Title)
                : items.OrderBy(i => i.Title),
            _ => isDescending 
                ? items.OrderByDescending(i => i.Timestamp)
                : items.OrderBy(i => i.Timestamp)
        };

        return sorted.ToList();
    }

    /// <summary>
    /// Internal class that holds the result of a single API fetch operation
    /// </summary>
    private class AggregatorResult
    {
        /// <summary>
        /// Gets or sets the name of the API service
        /// </summary>
        public string ApiName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data items fetched from the API
        /// </summary>
        public List<DataItem> Items { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether the data was retrieved from cache
        /// </summary>
        public bool FromCache { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the fetch operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the fetch operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
