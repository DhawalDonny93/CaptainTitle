namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for caching data with Redis
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get cached value
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Set cached value with optional expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Remove cached value
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Remove all keys matching a pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Get or set cached value (cache-aside pattern)
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
}
