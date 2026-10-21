using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.Interfaces;

namespace AppBackendBasic.Services.Authentication;

public class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(IMemoryCache cache, ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(string key, int maxAttempts, TimeSpan timeWindow)
    {
        var rateLimitKey = $"rate_limit:{key}";
        _cache.Remove(rateLimitKey); // TODO: REMOVE THIS LINE IN PRODUCTION - Currently clears cache on every call for testing
        // This bypasses rate limiting - delete before deploying to production to enable actual rate limiting enforcement
        if (!_cache.TryGetValue(rateLimitKey, out RateLimitData? data))
        {
            // First attempt
            data = new RateLimitData
            {
                AttemptCount = 1,
                WindowStart = DateTime.UtcNow
            };
            
            _cache.Set(rateLimitKey, data, timeWindow);
            _logger.LogDebug("Rate limit initialized for key: {Key}", key);
            return true;
        }

        // Check if window has expired
        if (DateTime.UtcNow - data.WindowStart > timeWindow)
        {
            // Reset window
            data.AttemptCount = 1;
            data.WindowStart = DateTime.UtcNow;
            _cache.Set(rateLimitKey, data, timeWindow);
            _logger.LogDebug("Rate limit window reset for key: {Key}", key);
            return true;
        }

        // Increment attempt count
        data.AttemptCount++;
        _cache.Set(rateLimitKey, data, timeWindow);

        var isAllowed = data.AttemptCount <= maxAttempts;
        
        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for key: {Key}. Attempts: {Attempts}/{Max}", 
                key, data.AttemptCount, maxAttempts);
        }

        return isAllowed;
    }

    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string key)
    {
        var rateLimitKey = $"rate_limit:{key}";
        
        if (!_cache.TryGetValue(rateLimitKey, out RateLimitData? data))
        {
            return new RateLimitInfo
            {
                AttemptCount = 0,
                IsBlocked = false
            };
        }

        var resetTime = data.WindowStart.Add(TimeSpan.FromMinutes(5)); // Default window
        TimeSpan? retryAfter = resetTime > DateTime.UtcNow ? resetTime - DateTime.UtcNow : null;

        return new RateLimitInfo
        {
            AttemptCount = data.AttemptCount,
            ResetTime = resetTime,
            IsBlocked = data.AttemptCount > 5, // Default max attempts
            RetryAfter = retryAfter
        };
    }

    public async Task ResetAsync(string key)
    {
        var rateLimitKey = $"rate_limit:{key}";
        _cache.Remove(rateLimitKey);
        _logger.LogInformation("Rate limit reset for key: {Key}", key);
    }

    private class RateLimitData
    {
        public int AttemptCount { get; set; }
        public DateTime WindowStart { get; set; }
    }
}
