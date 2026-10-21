using AppBackendBasic.Services.BackgroundJobs.Jobs;
using AppBackendBasic.Core.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.BackgroundJobs;

public class JobScheduler
{
    private readonly ILogger<JobScheduler> _logger;
    private readonly IRecurringJobManager _recurringJobManager;

    public JobScheduler(ILogger<JobScheduler> logger, IRecurringJobManager recurringJobManager)
    {
        _logger = logger;
        _recurringJobManager = recurringJobManager;
    }

    /// <summary>
    /// Configure all recurring jobs
    /// </summary>
    public void ConfigureRecurringJobs()
    {
        _logger.LogInformation("Configuring recurring background jobs...");

        try
        {
            // Email Queue Processing - Every 5 minutes
            _recurringJobManager.AddOrUpdate<EmailQueueJob>(
                "process-email-queue",
                job => job.ProcessEmailQueueAsync(),
                "*/5 * * * *"); // Every 5 minutes

            // Cleanup Expired Refresh Tokens - Daily at 2 AM
            _recurringJobManager.AddOrUpdate<CleanupJob>(
                "cleanup-expired-tokens",
                job => job.CleanupExpiredRefreshTokensAsync(),
                Cron.Daily(2));

            // Cleanup Old Audit Logs - Weekly on Sunday at 3 AM
            _recurringJobManager.AddOrUpdate<CleanupJob>(
                "cleanup-old-audit-logs",
                job => job.CleanupOldAuditLogsAsync(),
                Cron.Weekly(DayOfWeek.Sunday, 3));

            // Cleanup Inactive Users - Monthly on 1st at 4 AM
            _recurringJobManager.AddOrUpdate<CleanupJob>(
                "cleanup-inactive-users",
                job => job.CleanupInactiveUsersAsync(),
                Cron.Monthly(1, 4));

            // Cleanup Expired Password Reset Tokens - Daily at 1 AM
            _recurringJobManager.AddOrUpdate<CleanupJob>(
                "cleanup-expired-password-tokens",
                job => job.CleanupExpiredPasswordResetTokensAsync(),
                Cron.Daily(1));

            // Cleanup Old Notifications - Daily at 5 AM
            _recurringJobManager.AddOrUpdate<CleanupJob>(
                "cleanup-old-notifications",
                job => job.CleanupOldNotificationsAsync(),
                Cron.Daily(5));

            // Generate Daily Statistics - Daily at 6 AM
            _recurringJobManager.AddOrUpdate<CleanupJob>(
                "generate-daily-stats",
                job => job.GenerateDailyStatsAsync(),
                Cron.Daily(6));

            // Run All Cleanup Tasks - Weekly on Monday at 3 AM
            _recurringJobManager.AddOrUpdate<CleanupJob>(
                "run-all-cleanup-tasks",
                job => job.RunAllCleanupTasksAsync(),
                Cron.Weekly(DayOfWeek.Monday, 3));

            // AI SEO: Auto-Blog Publishing - Daily at 9 AM
            _recurringJobManager.AddOrUpdate<IAutoBlogService>(
                "auto-blog-publishing",
                job => job.PublishScheduledArticlesAsync(),
                Cron.Daily(9));

            // AI SEO: AI Visibility Tracking - Daily at 10 AM
            _recurringJobManager.AddOrUpdate<IAiTrackingService>(
                "ai-visibility-tracking",
                job => job.TrackAllActivePromptsAsync(),
                Cron.Daily(10));

            // Social Signal Engine: Opportunity Scan - Daily at 11 AM
            _recurringJobManager.AddOrUpdate<ISocialMonitoringService>(
                "social-monitoring-scan",
                job => job.ScanOpportunitiesAsync(),
                Cron.Daily(11));

            // Agentic Auditor: Weekly Site Audit - Mondays at 9 AM
            _recurringJobManager.AddOrUpdate<ISiteAuditService>(
                "site-audit-scan",
                job => job.RunAllProjectAuditsAsync(),
                Cron.Weekly(DayOfWeek.Monday, 9));

            _logger.LogInformation("Recurring background jobs configured successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring recurring jobs");
            throw;
        }
    }

    /// <summary>
    /// Remove all recurring jobs (for testing or maintenance)
    /// </summary>
    public void RemoveAllRecurringJobs()
    {
        _logger.LogInformation("Removing all recurring jobs...");

        try
        {
            _recurringJobManager.RemoveIfExists("process-email-queue");
            _recurringJobManager.RemoveIfExists("cleanup-expired-tokens");
            _recurringJobManager.RemoveIfExists("cleanup-old-audit-logs");
            _recurringJobManager.RemoveIfExists("cleanup-inactive-users");
            _recurringJobManager.RemoveIfExists("cleanup-expired-password-tokens");
            _recurringJobManager.RemoveIfExists("cleanup-old-notifications");
            _recurringJobManager.RemoveIfExists("generate-daily-stats");
            _recurringJobManager.RemoveIfExists("run-all-cleanup-tasks");

            _logger.LogInformation("All recurring jobs removed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing recurring jobs");
            throw;
        }
    }
}

/// <summary>
/// Common cron expressions for reference
/// </summary>
public static class CronExpressions
{
    /// <summary>Every minute</summary>
    public const string EveryMinute = "* * * * *";
    
    /// <summary>Every 5 minutes</summary>
    public const string Every5Minutes = "*/5 * * * *";
    
    /// <summary>Every 15 minutes</summary>
    public const string Every15Minutes = "*/15 * * * *";
    
    /// <summary>Every 30 minutes</summary>
    public const string Every30Minutes = "*/30 * * * *";
    
    /// <summary>Every hour</summary>
    public const string Hourly = "0 * * * *";
    
    /// <summary>Daily at midnight</summary>
    public const string DailyAtMidnight = "0 0 * * *";
    
    /// <summary>Daily at 2 AM</summary>
    public const string DailyAt2AM = "0 2 * * *";
    
    /// <summary>Weekly on Monday at 3 AM</summary>
    public const string WeeklyMonday3AM = "0 3 * * 1";
    
    /// <summary>Monthly on 1st at 4 AM</summary>
    public const string Monthly1st4AM = "0 4 1 * *";
}
