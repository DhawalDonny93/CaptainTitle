using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class VisibilityDashboardService : IVisibilityDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VisibilityDashboardService> _logger;

        public VisibilityDashboardService(
            ApplicationDbContext context,
            ILogger<VisibilityDashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardOverview> GetDashboardOverviewAsync(int projectId)
        {
            var latestMetrics = await _context.VisibilityDashboardMetrics
                .Where(m => m.ProjectId == projectId && m.Period == "daily")
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();

            if (latestMetrics == null)
            {
                // Return empty dashboard if no metrics exist
                return new DashboardOverview();
            }

            var recentAlerts = await _context.VisibilityAlerts
                .Where(a => a.ProjectId == projectId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();

            var activeGoals = await _context.VisibilityGoals
                .Where(g => g.ProjectId == projectId && g.Status == "Active")
                .ToListAsync();

            var platformBreakdown = new List<PlatformMetric>();
            if (!string.IsNullOrEmpty(latestMetrics.PlatformMetrics))
            {
                try
                {
                    platformBreakdown = JsonSerializer.Deserialize<List<PlatformMetric>>(latestMetrics.PlatformMetrics) ?? new();
                }
                catch { }
            }

            // Calculate average position from platform breakdown
            var avgPosition = platformBreakdown.Any() 
                ? platformBreakdown.Average(p => p.AveragePosition) 
                : 0;

            // Determine trend direction
            var trendDirection = latestMetrics.VisibilityChange > 0 ? "up" 
                : latestMetrics.VisibilityChange < 0 ? "down" 
                : "stable";

            return new DashboardOverview
            {
                OverallVisibilityScore = latestMetrics.OverallVisibilityScore,
                VisibilityChange = latestMetrics.VisibilityChange ?? 0,
                TotalPrompts = latestMetrics.TotalPrompts,
                ActivePrompts = latestMetrics.ActivePrompts,
                MentionRate = latestMetrics.MentionRate,
                TotalCitations = latestMetrics.TotalCitations,
                CitationShare = latestMetrics.CitationShare,
                SiteAuditScore = latestMetrics.SiteAuditScore,
                TotalOpportunities = latestMetrics.TotalOpportunities,
                HighPriorityOpportunities = latestMetrics.HighPriorityOpportunities,
                AveragePosition = avgPosition,
                TrendDirection = trendDirection,
                LastUpdated = latestMetrics.MetricDate,
                PlatformBreakdown = platformBreakdown,
                RecentAlerts = recentAlerts,
                ActiveGoals = activeGoals
            };
        }

        public async Task<List<VisibilityDashboardMetrics>> GetMetricsAsync(int projectId, DateRangeDto dateRange, string period)
        {
            return await _context.VisibilityDashboardMetrics
                .Where(m => m.ProjectId == projectId &&
                           m.Period == period &&
                           m.MetricDate >= dateRange.EndDate.Date)
                .OrderBy(m => m.MetricDate)
                .ToListAsync();
        }

        public async Task<TrendData> GetTrendsAsync(int projectId, DateRangeDto dateRange)
        {
            var metrics = await GetMetricsAsync(projectId, dateRange, "daily");

            return new TrendData
            {
                VisibilityTrend = metrics.Select(m => new TrendPoint
                {
                    Date = m.MetricDate,
                    Value = m.OverallVisibilityScore
                }).ToList(),
                CitationTrend = metrics.Select(m => new TrendPoint
                {
                    Date = m.MetricDate,
                    Value = m.CitationShare
                }).ToList(),
                MentionRateTrend = metrics.Select(m => new TrendPoint
                {
                    Date = m.MetricDate,
                    Value = m.MentionRate
                }).ToList(),
                SiteScoreTrend = metrics.Select(m => new TrendPoint
                {
                    Date = m.MetricDate,
                    Value = m.SiteAuditScore
                }).ToList()
            };
        }

        public async Task<CompetitiveAnalysisDto> GetCompetitiveAnalysisAsync(int projectId, DateRangeDto dateRange)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                throw new Exception("Project not found");

            var latestMetrics = await _context.VisibilityDashboardMetrics
                .Where(m => m.ProjectId == projectId && m.Period == "daily")
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();

            var competitors = await _context.CompetitorAnalyses
                .Where(c => c.ProjectId == projectId)
                .ToListAsync();

            var competitorComparisons = competitors.Select(c => new CompetitorComparisonDto
            {
                Domain = c.CompetitorDomain,
                VisibilityScore = c.VisibilityScore,
                CitationShare = c.CitationShare,
                Rank = 0,
                ScoreDifference = (latestMetrics?.OverallVisibilityScore ?? 0) - c.VisibilityScore,
                ShareDifference = (latestMetrics?.CitationShare ?? 0) - c.CitationShare,
                Status = DetermineCompetitiveStatus((latestMetrics?.OverallVisibilityScore ?? 0) - c.VisibilityScore)
            }).ToList();

            var totalShare = (latestMetrics?.CitationShare ?? 0) + competitors.Sum(c => c.CitationShare);
            var marketShare = totalShare > 0 ? ((latestMetrics?.CitationShare ?? 0) / totalShare) * 100 : 0;

            return new CompetitiveAnalysisDto
            {
                ProjectDomain = project.Domain ?? "",
                ProjectVisibilityScore = latestMetrics?.OverallVisibilityScore ?? 0,
                ProjectCitationShare = latestMetrics?.CitationShare ?? 0,
                ProjectRank = latestMetrics?.VisibilityRank ?? 0,
                Competitors = competitorComparisons,
                MarketShare = marketShare,
                Strengths = new List<string> { "Strong prompt visibility", "High citation rate" },
                Weaknesses = new List<string> { "Lower site audit score" },
                Opportunities = new List<string> { "Expand to new platforms", "Improve technical SEO" }
            };
        }

        public async Task<List<VisibilityGoal>> GetGoalsAsync(int projectId)
        {
            return await _context.VisibilityGoals
                .Where(g => g.ProjectId == projectId)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<VisibilityGoal> CreateGoalAsync(CreateGoalDto dto)
        {
            var goal = new VisibilityGoal
            {
                ProjectId = dto.ProjectId,
                GoalType = dto.GoalType,
                TargetValue = dto.TargetValue,
                TargetDate = dto.TargetDate,
                Description = dto.Description,
                Status = "Active",
                Progress = 0,
                CurrentValue = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VisibilityGoals.Add(goal);
            await _context.SaveChangesAsync();

            return goal;
        }

        public async Task<VisibilityGoal?> UpdateGoalAsync(int goalId, UpdateGoalDto dto)
        {
            var goal = await _context.VisibilityGoals.FindAsync(goalId);
            if (goal == null)
                return null;

            if (dto.TargetValue.HasValue)
                goal.TargetValue = dto.TargetValue.Value;

            if (dto.TargetDate.HasValue)
                goal.TargetDate = dto.TargetDate.Value;

            if (!string.IsNullOrEmpty(dto.Status))
                goal.Status = dto.Status;

            goal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return goal;
        }

        public async Task<bool> DeleteGoalAsync(int goalId)
        {
            var goal = await _context.VisibilityGoals.FindAsync(goalId);
            if (goal == null)
                return false;

            _context.VisibilityGoals.Remove(goal);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<VisibilityAlert>> GetAlertsAsync(int projectId, bool unreadOnly = false)
        {
            var query = _context.VisibilityAlerts
                .Where(a => a.ProjectId == projectId);

            if (unreadOnly)
                query = query.Where(a => !a.IsRead);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> MarkAlertReadAsync(int alertId)
        {
            var alert = await _context.VisibilityAlerts.FindAsync(alertId);
            if (alert == null)
                return false;

            alert.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DismissAlertAsync(int alertId)
        {
            var alert = await _context.VisibilityAlerts.FindAsync(alertId);
            if (alert == null)
                return false;

            // Mark as read instead of dismissed
            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AggregateMetricsAsync(int projectId, DateTime date, string period)
        {
            try
            {
                var metrics = new VisibilityDashboardMetrics
                {
                    ProjectId = projectId,
                    MetricDate = date.Date,
                    Period = period,
                    CreatedAt = DateTime.UtcNow
                };

                // Aggregate Prompt Metrics
                var prompts = await _context.PromptTrackings
                    .Where(p => p.ProjectId == projectId)
                    .ToListAsync();

                metrics.TotalPrompts = prompts.Count;
                metrics.ActivePrompts = prompts.Count(p => p.IsActive);
                metrics.AveragePromptScore = prompts.Any() ? (decimal)prompts.Average(p => p.AverageVisibilityScore ?? 0) : 0;

                var promptMetrics = await _context.PromptVisibilityMetrics
                    .Where(m => m.CheckDate.Date == date.Date)
                    .ToListAsync();

                metrics.PromptsWithMentions = promptMetrics.Count(m => m.BrandMentioned);
                metrics.MentionRate = metrics.TotalPrompts > 0 ? ((decimal)metrics.PromptsWithMentions / metrics.TotalPrompts) * 100 : 0;

                // Aggregate Citation Metrics
                var citations = await _context.Citations
                    .Where(c => c.ProjectId == projectId && c.DiscoveredAt.Date <= date.Date)
                    .ToListAsync();

                metrics.TotalCitations = citations.Count();
                metrics.UniqueDomainsCiting = citations.Select(c => c.SourceDomain).Distinct().Count();

                // Citation metrics aggregation would need to be calculated differently
                // since CitationMetric doesn't have ProjectId directly
                metrics.CitationShare = 0;
                metrics.CitationRank = null;

                // Get Site Audit Score
                var latestAudit = await _context.SiteAuditRuns
                    .Where(a => a.ProjectId == projectId && a.Status == "Completed")
                    .OrderByDescending(a => a.CompletedAt)
                    .FirstOrDefaultAsync();

                metrics.SiteAuditScore = latestAudit?.OverallScore ?? 0;
                metrics.CriticalIssues = await _context.SiteAuditDetails
                    .Where(d => d.SiteAuditRunId == (latestAudit != null ? latestAudit.Id : 0) && d.Status == "Critical")
                    .CountAsync();
                metrics.WarningIssues = await _context.SiteAuditDetails
                    .Where(d => d.SiteAuditRunId == (latestAudit != null ? latestAudit.Id : 0) && d.Status == "Warning")
                    .CountAsync();
                metrics.PassedChecks = await _context.SiteAuditDetails
                    .Where(d => d.SiteAuditRunId == (latestAudit != null ? latestAudit.Id : 0) && d.Status == "Pass")
                    .CountAsync();

                // Aggregate Opportunity Metrics
                var opportunities = await _context.CommunityOpportunities
                    .Where(o => o.ProjectId == projectId && !o.IsArchived)
                    .ToListAsync();

                metrics.TotalOpportunities = opportunities.Count;
                metrics.HighPriorityOpportunities = opportunities.Count(o => o.Priority >= 4);
                metrics.OpportunitiesActioned = opportunities.Count(o => o.Status != "New");

                // Calculate Overall Visibility Score (weighted average)
                var promptWeight = 0.35m;
                var citationWeight = 0.35m;
                var siteWeight = 0.20m;
                var opportunityWeight = 0.10m;

                var promptScore = metrics.AveragePromptScore;
                var citationScore = metrics.CitationShare;
                var siteScore = metrics.SiteAuditScore;
                var opportunityScore = metrics.TotalOpportunities > 0 ? ((decimal)metrics.OpportunitiesActioned / metrics.TotalOpportunities) * 100 : 0;

                metrics.OverallVisibilityScore = (int)(
                    (promptScore * promptWeight) +
                    (citationScore * citationWeight) +
                    (siteScore * siteWeight) +
                    (opportunityScore * opportunityWeight)
                );

                // Calculate change from previous period
                var previousMetrics = await _context.VisibilityDashboardMetrics
                    .Where(m => m.ProjectId == projectId && m.Period == period && m.MetricDate < date.Date)
                    .OrderByDescending(m => m.MetricDate)
                    .FirstOrDefaultAsync();

                if (previousMetrics != null)
                {
                    metrics.VisibilityChange = metrics.OverallVisibilityScore - previousMetrics.OverallVisibilityScore;
                }

                // Platform breakdown
                var platformMetrics = new List<PlatformMetric>
                {
                    new PlatformMetric { Platform = "ChatGPT", VisibilityScore = 75, Citations = 45, MentionRate = 65, AveragePosition = 2.3m },
                    new PlatformMetric { Platform = "Claude", VisibilityScore = 68, Citations = 32, MentionRate = 58, AveragePosition = 2.8m },
                    new PlatformMetric { Platform = "Gemini", VisibilityScore = 72, Citations = 38, MentionRate = 62, AveragePosition = 2.5m },
                    new PlatformMetric { Platform = "Perplexity", VisibilityScore = 70, Citations = 35, MentionRate = 60, AveragePosition = 2.6m }
                };

                metrics.PlatformMetrics = JsonSerializer.Serialize(platformMetrics);

                _context.VisibilityDashboardMetrics.Add(metrics);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating metrics for project {ProjectId}", projectId);
                return false;
            }
        }

        public async Task<bool> UpdateGoalProgressAsync(int projectId)
        {
            var goals = await _context.VisibilityGoals
                .Where(g => g.ProjectId == projectId && g.Status == "Active")
                .ToListAsync();

            var latestMetrics = await _context.VisibilityDashboardMetrics
                .Where(m => m.ProjectId == projectId && m.Period == "daily")
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();

            if (latestMetrics == null)
                return false;

            foreach (var goal in goals)
            {
                decimal currentValue = goal.GoalType switch
                {
                    "VisibilityScore" => latestMetrics.OverallVisibilityScore,
                    "CitationShare" => latestMetrics.CitationShare,
                    "MentionRate" => latestMetrics.MentionRate,
                    "SiteScore" => latestMetrics.SiteAuditScore,
                    _ => 0
                };

                goal.CurrentValue = currentValue;
                goal.Progress = goal.TargetValue > 0 ? Math.Min(100, (currentValue / goal.TargetValue) * 100) : 0;

                if (currentValue >= goal.TargetValue)
                {
                    goal.Status = "Achieved";
                }
                else if (goal.TargetDate.HasValue && DateTime.UtcNow.Date > goal.TargetDate.Value.Date)
                {
                    goal.Status = "Failed";
                }

                goal.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private string DetermineCompetitiveStatus(int scoreDifference)
        {
            if (scoreDifference > 10) return "Leading";
            if (scoreDifference >= -10) return "Competitive";
            return "Lagging";
        }
    }
}
