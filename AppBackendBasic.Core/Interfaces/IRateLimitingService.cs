namespace AppBackendBasic.Core.Interfaces;

public interface IRateLimitingService
{
    Task<bool> IsAllowedAsync(string key, int maxAttempts, TimeSpan timeWindow);
    Task<RateLimitInfo> GetRateLimitInfoAsync(string key);
    Task ResetAsync(string key);
}

public class RateLimitInfo
{
    public int AttemptCount { get; set; }
    public DateTime? ResetTime { get; set; }
    public bool IsBlocked { get; set; }
    public TimeSpan? RetryAfter { get; set; }
}
