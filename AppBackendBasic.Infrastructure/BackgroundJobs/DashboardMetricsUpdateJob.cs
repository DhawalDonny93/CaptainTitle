using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.BackgroundJobs
{
    public class DashboardMetricsUpdateJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardMetricsUpdateJob> _logger;

        public DashboardMetricsUpdateJob(
            ApplicationDbContext context,
            ILogger<DashboardMetricsUpdateJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting Dashboard Metrics Update Job at {Time}", startTime);

            try
            {
                var projects = await _context.Projects.ToListAsync();
                _logger.LogInformation("Updating dashboard metrics for {Count} projects", projects.Count);

                var successCount = 0;
                var failureCount = 0;

                foreach (var project in projects)
                {
                    try
                    {
                        var today = DateTime.UtcNow.Date;

                        // Get prompt metrics
                        var totalPrompts = await _context.PromptTrackings
                            .CountAsync(p => p.ProjectId == project.Id);

                        var activePrompts = await _context.PromptTrackings
                            .CountAsync(p => p.ProjectId == project.Id && p.Status == "Active");

                        var promptsWithScores = await _context.PromptTrackings
                            .Where(p => p.ProjectId == project.Id && p.AverageVisibilityScore.HasValue)
                            .Select(p => p.AverageVisibilityScore!.Value)
                            .ToListAsync();
                        
                        var avgPromptScore = promptsWithScores.Any() ? promptsWithScores.Average() : 0;

                        // Get visibility metrics (last 7 days)
                        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                        var recentMetrics = await _context.PromptVisibilityMetrics
                            .Include(m => m.PromptPlatform)
                                .ThenInclude(p => p.Prompt)
                            .Where(m => m.PromptPlatform.Prompt.ProjectId == project.Id
                                && m.CheckDate >= sevenDaysAgo)
                            .ToListAsync();

                        var promptsWithMentions = recentMetrics
                            .Where(m => m.BrandMentioned)
                            .Select(m => m.PromptPlatform.PromptId)
                            .Distinct()
                            .Count();

                        var mentionRate = activePrompts > 0 
                            ? (decimal)promptsWithMentions / activePrompts * 100 
                            : 0;

                        // Get citation metrics (last 30 days)
                        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                        var totalCitations = await _context.Citations
                            .CountAsync(c => c.ProjectId == project.Id && c.DiscoveredAt >= thirtyDaysAgo);

                        var uniqueDomainsCiting = await _context.Citations
                            .Where(c => c.ProjectId == project.Id && c.DiscoveredAt >= thirtyDaysAgo)
                            .Select(c => c.SourceDomain)
                            .Distinct()
                            .CountAsync();

                        // Get site audit metrics (latest)
                        var latestAudit = await _context.SiteAuditRuns
                            .Where(a => a.ProjectId == project.Id)
                            .OrderByDescending(a => a.StartedAt)
                            .FirstOrDefaultAsync();

                        var siteAuditScore = latestAudit?.OverallScore ?? 0;
                        var criticalIssues = latestAudit?.FailedChecks ?? 0;
                        var warningIssues = latestAudit?.WarningChecks ?? 0;
                        var passedChecks = latestAudit?.PassedChecks ?? 0;

                        // Get opportunity metrics
                        var totalOpportunities = 
                            await _context.ContentGapOpportunities.CountAsync(o => o.ProjectId == project.Id && o.Status == "Open") +
                            await _context.SeoOpportunities.CountAsync(o => o.ProjectId == project.Id && o.Status == "Open") +
                            await _context.VisibilityOpportunities.CountAsync(o => o.ProjectId == project.Id && (o.Status == "New" || o.Status == "InProgress"));

                        var highPriorityOpps = 
                            await _context.ContentGapOpportunities.CountAsync(o => o.ProjectId == project.Id && o.Status == "New" && o.EstimatedImpact == "High") +
                            await _context.SeoOpportunities.CountAsync(o => o.ProjectId == project.Id && o.Status == "Open" && o.Priority == "High") +
                            await _context.VisibilityOpportunities.CountAsync(o => o.ProjectId == project.Id && (o.Status == "New" || o.Status == "InProgress") && o.Priority == 1);

                        var opportunitiesActioned = 
                            await _context.ContentGapOpportunities.CountAsync(o => o.ProjectId == project.Id && o.Status == "Implemented") +
                            await _context.SeoOpportunities.CountAsync(o => o.ProjectId == project.Id && o.Status == "Fixed") +
                            await _context.VisibilityOpportunities.CountAsync(o => o.ProjectId == project.Id && o.Status == "Actioned");

                        // Calculate overall visibility score
                        var avgScore = recentMetrics.Any() ? (decimal)recentMetrics.Average(m => m.VisibilityScore) : 0M;
                        var scoreMultiplier = 0.3M;
                        var overallVisibilityScore = (int)(avgScore * scoreMultiplier + ((decimal)avgPromptScore * 0.4M) + (mentionRate * 0.3M) + ((decimal)siteAuditScore * 0.3M));

                        // Check if metric exists for today
                        var existingMetric = await _context.VisibilityDashboardMetrics
                            .FirstOrDefaultAsync(m => m.ProjectId == project.Id && m.MetricDate == today);

                        if (existingMetric != null)
                        {
                            // Update existing
                            existingMetric.OverallVisibilityScore = overallVisibilityScore;
                            existingMetric.TotalPrompts = totalPrompts;
                            existingMetric.ActivePrompts = activePrompts;
                            existingMetric.AveragePromptScore = (decimal)avgPromptScore;
                            existingMetric.PromptsWithMentions = promptsWithMentions;
                            existingMetric.MentionRate = mentionRate;
                            existingMetric.TotalCitations = totalCitations;
                            existingMetric.UniqueDomainsCiting = uniqueDomainsCiting;
                            existingMetric.SiteAuditScore = siteAuditScore;
                            existingMetric.CriticalIssues = criticalIssues;
                            existingMetric.WarningIssues = warningIssues;
                            existingMetric.PassedChecks = passedChecks;
                            existingMetric.TotalOpportunities = totalOpportunities;
                            existingMetric.HighPriorityOpportunities = highPriorityOpps;
                            existingMetric.OpportunitiesActioned = opportunitiesActioned;
                        }
                        else
                        {
                            // Create new metric
                            var metric = new VisibilityDashboardMetrics
                            {
                                ProjectId = project.Id,
                                MetricDate = today,
                                Period = "daily",
                                OverallVisibilityScore = overallVisibilityScore,
                                TotalPrompts = totalPrompts,
                                ActivePrompts = activePrompts,
                                AveragePromptScore = (decimal)avgPromptScore,
                                PromptsWithMentions = promptsWithMentions,
                                MentionRate = mentionRate,
                                TotalCitations = totalCitations,
                                UniqueDomainsCiting = uniqueDomainsCiting,
                                SiteAuditScore = siteAuditScore,
                                CriticalIssues = criticalIssues,
                                WarningIssues = warningIssues,
                                PassedChecks = passedChecks,
                                TotalOpportunities = totalOpportunities,
                                HighPriorityOpportunities = highPriorityOpps,
                                OpportunitiesActioned = opportunitiesActioned,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.VisibilityDashboardMetrics.Add(metric);
                        }

                        successCount++;
                        _logger.LogDebug(
                            "Updated dashboard metrics for project {ProjectId}: Score={Score}, Prompts={Prompts}, Citations={Citations}",
                            project.Id, overallVisibilityScore, activePrompts, totalCitations);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex, "Failed to update dashboard metrics for project {ProjectId}", project.Id);
                    }
                }

                await _context.SaveChangesAsync();

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalSeconds;

                // Log job execution
                var jobLog = new JobExecutionLog
                {
                    JobName = "DashboardMetricsUpdateJob",
                    JobType = "DashboardUpdate",
                    Status = failureCount == 0 ? "Completed" : "PartialSuccess",
                    StartedAt = startTime,
                    CompletedAt = endTime,
                    DurationSeconds = (int)duration,
                    ItemsProcessed = successCount,
                    ItemsSucceeded = successCount,
                    ItemsFailed = failureCount,
                    DetailsJson = $"{{\"summary\":\"Updated dashboard metrics for {successCount} projects\"}}",
                    ErrorMessage = failureCount > 0 ? $"{failureCount} projects failed" : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobExecutionLogs.Add(jobLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Dashboard Metrics Update Job completed. Projects: {Success}, Duration: {Duration}s",
                    successCount, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard Metrics Update Job failed");

                var jobLog = new JobExecutionLog
                {
                    JobName = "DashboardMetricsUpdateJob",
                    Status = "Failed",
                    StartedAt = startTime,
                    CompletedAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobExecutionLogs.Add(jobLog);
                await _context.SaveChangesAsync();

                throw;
            }
        }
    }
}
