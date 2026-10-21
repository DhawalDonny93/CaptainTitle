using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces;

public interface IPaywallAnalyticsService
{
    Task<string> TrackEventAsync(int? userId, PaywallEventDto dto);
    Task<PaywallPerformanceDto> GetPerformanceAsync(DateTime startDate, DateTime endDate, string? paywallId = null);
}
