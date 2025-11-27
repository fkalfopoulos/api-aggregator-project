using api_aggregator.Abstractions;
using api_aggregator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_aggregator.WebAPI.Controllers;

/// <summary>
/// Controller for retrieving API performance statistics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Require authentication for all endpoints
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController( IStatisticsService statisticsService,ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get performance statistics for all APIs
    /// </summary>
    /// <returns>Statistics including request counts and performance buckets</returns>
    /// <response code="200">Returns statistics for all APIs</response>
    [HttpGet]
    [ProducesResponseType(typeof(StatisticsResponse), StatusCodes.Status200OK)]
    public ActionResult<StatisticsResponse> GetAllStatistics()
    {
        _logger.LogInformation("Statistics request received");

        var result = _statisticsService.GetAllStatistics();

        if (result.Failed)
        {
            _logger.LogError("Failed to get statistics: {Error}", result.Error?.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to retrieve statistics",
                message = result.Error?.Message ?? "Unknown error"
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get performance statistics for a specific API
    /// </summary>
    /// <param name="apiName">Name of the API (e.g., "Weather", "News", "Users")</param>
    /// <returns>Statistics for the specified API</returns>
    /// <response code="200">Returns statistics for the specified API</response>
    /// <response code="404">If no statistics exist for the specified API</response>
    [HttpGet("{apiName}")]
    [ProducesResponseType(typeof(ApiStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApiStatistics> GetStatistics(string apiName)
    {
        _logger.LogInformation("Statistics request received for API: {ApiName}", apiName);

        var result = _statisticsService.GetStatistics(apiName);

        if (result.Failed)
        {
            _logger.LogError("Failed to get statistics for {ApiName}: {Error}", apiName, result.Error?.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to retrieve statistics",
                message = result.Error?.Message ?? "Unknown error"
            });
        }

        if (result.Value == null)
        {
            return NotFound(new { error = $"No statistics found for API: {apiName}" });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Reset all statistics (useful for testing)
    /// </summary>
    /// <returns>Success message</returns>
    /// <response code="200">Statistics reset successfully</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult ResetStatistics()
    {
        _logger.LogInformation("Resetting all statistics");

        var result = _statisticsService.ResetStatistics();

        if (result.Failed)
        {
            _logger.LogError("Failed to reset statistics: {Error}", result.Error?.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to reset statistics",
                message = result.Error?.Message ?? "Unknown error"
            });
        }

        return Ok(new { message = "Statistics reset successfully" });
    }
}
