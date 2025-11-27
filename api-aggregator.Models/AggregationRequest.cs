namespace api_aggregator.Models;

/// <summary>
/// Request parameters for data aggregation
/// </summary>
public class AggregationRequest
{
    /// <summary>
    /// Filter by specific API sources (e.g., "Weather", "News")
    /// If empty, all APIs are queried
    /// </summary>
    public List<string>? Sources { get; set; }

    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter items from this date onwards
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter items up to this date
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Sort field (e.g., "timestamp", "relevance", "title")
    /// </summary>
    public string SortBy { get; set; } = "timestamp";

    /// <summary>
    /// Sort direction: "asc" or "desc"
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Maximum number of items to return
    /// </summary>
    public int? MaxItems { get; set; }
}
