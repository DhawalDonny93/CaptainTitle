using System;
using System.Collections.Generic;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.DTOs
{
    public class SystemHealthSnapshot
    {
        public decimal CpuUsagePercent { get; set; }
        public long MemoryUsageMb { get; set; }
        public int ActiveConnections { get; set; }
        public int RequestsPerMinute { get; set; }
        public int AverageResponseTimeMs { get; set; }
        public decimal ErrorRate { get; set; }
        public int BackgroundJobsRunning { get; set; }
        public int BackgroundJobsFailed { get; set; }
        public int HealthScore { get; set; } // 0-100
        public string Status { get; set; } = "Healthy"; // Healthy, Warning, Critical
    }

    public class PlatformAnalytics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalCaptionProjects { get; set; }
        public int CompletedCaptionProjects { get; set; }
        public int FailedCaptionProjects { get; set; }
        public int PaidCaptionTransactions { get; set; }
        public decimal TotalLlmCost { get; set; }
        public int TotalLlmRequests { get; set; }
    }

    public class UsageStatistics
    {
        public Dictionary<string, int> RequestsByEndpoint { get; set; } = new();
        public Dictionary<string, int> RequestsByUser { get; set; } = new();
        public Dictionary<string, decimal> CostByProvider { get; set; } = new();
        public List<TrendPoint> DailyActiveUsers { get; set; } = new();
        public List<TrendPoint> DailyRequests { get; set; } = new();
    }

    public class CostAnalytics
    {
        public decimal TotalCost { get; set; }
        public Dictionary<string, decimal> CostByProvider { get; set; } = new();
        public Dictionary<string, decimal> CostByModel { get; set; } = new();
        public List<TrendPoint> DailyCosts { get; set; } = new();
        public decimal AverageCostPerRequest { get; set; }
        public int TotalRequests { get; set; }
    }

    public class BackgroundJobStatus
    {
        public string JobId { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Running, Completed, Failed, Pending
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
    }

    public class TestEmailDto
    {
        public string RecipientEmail { get; set; } = string.Empty;
    }

    public class CreateFeatureFlagDto
    {
        public string FlagKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
        public int RolloutPercentage { get; set; }
    }

    public class UpdateFeatureFlagDto
    {
        public bool? IsEnabled { get; set; }
        public int? RolloutPercentage { get; set; }
        public string? EnabledForUserIds { get; set; }
        public string? EnabledForRoles { get; set; }
    }

    public class CreateEmailTemplateDto
    {
        public string TemplateKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? TextBody { get; set; }
        public List<string> Variables { get; set; } = new();
    }

    public class UpdateEmailTemplateDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? TextBody { get; set; }
        public string? Variables { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminDashboardOverview
    {
        public PlatformAnalytics Analytics { get; set; } = new();
        public SystemHealthSnapshot Health { get; set; } = new();
        public List<BackgroundJobStatus> RecentJobs { get; set; } = new();
        public List<AdminAuditLog> RecentAuditLogs { get; set; } = new();
    }
}
