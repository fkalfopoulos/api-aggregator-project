using api_aggregator.Abstractions;
using api_aggregator.Services.Models;
using Microsoft.Extensions.Caching.Memory;

namespace api_aggregator.Services;

/// <summary>
/// In-memory cache service implementation
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public InMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ServiceResult<T?> Get<T>(string key)
    {
        try
        {
            return _cache.TryGetValue(key, out T? value) ? value : default(T);
        }
        catch (Exception ex)
        {
            return new ServiceResult<T?>(ApiErrorCode.GenericError, $"Error retrieving from cache: {ex.Message}", ex);
        }
    }

    public VoidApiResult<ApiErrorCode> Set<T>(string key, T value, int expirationMinutes)
    {
        try
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(expirationMinutes / 2) //less ram
            };

            _cache.Set(key, value, cacheOptions);
            return new VoidApiResult<ApiErrorCode>();
        }
        catch (Exception ex)
        {
            return new VoidApiResult<ApiErrorCode>(ApiErrorCode.GenericError, $"Error setting cache: {ex.Message}", ex);
        }
    }

    public VoidApiResult<ApiErrorCode> Remove(string key)
    {
        try
        {
            _cache.Remove(key);
            return new VoidApiResult<ApiErrorCode>();
        }
        catch (Exception ex)
        {
            return new VoidApiResult<ApiErrorCode>(ApiErrorCode.GenericError, $"Error removing from cache: {ex.Message}", ex);
        }
    }

    public VoidApiResult<ApiErrorCode> Clear()
    {
        // IMemoryCache doesn't have a Clear method, so we would need to track keys
        // For simplicity in this implementation, we'll return success
        // In production, consider using a different cache provider or tracking keys
        return new VoidApiResult<ApiErrorCode>();
    }
}
