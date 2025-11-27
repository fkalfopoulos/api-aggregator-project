# API Aggregator Service

A high-performance .NET 9 API aggregation service built with ASP.NET Core that fetches, combines, and processes data from multiple external APIs simultaneously with intelligent caching, resilience patterns, and comprehensive performance monitoring.

## ?? Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Architecture](#architecture)
- [Why This Architecture?](#why-this-architecture)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Adding New APIs](#adding-new-apis)
- [Result Pattern](#result-pattern)
- [Testing](#testing)
- [Performance Monitoring](#performance-monitoring)

## ?? Overview

The API Aggregator Service is designed to solve a common problem: efficiently fetching and combining data from multiple external APIs while handling failures gracefully, providing caching for performance, and monitoring the health of each data source.

**What it does:**
- Fetches data from multiple external APIs **in parallel** (not sequentially)
- Aggregates results into a unified response format
- Filters and sorts data based on user preferences
- Caches responses to minimize redundant API calls
- Tracks detailed performance statistics for each API
- Handles partial failures gracefully (returns data from successful APIs)
- Implements resilience patterns (retries, circuit breakers, timeouts)

## ? Key Features

### 1. **Parallel API Execution**
- All configured APIs are called simultaneously using `Task.WhenAll()`
- Significantly reduces response time compared to sequential calls
- Example: 3 APIs taking 500ms each = ~500ms total (not 1500ms)

### 2. **Intelligent Caching**
- In-memory caching with configurable TTL (Time To Live)
- Sliding expiration for frequently accessed data
- Cache hit/miss tracking per API
- Reduces load on external APIs and improves response times

### 3. **Resilience & Fault Tolerance**
- **Polly Integration**: Automatic retries with exponential backoff
- **Circuit Breaker**: Prevents cascading failures
- **Timeouts**: Prevents hanging requests
- **Graceful Degradation**: Returns partial results if some APIs fail
- **Configurable Behavior**: Choose between "require all" or "partial results" mode

### 4. **Performance Monitoring**
- Real-time statistics per API
- Performance bucket classification:
  - **Fast**: < 200ms
  - **Average**: 200-500ms
  - **Slow**: > 500ms
- Success/failure rate tracking
- Average response time calculation
- Cache efficiency metrics

### 5. **Flexible Data Operations**
- **Filtering**: By category, date range, source
- **Sorting**: By timestamp, relevance, title (ascending/descending)
- **Limiting**: Maximum items per response
- **Source Selection**: Query specific APIs only

## ??? Architecture

The solution follows **Clean Architecture** principles with clear separation of concerns:

```
???????????????????????????????????????????????????????????????
?                     api-aggregator.WebAPI                   ?
?  Controllers ? Middleware ? DI Configuration ? Endpoints   ?
???????????????????????????????????????????????????????????????
                         ?
                         ?
???????????????????????????????????????????????????????????????
?                   api-aggregator.Services                   ?
?  DataAggregatorService ? ExternalApiServices ? Cache        ?
?  StatisticsService ? Polly Policies ? Business Logic       ?
???????????????????????????????????????????????????????????????
                         ?
          ???????????????????????????????
          ?                             ?
???????????????????????    ????????????????????????????????
?  api-aggregator.    ?    ?   api-aggregator.Models      ?
?    Abstractions     ?    ?   DTOs ? Entities ? Results  ?
?  Interfaces Only    ?    ?   Configuration ? Errors     ?
???????????????????????    ????????????????????????????????
          ?
          ?
          ???????????????????
                            ?
                  ????????????????????????
                  ?  api-aggregator.     ?
                  ?    UnitTests         ?
                  ?  xUnit ? Moq ?       ?
                  ?  FluentAssertions    ?
                  ????????????????????????
```

### Project Structure

| Project | Purpose | Dependencies |
|---------|---------|-------------|
| **WebAPI** | API endpoints, controllers, configuration | Services, Models, Abstractions |
| **Services** | Business logic, external API integrations | Abstractions, Models |
| **Models** | DTOs, domain models, result types | None |
| **Abstractions** | Interfaces and contracts | Models |
| **UnitTests** | Comprehensive test coverage | Services, Models, Abstractions |

## ?? Why This Architecture?

### Easy API Integration

Adding a new external API requires minimal code changes in only 3 places:

#### **1. Create the API Service Implementation** (5-10 minutes)
```csharp
public class NewApiService : IExternalApiService
{
    public string ServiceName => "NewApi";
    
    public async Task<ServiceResult<List<DataItem>>> FetchDataAsync(...)
    {
        // Your API integration logic
        return items; // Implicit conversion to ServiceResult
    }
}
```

#### **2. Register in DI Container** (1 minute)
```csharp
// In DependencyInjectionExtensions.cs
services.AddHttpClient<IExternalApiService, NewApiService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

#### **3. Add Configuration** (1 minute)
```json
// In appsettings.json
"ExternalApis": {
  "NewApi": {
    "BaseUrl": "https://api.example.com",
    "ApiKey": "your_key",
    "Enabled": true
  }
}
```

**That's it!** The aggregator automatically:
- ? Includes your API in parallel execution
- ? Applies caching to your API responses
- ? Tracks statistics for your API
- ? Handles failures with resilience patterns
- ? Allows filtering/sorting of your data

### Benefits of the Result Pattern

The custom `ServiceResult<TValue, TErrorCode>` pattern provides:

? **No Exceptions for Business Logic**: Errors are values, not control flow  
? **Explicit Error Handling**: Compiler forces you to handle errors  
? **Type-Safe Error Codes**: Enumeration-based error classification  
? **Implicit Conversions**: Clean success cases: `return items;`  
? **Exception Tracking**: Still captures unexpected exceptions  
? **Better Performance**: Avoids expensive exception throwing  

```csharp
// Success case - clean and readable
return items; // Implicitly converts to ServiceResult<List<DataItem>>

// Error case - explicit and type-safe
return ServiceErrorCode.NotFoundError; // Implicit conversion

// Detailed error
return new ServiceResult<List<DataItem>>(
    ServiceErrorCode.GenericError, 
    "API returned 404", 
    exception);

// Consuming the result
var result = await service.FetchDataAsync();
if (result.Success)
{
    ProcessData(result.Value);
}
else
{
    LogError(result.Error.Code, result.Error.Message);
}
```

## ?? Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Your favorite IDE (Visual Studio 2022, VS Code, Rider)
- API keys for external services (optional - see configuration)

### Installation

1. **Clone the repository**
```bash
git clone https://github.com/yourusername/api-aggregator.git
cd api-aggregator
```

2. **Restore dependencies**
```bash
dotnet restore
```

3. **Configure API keys** (see [Configuration](#configuration))

4. **Build the solution**
```bash
dotnet build
```

5. **Run the application**
```bash
cd api-aggregator.WebAPI
dotnet run
```

6. **Access the API**
- Swagger UI: `https://localhost:5001/openapi/v1.json`
- Base URL: `https://localhost:5001`

## ?? Configuration

### appsettings.json

```json
{
  "Aggregation": {
    "RequireAllApis": false,        // true = fail if any API fails
    "ApiTimeoutSeconds": 10,        // Timeout per API call
    "MaxRetryAttempts": 3,          // Number of retry attempts
    "CacheDurationMinutes": 5       // Cache TTL
  },
  "ExternalApis": {
    "Weather": {
      "BaseUrl": "https://api.openweathermap.org",
      "ApiKey": "your_openweathermap_key",
      "Enabled": true               // Toggle APIs on/off
    },
    "News": {
      "BaseUrl": "https://newsapi.org",
      "ApiKey": "your_newsapi_key",
      "Enabled": true
    },
    "Users": {
      "BaseUrl": "https://jsonplaceholder.typicode.com",
      "ApiKey": null,               // Some APIs don't need keys
      "Enabled": true
    }
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `RequireAllApis` | If `true`, request fails if any API fails. If `false`, returns partial results | `false` |
| `ApiTimeoutSeconds` | Timeout for individual API calls | `10` |
| `MaxRetryAttempts` | Number of retry attempts with exponential backoff | `3` |
| `CacheDurationMinutes` | How long to cache API responses | `5` |

### Getting API Keys

- **OpenWeatherMap**: [https://openweathermap.org/api](https://openweathermap.org/api) (Free tier available)
- **NewsAPI**: [https://newsapi.org/](https://newsapi.org/) (Free tier: 100 requests/day)
- **JSONPlaceholder**: No API key needed (free fake REST API)

## ?? API Endpoints

### 1. Aggregate Data (POST)

**Endpoint**: `POST /api/aggregation/aggregate`

**Request Body**:
```json
{
  "sources": ["Weather", "News"],    // Optional: specific APIs
  "category": "News",                // Optional: filter by category
  "fromDate": "2024-01-01",         // Optional: date range start
  "toDate": "2024-12-31",           // Optional: date range end
  "sortBy": "relevance",            // timestamp, relevance, title
  "sortDirection": "desc",          // asc or desc
  "maxItems": 10                    // Optional: limit results
}
```

**Response**:
```json
{
  "items": [
    {
      "source": "News",
      "id": "123",
      "title": "Breaking News",
      "description": "Details...",
      "category": "News",
      "timestamp": "2024-01-15T10:30:00Z",
      "relevanceScore": 95,
      "additionalData": {
        "author": "John Doe",
        "url": "https://..."
      }
    }
  ],
  "metadata": {
    "totalItems": 10,
    "successfulApis": ["News", "Weather"],
    "failedApis": [],
    "aggregatedAt": "2024-01-15T10:30:00Z",
    "fromCache": false,
    "totalResponseTimeMs": 345
  }
}
```

### 2. Aggregate Data (GET)

**Endpoint**: `GET /api/aggregation`

**Query Parameters**:
- `sources` (string): Comma-separated list of API names
- `category` (string): Filter by category
- `sortBy` (string): Sort field (timestamp, relevance, title)
- `sortDirection` (string): asc or desc
- `maxItems` (int): Maximum number of items

**Example**:
```
GET /api/aggregation?sources=Weather,News&sortBy=relevance&maxItems=5
```

### 3. Get All Statistics

**Endpoint**: `GET /api/statistics`

**Response**:
```json
{
  "apiStats": [
    {
      "apiName": "Weather",
      "totalRequests": 150,
      "successfulRequests": 148,
      "failedRequests": 2,
      "averageResponseTimeMs": 234.56,
      "buckets": {
        "fast": 120,      // < 200ms
        "average": 25,    // 200-500ms
        "slow": 5         // > 500ms
      },
      "cacheHitRate": 65.5
    }
  ],
  "overall": {
    "totalRequests": 450,
    "averageResponseTimeMs": 289.33,
    "successRate": 97.8
  }
}
```

### 4. Get Statistics for Specific API

**Endpoint**: `GET /api/statistics/{apiName}`

**Example**: `GET /api/statistics/Weather`

### 5. Reset Statistics

**Endpoint**: `DELETE /api/statistics`

## ?? Adding New APIs

### Step-by-Step Guide

#### 1. Create the Service Class

Create a new file in `api-aggregator.Services/ExternalApis/`:

```csharp
using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace api_aggregator.Services.ExternalApis;

public class GitHubApiService : IExternalApiService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiOptions _options;

    public string ServiceName => "GitHub";

    public GitHubApiService(HttpClient httpClient, IOptions<ExternalApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ServiceResult<List<DataItem>>> FetchDataAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_options.GitHub.Enabled)
        {
            return ServiceErrorCode.GenericError;
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.GitHub.BaseUrl}/repos/microsoft/dotnet/events",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ServiceResult<List<DataItem>>(
                    ServiceErrorCode.GenericError,
                    $"GitHub API returned {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var events = JsonSerializer.Deserialize<JsonElement>(content);

            var items = new List<DataItem>();
            
            foreach (var evt in events.EnumerateArray().Take(5))
            {
                items.Add(new DataItem
                {
                    Source = ServiceName,
                    Id = evt.GetProperty("id").GetString() ?? "",
                    Title = evt.GetProperty("type").GetString() ?? "",
                    Description = $"Actor: {evt.GetProperty("actor").GetProperty("login").GetString()}",
                    Category = "GitHub",
                    Timestamp = evt.GetProperty("created_at").GetDateTime(),
                    RelevanceScore = 80
                });
            }

            return items; // Implicit conversion to ServiceResult
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return new ServiceResult<List<DataItem>>(
                ServiceErrorCode.GenericError,
                "Request timeout",
                ex);
        }
        catch (Exception ex)
        {
            return new ServiceResult<List<DataItem>>(
                ServiceErrorCode.GenericError,
                ex.Message,
                ex);
        }
    }
}
```

#### 2. Register in Dependency Injection

In `api-aggregator.Services/DependencyInjectionExtensions.cs`:

```csharp
services.AddHttpClient<IExternalApiService, GitHubApiService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());
```

#### 3. Add Configuration Model

In `api-aggregator.Models/Configuration.cs`, add to `ExternalApiOptions`:

```csharp
public class ExternalApiOptions
{
    // ... existing properties ...
    
    public ApiEndpointConfig GitHub { get; set; } = new();
}
```

#### 4. Add to Configuration File

In `api-aggregator.WebAPI/appsettings.json`:

```json
{
  "ExternalApis": {
    "GitHub": {
      "BaseUrl": "https://api.github.com",
      "ApiKey": null,
      "Enabled": true
    }
  }
}
```

#### 5. Test Your New API

```bash
# Request data from only your new API
curl -X GET "https://localhost:5001/api/aggregation?sources=GitHub"

# Check statistics
curl -X GET "https://localhost:5001/api/statistics/GitHub"
```

## ?? Result Pattern

### ServiceResult<TValue, TErrorCode>

The custom result pattern provides type-safe error handling without exceptions.

**Key Components**:
- `ServiceResult<TValue>`: For operations that return data
- `VoidResultServiceResult<TErrorCode>`: For operations with no return value
- `ServiceErrorCode`: Enumeration of error types
- `ApiErrorCode`: Enumeration for API-specific errors

**Usage Examples**:

```csharp
// Success with implicit conversion
public ServiceResult<User> GetUser(int id)
{
    var user = _repository.GetById(id);
    return user; // Implicitly converts to ServiceResult<User>
}

// Error with error code
public ServiceResult<User> GetUser(int id)
{
    if (id <= 0)
        return ServiceErrorCode.ValidationError;
    // ...
}

// Error with details
public ServiceResult<Data> FetchData()
{
    try
    {
        // ...
    }
    catch (Exception ex)
    {
        return new ServiceResult<Data>(
            ServiceErrorCode.GenericError,
            "Failed to fetch data",
            ex);
    }
}

// Consuming results
var result = service.GetUser(123);
if (result.Success)
{
    Console.WriteLine($"User: {result.Value.Name}");
}
else
{
    Console.WriteLine($"Error: {result.Error.Code} - {result.Error.Message}");
}
```

## ?? Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~DataAggregatorServiceTests"
```

### Test Coverage

The solution includes comprehensive unit tests:

- **DataAggregatorServiceTests**: Aggregation logic, filtering, sorting
- **StatisticsServiceTests**: Performance tracking, bucket classification
- **InMemoryCacheServiceTests**: Caching behavior
- **ServiceResultTests**: Result pattern validation

### Writing Tests for New APIs

```csharp
[Fact]
public async Task FetchDataAsync_ShouldReturn_ValidData()
{
    // Arrange
    var mockHttp = new Mock<HttpClient>();
    var service = new YourApiService(mockHttp.Object, options);

    // Act
    var result = await service.FetchDataAsync();

    // Assert
    result.Success.Should().BeTrue();
    result.Value.Should().NotBeEmpty();
}
```

## ?? Performance Monitoring

### Understanding Performance Buckets

The statistics endpoint categorizes API response times into buckets:

| Bucket | Response Time | Interpretation |
|--------|--------------|----------------|
| **Fast** | < 200ms | Excellent performance |
| **Average** | 200-500ms | Acceptable performance |
| **Slow** | > 500ms | May need investigation |

### Monitoring Tips

1. **Check cache hit rate**: High rate (>70%) indicates good caching
2. **Watch slow requests**: Consistently slow? Check API health or increase timeout
3. **Monitor failure rate**: High failures? Check API keys, rate limits, or connectivity
4. **Track trends**: Compare statistics over time to detect degradation

### Example Monitoring Query

```bash
# Get current statistics
curl https://localhost:5001/api/statistics | jq '.apiStats[] | select(.buckets.slow > 10)'
```

## ?? Security Considerations

- **API Keys**: Store in User Secrets (development) or Azure Key Vault (production)
- **Rate Limiting**: Implement if exposing publicly
- **Authentication**: Add JWT authentication (optional feature - see requirements)
- **HTTPS**: Always use in production
- **CORS**: Configure appropriately for your use case

## ?? Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "api-aggregator.WebAPI.dll"]
```

### Environment Variables

```bash
export Aggregation__RequireAllApis=false
export ExternalApis__Weather__ApiKey="your_key"
export ExternalApis__News__ApiKey="your_key"
```

## ?? License

This project is licensed under the MIT License.

## ?? Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## ?? Support

For issues and questions, please open an issue on GitHub.

---

**Built with ?? using .NET 9 and ASP.NET Core**
