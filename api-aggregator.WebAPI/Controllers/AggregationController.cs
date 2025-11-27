using api_aggregator.Abstractions;
using api_aggregator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_aggregator.WebAPI.Controllers;

/// <summary>
/// Controller for aggregating data from multiple external APIs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Require authentication for all endpoints
public class AggregationController : ControllerBase
{
    private readonly IDataAggregatorService _aggregatorService;
    private readonly ILogger<AggregationController> _logger;

    public AggregationController(
        IDataAggregatorService aggregatorService,
        ILogger<AggregationController> logger)
    {
        _aggregatorService = aggregatorService;
        _logger = logger;
    }

    /// <summary>
    /// Aggregate data from multiple external APIs
    /// </summary>
    /// <param name="request">Aggregation request with optional filters and sorting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated data from all available APIs</returns>
    /// <response code="200">Returns the aggregated data</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If one or more required APIs fail</response>
    [HttpPost("aggregate")]
    [ProducesResponseType(typeof(AggregatedDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AggregatedDataResponse>> AggregateData(
        [FromBody] AggregationRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Aggregation request received with sources: {Sources}", 
            request.Sources != null ? string.Join(", ", request.Sources) : "all");

        var result = await _aggregatorService.AggregateDataAsync(request, cancellationToken);

        if (result.Failed)
        {
            _logger.LogError("Aggregation failed: {Error}", result.Error?.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Aggregation failed",
                message = result.Error?.Message ?? "Unknown error",
                code = result.Error?.Code
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get aggregated data using query parameters (simpler GET endpoint)
    /// </summary>
    /// <param name="sources">Comma-separated list of sources to query</param>
    /// <param name="category">Filter by category</param>
    /// <param name="sortBy">Sort field (timestamp, relevance, title)</param>
    /// <param name="sortDirection">Sort direction (asc, desc)</param>
    /// <param name="maxItems">Maximum number of items to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated data from selected APIs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AggregatedDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AggregatedDataResponse>> GetAggregatedData(
        [FromQuery] string? sources = null,
        [FromQuery] string? category = null,
        [FromQuery] string sortBy = "timestamp",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] int? maxItems = null,
        CancellationToken cancellationToken = default)
    {
        var request = new AggregationRequest
        {
            Sources = !string.IsNullOrWhiteSpace(sources) 
                ? sources.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() 
                : null,
            Category = category,
            SortBy = sortBy,
            SortDirection = sortDirection,
            MaxItems = maxItems
        };

        return await AggregateData(request, cancellationToken);
    }
}
