using Microsoft.AspNetCore.Mvc;
using api_aggregator.Models;
using api_aggregator.Services;

namespace api_aggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IStatisticsService statisticsService,
        ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves request statistics for all APIs including performance buckets
    /// </summary>
    /// <returns>Statistics showing total requests, average response times, and performance buckets</returns>
    [HttpGet]
    [ProducesResponseType(typeof(StatisticsResponse), StatusCodes.Status200OK)]
    public ActionResult<StatisticsResponse> GetStatistics()
    {
        try
        {
            var statistics = _statisticsService.GetStatistics();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics");
            return StatusCode(500, new { error = "An error occurred while retrieving statistics" });
        }
    }

    /// <summary>
    /// Resets all statistics (useful for testing)
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ResetStatistics()
    {
        try
        {
            _statisticsService.Reset();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting statistics");
            return StatusCode(500, new { error = "An error occurred while resetting statistics" });
        }
    }
}
