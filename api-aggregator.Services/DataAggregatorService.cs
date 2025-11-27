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
public class DataAggregatorService : IDataAggregatorService
{
    private readonly IEnumerable<IExternalApiService> _apiServices;
    private readonly ICacheService _cacheService;
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<DataAggregatorService> _logger;
    private readonly AggregationOptions _options;

    public DataAggregatorService(IEnumerable<IExternalApiService> apiServices,ICacheService cacheService,IStatisticsService statisticsService, ILogger<DataAggregatorService> logger,
        IOptions<AggregationOptions> options)
    {
        _apiServices = apiServices;
        _cacheService = cacheService;
        _statisticsService = statisticsService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ServiceResult<AggregatedDataResponse>> AggregateDataAsync(AggregationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var overallStopwatch = Stopwatch.StartNew();
            var response = new AggregatedDataResponse();

            var servicesToQuery = _apiServices;
            if (request.Sources?.Any() == true)
            {
                servicesToQuery = _apiServices.Where(s => 
                    request.Sources.Contains(s.ServiceName, StringComparer.OrdinalIgnoreCase));
            }

            var tasks = servicesToQuery.Select(service => FetchFromApiAsync(service, cancellationToken));
            var results = await Task.WhenAll(tasks);

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

            if (_options.RequireAllApis && response.Metadata.FailedApis.Any())
            {
                return new ServiceResult<AggregatedDataResponse>( ApiErrorCode.GenericError, $"One or more required APIs failed: {string.Join(", ", response.Metadata.FailedApis)}");
            }

            var filteredItems = ApplyFilters(allItems, request);
            var sortedItems = ApplySorting(filteredItems, request);

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
            return new ServiceResult<AggregatedDataResponse>(ApiErrorCode.GenericError,$"Error during aggregation: {ex.Message}", ex);
        }
    }

    private async Task<AggregatorResult> FetchFromApiAsync(IExternalApiService service, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = $"api_data_{service.ServiceName}";

        try
        {
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

            _logger.LogInformation("Fetching data from {ServiceName}", service.ServiceName);
            
            var result = await service.FetchDataAsync(cancellationToken);
            
            stopwatch.Stop();

            if (result.Success && result.Value != null)
            {
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

    private class AggregatorResult
    {
        public string ApiName { get; set; } = string.Empty;
        public List<DataItem> Items { get; set; } = new();
        public bool FromCache { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
