using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces;

public interface IAnalyticsService
{
    // Event Tracking
    Task<TrackEventResponseDto> TrackEventAsync(TrackEventRequestDto request);
    
    // User Properties
    Task<UserPropertiesResponseDto> SetUserPropertiesAsync(UserPropertiesRequestDto request);
    Task<IncrementPropertyResponseDto> IncrementUserPropertyAsync(IncrementPropertyRequestDto request);
    
    // Revenue Tracking
    Task<TrackRevenueResponseDto> TrackRevenueAsync(TrackRevenueRequestDto request);
    
    // User Analytics
    Task<UserAnalyticsDto> GetUserAnalyticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    
    // Dashboard Analytics
    Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, string? metrics = null);
    
    // Funnels
    Task<CreateFunnelResponseDto> CreateFunnelAsync(CreateFunnelRequestDto request);
    Task<FunnelResultsDto> GetFunnelResultsAsync(string funnelId, DateTime? startDate = null, DateTime? endDate = null);
    
    // Export
    Task<ExportResponseDto> CreateExportAsync(ExportRequestDto request);
    Task<ExportResponseDto> GetExportStatusAsync(string exportId);
    
    // Cohorts
    Task<CreateCohortResponseDto> CreateCohortAsync(CreateCohortRequestDto request);
}
