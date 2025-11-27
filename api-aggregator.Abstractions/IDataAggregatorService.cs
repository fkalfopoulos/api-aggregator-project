using api_aggregator.Models;
using api_aggregator.Services.Models;

namespace api_aggregator.Abstractions;

/// <summary>
/// Service for aggregating data from multiple external APIs
/// </summary>
public interface IDataAggregatorService
{
    /// <summary>
    /// Aggregate data from multiple APIs based on the request parameters
    /// </summary>
    /// <param name="request">Aggregation request with filters and sorting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated data response</returns>
    Task<ServiceResult<AggregatedDataResponse>> AggregateDataAsync(AggregationRequest request, CancellationToken cancellationToken = default);
}
