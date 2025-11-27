using api_aggregator.Services.Models;

namespace api_aggregator.Abstractions;

/// <summary>
/// Generic cache service for storing and retrieving data
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a cached value
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or default if not found</returns>
    ServiceResult<T?> Get<T>(string key);

    /// <summary>
    /// Set a cached value with expiration
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expirationMinutes">Expiration time in minutes</param>
    VoidApiResult<ApiErrorCode> Set<T>(string key, T value, int expirationMinutes);

    /// <summary>
    /// Remove a cached value
    /// </summary>
    /// <param name="key">Cache key</param>
    VoidApiResult<ApiErrorCode> Remove(string key);

    /// <summary>
    /// Clear all cached values
    /// </summary>
    VoidApiResult<ApiErrorCode> Clear();
}
