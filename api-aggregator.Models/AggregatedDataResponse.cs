namespace api_aggregator.Models;

/// <summary>
/// Response containing aggregated data from multiple APIs
/// </summary>
public class AggregatedDataResponse
{
    /// <summary>
    /// Collection of data items from all APIs
    /// </summary>
    public List<DataItem> Items { get; set; } = new();

    /// <summary>
    /// Metadata about the aggregation request
    /// </summary>
    public AggregationMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Individual data item from an external API
/// </summary>
public class DataItem
{
    /// <summary>
    /// Source API name (e.g., "Weather", "News", "Users")
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for the item
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Title or main text content
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description or additional content
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category or type of the item
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the item
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Relevance score (0-100)
    /// </summary>
    public int RelevanceScore { get; set; }

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

/// <summary>
/// Metadata about the aggregation operation
/// </summary>
public class AggregationMetadata
{
    /// <summary>
    /// Total number of items returned
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// APIs that successfully responded
    /// </summary>
    public List<string> SuccessfulApis { get; set; } = new();

    /// <summary>
    /// APIs that failed to respond
    /// </summary>
    public List<string> FailedApis { get; set; } = new();

    /// <summary>
    /// Timestamp when the aggregation was performed
    /// </summary>
    public DateTime AggregatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether data was served from cache
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// Total response time in milliseconds
    /// </summary>
    public long TotalResponseTimeMs { get; set; }
}
