using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace api_aggregator.Services.ExternalApis;

/// <summary>
/// Users API service - uses JSONPlaceholder
/// </summary>
public class UsersApiService : IExternalApiService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiOptions _options;

    public string ServiceName => "Users";

    public UsersApiService(HttpClient httpClient, IOptions<ExternalApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ServiceResult<List<DataItem>>> FetchDataAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Users.Enabled)
        {
            return ApiErrorCode.GenericError;
        }

        try
        {
            // Using JSONPlaceholder - a free fake REST API
            var response = await _httpClient.GetAsync($"{_options.Users.BaseUrl}/users", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ServiceResult<List<DataItem>>(
                    ApiErrorCode.GenericError,
                    $"Users API returned status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var usersData = JsonSerializer.Deserialize<JsonElement>(content);

            var items = new List<DataItem>();

            int count = 0;
            foreach (var user in usersData.EnumerateArray())
            {
                if (count >= 3) break; // Limit to 3 users

                var item = new DataItem
                {
                    Source = ServiceName,
                    Id = user.GetProperty("id").GetInt32().ToString(),
                    Title = user.GetProperty("name").GetString() ?? string.Empty,
                    Description = $"User: {user.GetProperty("username").GetString()}",
                    Category = "User",
                    Timestamp = DateTime.UtcNow.AddMinutes(-count * 10), 
                    RelevanceScore = 75 - (count * 10),
                    AdditionalData = new Dictionary<string, string>
                    {
                        ["Email"] = user.GetProperty("email").GetString() ?? string.Empty,
                        ["Company"] = user.TryGetProperty("company", out var company) 
                            ? company.GetProperty("name").GetString() ?? string.Empty 
                            : string.Empty,
                        ["City"] = user.TryGetProperty("address", out var address) 
                            ? address.GetProperty("city").GetString() ?? string.Empty 
                            : string.Empty
                    }
                };

                items.Add(item);
                count++;
            }

            return items;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return new ServiceResult<List<DataItem>>(ApiErrorCode.GenericError,ex.Message,ex);
        }
        catch (HttpRequestException ex)
        {
            return new ServiceResult<List<DataItem>>( ApiErrorCode.GenericError, $"Unexpected error in Users API: {ex.Message}",ex);
        }
    }
}
