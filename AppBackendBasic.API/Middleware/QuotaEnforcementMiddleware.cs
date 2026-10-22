using AppBackendBasic.Core.Interfaces;
using System.Security.Claims;

namespace AppBackendBasic.API.Middleware
{
    public class QuotaEnforcementMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<QuotaEnforcementMiddleware> _logger;

        public QuotaEnforcementMiddleware(
            RequestDelegate next,
            ILogger<QuotaEnforcementMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptionService)
        {
            // Only check quota for authenticated requests
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                await _next(context);
                return;
            }

            // Check quota for specific endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var method = context.Request.Method.ToUpper();
            string? quotaType = null;

            // Only check quota for POST/PUT operations that consume resources
            if (method == "POST" || method == "PUT")
            {
                if (path.Contains("/llm/") || path.Contains("/llmproviders/generate"))
                    quotaType = "LlmRequests";
            }

            if (quotaType != null)
            {
                try
                {
                    var hasQuota = await subscriptionService.CheckQuotaAvailableAsync(userId, quotaType);
                    
                    if (!hasQuota)
                    {
                        context.Response.StatusCode = 429; // Too Many Requests
                        context.Response.ContentType = "application/json";
                        
                        var quota = await subscriptionService.GetQuotaStatusAsync(userId, quotaType);
                        
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "Quota exceeded",
                            message = $"You have exceeded your {quotaType} quota. Please upgrade your plan to continue.",
                            quotaType,
                            limit = quota.Limit,
                            used = quota.Used,
                            remaining = quota.Remaining,
                            percentageUsed = quota.PercentageUsed,
                            upgradeUrl = "/pricing"
                        });
                        
                        _logger.LogWarning("Quota exceeded for user {UserId}, quota type {QuotaType}", userId, quotaType);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking quota for user {UserId}, quota type {QuotaType}", userId, quotaType);
                    // Continue on error - don't block user
                }
            }

            await _next(context);
        }
    }

    public static class QuotaEnforcementMiddlewareExtensions
    {
        public static IApplicationBuilder UseQuotaEnforcement(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<QuotaEnforcementMiddleware>();
        }
    }
}
