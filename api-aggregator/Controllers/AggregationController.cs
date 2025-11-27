using Microsoft.AspNetCore.Mvc;
using api_aggregator.Models;
using api_aggregator.Services;

namespace api_aggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AggregationController : ControllerBase
{
    private readonly IAggregationService _aggregationService;
    private readonly ILogger<AggregationController> _logger;

    public AggregationController(
        IAggregationService aggregationService,
        ILogger<AggregationController> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves aggregated data from multiple external APIs
    /// </summary>
    /// <param name="fromDate">Filter items from this date (optional)</param>
    /// <param name="toDate">Filter items to this date (optional)</param>
    /// <param name="category">Filter by category (optional)</param>
    /// <param name="sortBy">Sort by field: 'date' or 'relevance' (default: date)</param>
    /// <param name="sortOrder">Sort order: 'asc' or 'desc' (default: desc)</param>
    /// <param name="maxResults">Maximum results per source (default: 100)</param>
    /// <returns>Aggregated data from all APIs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AggregatedDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AggregatedDataResponse>> GetAggregatedData(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? category = null,
        [FromQuery] string sortBy = "date",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filterOptions = new FilterOptions
            {
                FromDate = fromDate,
                ToDate = toDate,
                Category = category,
                SortBy = sortBy,
                SortOrder = sortOrder,
                MaxResults = maxResults
            };

            var result = await _aggregationService.AggregateDataAsync(filterOptions, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving aggregated data");
            return StatusCode(500, new { error = "An error occurred while aggregating data" });
        }
    }

    /// <summary>
    /// Retrieves aggregated data with POST for complex filter options
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AggregatedDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AggregatedDataResponse>> GetAggregatedDataPost(
        [FromBody] FilterOptions filterOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _aggregationService.AggregateDataAsync(filterOptions, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving aggregated data");
            return StatusCode(500, new { error = "An error occurred while aggregating data" });
        }
    }
}
