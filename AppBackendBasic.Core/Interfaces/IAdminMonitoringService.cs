using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IAdminMonitoringService
    {
        // System Health
        Task<SystemHealthSnapshot> GetSystemHealthAsync();
        Task<List<SystemHealthMetrics>> GetHealthHistoryAsync(DateRangeDto dateRange);
        Task RecordHealthMetricsAsync();

        // Platform Analytics
        Task<PlatformAnalytics> GetPlatformAnalyticsAsync(DateRangeDto dateRange);
        Task<UsageStatistics> GetUsageStatisticsAsync(DateRangeDto dateRange);
        Task<CostAnalytics> GetCostAnalyticsAsync(DateRangeDto dateRange);

        // Admin Dashboard
        Task<AdminDashboardOverview> GetAdminDashboardAsync();

        // Audit Logs
        Task<List<AdminAuditLog>> GetAuditLogsAsync(int? adminUserId = null, int page = 1, int pageSize = 50);
        Task LogAdminActionAsync(int adminUserId, string action, string? entityType = null, string? entityId = null, string? oldValue = null, string? newValue = null, string? ipAddress = null, string? userAgent = null);
    }
}
