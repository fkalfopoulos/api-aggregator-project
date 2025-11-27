using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace api_aggregator.Services.ExternalApis;

/// <summary>
/// News API service - uses NewsAPI-like structure
/// </summary>
public class NewsApiService : IExternalApiService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiOptions _options;

    public string ServiceName => "News";

    public NewsApiService(HttpClient httpClient, IOptions<ExternalApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ServiceResult<List<DataItem>>> FetchDataAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.News.Enabled)
        {
            return ApiErrorCode.GenericError;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{_options.News.BaseUrl}/v2/top-headlines?country=us&apiKey={_options.News.ApiKey}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ServiceResult<List<DataItem>>(
                    ApiErrorCode.GenericError,
                    $"News API returned status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var newsData = JsonSerializer.Deserialize<JsonElement>(content);

            var items = new List<DataItem>();

            if (newsData.TryGetProperty("articles", out var articles))
            {
                int count = 0;
                foreach (var article in articles.EnumerateArray())
                {
                    if (count >= 5) break; 

                    var publishedAt = article.TryGetProperty("publishedAt", out var pubDate)
                        ? DateTime.Parse(pubDate.GetString() ?? DateTime.UtcNow.ToString())
                        : DateTime.UtcNow;

                    var item = new DataItem
                    {
                        Source = ServiceName,
                        Id = Guid.NewGuid().ToString(),
                        Title = article.GetProperty("title").GetString() ?? string.Empty,
                        Description = article.GetProperty("description").GetString() ?? string.Empty,
                        Category = "News",
                        Timestamp = publishedAt,
                        RelevanceScore = 90 - (count * 5), // Decreasing relevance
                        AdditionalData = new Dictionary<string, string>
                        {
                            ["Author"] = article.TryGetProperty("author", out var author) ? author.GetString() ?? "Unknown" : "Unknown",
                            ["Url"] = article.TryGetProperty("url", out var url) ? url.GetString() ?? string.Empty : string.Empty
                        }
                    };

                    items.Add(item);
                    count++;
                }
            }

            return items;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return new ServiceResult<List<DataItem>>( ApiErrorCode.GenericError, ex.Message,ex);
        }
        catch (HttpRequestException ex)
        {
            return new ServiceResult<List<DataItem>>(ApiErrorCode.GenericError,$"Unexpected error in News API: {ex.Message}", ex);
        }
    }
}
