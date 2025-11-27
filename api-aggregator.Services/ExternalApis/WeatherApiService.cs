using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace api_aggregator.Services.ExternalApis;

/// <summary>
/// Weather API service - uses OpenWeatherMap-like structure
/// </summary>
public class WeatherApiService : IExternalApiService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiOptions _options;

    public string ServiceName => "Weather";

    public WeatherApiService(HttpClient httpClient, IOptions<ExternalApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ServiceResult<List<DataItem>>> FetchDataAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Weather.Enabled)
        {
            return ApiErrorCode.GenericError;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{_options.Weather.BaseUrl}/data/2.5/weather?q=London&appid={_options.Weather.ApiKey}", 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ServiceResult<List<DataItem>>(
                    ApiErrorCode.GenericError,
                    $"Weather API returned status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var weatherData = JsonSerializer.Deserialize<JsonElement>(content);

            var items = new List<DataItem>();

            if (weatherData.TryGetProperty("weather", out var weatherArray) && weatherArray.GetArrayLength() > 0)
            {
                var weather = weatherArray[0];
                var main = weatherData.GetProperty("main");
                
                var item = new DataItem
                {
                    Source = ServiceName,
                    Id = weatherData.GetProperty("id").GetInt32().ToString(),
                    Title = $"Weather in {weatherData.GetProperty("name").GetString()}",
                    Description = weather.GetProperty("description").GetString() ?? string.Empty,
                    Category = "Weather",
                    Timestamp = DateTime.UtcNow,
                    RelevanceScore = 85,
                    AdditionalData = new Dictionary<string, string>
                    {
                        ["Temperature"] = main.GetProperty("temp").GetDouble().ToString("F1"),
                        ["Humidity"] = main.GetProperty("humidity").GetInt32().ToString(),
                        ["Condition"] = weather.GetProperty("main").GetString() ?? string.Empty
                    }
                };

                items.Add(item);
            }

            return items;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return new ServiceResult<List<DataItem>>(ApiErrorCode.GenericError, ex.Message, ex);
        }
        catch (HttpRequestException ex)
        {
            return new ServiceResult<List<DataItem>>(ApiErrorCode.GenericError, $"Unexpected error in Weather API: {ex.Message}",
                ex);
        }
    }
}
