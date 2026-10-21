using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IVisibilityDashboardService
    {
        // Dashboard Data
        Task<DashboardOverview> GetDashboardOverviewAsync(int projectId);
        Task<List<VisibilityDashboardMetrics>> GetMetricsAsync(int projectId, DateRangeDto dateRange, string period);
        Task<TrendData> GetTrendsAsync(int projectId, DateRangeDto dateRange);
        
        // Competitive Analysis
        Task<CompetitiveAnalysisDto> GetCompetitiveAnalysisAsync(int projectId, DateRangeDto dateRange);
        
        // Goals Management
        Task<List<VisibilityGoal>> GetGoalsAsync(int projectId);
        Task<VisibilityGoal> CreateGoalAsync(CreateGoalDto dto);
        Task<VisibilityGoal?> UpdateGoalAsync(int goalId, UpdateGoalDto dto);
        Task<bool> DeleteGoalAsync(int goalId);
        
        // Alerts
        Task<List<VisibilityAlert>> GetAlertsAsync(int projectId, bool unreadOnly = false);
        Task<bool> MarkAlertReadAsync(int alertId);
        Task<bool> DismissAlertAsync(int alertId);
        
        // Background Aggregation
        Task<bool> AggregateMetricsAsync(int projectId, DateTime date, string period);
        Task<bool> UpdateGoalProgressAsync(int projectId);
    }
}
