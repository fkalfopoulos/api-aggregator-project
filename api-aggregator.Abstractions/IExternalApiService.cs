using api_aggregator.Models;
using api_aggregator.Services.Models;

namespace api_aggregator.Abstractions;

/// <summary>
/// Base interface for external API services
/// </summary>
public interface IExternalApiService
{
    /// <summary>
    /// Name of the API service
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Fetch data from the external API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of data items or an API error</returns>
    Task<ServiceResult<List<DataItem>>> FetchDataAsync(CancellationToken cancellationToken = default);
}
