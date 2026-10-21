using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.BackgroundJobs
{
    public class VisibilityTrendAggregationJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VisibilityTrendAggregationJob> _logger;

        public VisibilityTrendAggregationJob(
            ApplicationDbContext context,
            ILogger<VisibilityTrendAggregationJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting Visibility Trend Aggregation Job at {Time}", startTime);

            try
            {
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                var projects = await _context.Projects.ToListAsync();

                _logger.LogInformation("Aggregating visibility trends for {Count} projects", projects.Count);

                var successCount = 0;
                var failureCount = 0;
                var alertsCreated = 0;

                foreach (var project in projects)
                {
                    try
                    {
                        // Get yesterday's visibility metrics
                        var yesterdayMetrics = await _context.PromptVisibilityMetrics
                            .Include(m => m.PromptPlatform)
                                .ThenInclude(p => p.Prompt)
                            .Where(m => m.PromptPlatform.Prompt.ProjectId == project.Id
                                && m.CheckDate >= yesterday
                                && m.CheckDate < yesterday.AddDays(1))
                            .ToListAsync();

                        if (!yesterdayMetrics.Any())
                        {
                            _logger.LogDebug("No metrics found for project {ProjectId} on {Date}", 
                                project.Id, yesterday);
                            continue;
                        }

                        // Calculate average visibility score
                        var avgVisibility = yesterdayMetrics.Average(m => m.VisibilityScore);

                        // Get previous day's trend for comparison
                        var previousTrend = await _context.VisibilityTrends
                            .Where(t => t.ProjectId == project.Id)
                            .OrderByDescending(t => t.TrendDate)
                            .FirstOrDefaultAsync();

                        var changeFromPrevious = previousTrend != null ? ((decimal)avgVisibility - previousTrend.AverageVisibilityScore) : 0M;
                        var changePercentage = previousTrend != null && previousTrend.AverageVisibilityScore > 0
                            ? (double)((changeFromPrevious / previousTrend.AverageVisibilityScore) * 100)
                            : 0;

                        // Determine trend direction
                        string trendDirection = "Stable";
                        if (Math.Abs(changePercentage) > 5)
                        {
                            trendDirection = changePercentage > 0 ? "Improving" : "Declining";
                        }

                        // Calculate sentiment counts
                        var positiveMentions = yesterdayMetrics.Count(m => m.Sentiment == "Positive");
                        var neutralMentions = yesterdayMetrics.Count(m => m.Sentiment == "Neutral");
                        var negativeMentions = yesterdayMetrics.Count(m => m.Sentiment == "Negative");

                        // Create trend record
                        var trend = new VisibilityTrend
                        {
                            ProjectId = project.Id,
                            TrendDate = yesterday,
                            Platform = null, // Aggregate across all platforms
                            Topic = null, // Aggregate across all topics
                            TotalPromptsChecked = yesterdayMetrics.Count,
                            PromptsWithMention = yesterdayMetrics.Count(m => m.BrandMentioned),
                            AverageVisibilityScore = (decimal)avgVisibility,
                            AveragePosition = yesterdayMetrics
                                .Where(m => m.Position.HasValue)
                                .Select(m => (decimal)m.Position!.Value)
                                .DefaultIfEmpty(0)
                                .Average(),
                            PositiveMentions = positiveMentions,
                            NeutralMentions = neutralMentions,
                            NegativeMentions = negativeMentions,
                            CompetitorMentions = 0, // Would need competitor tracking data
                            ShareOfVoice = null, // Would need competitor data to calculate
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.VisibilityTrends.Add(trend);

                        // Create alert if significant drop (>20%)
                        if (project.EnableVisibilityAlerts == true && changePercentage < -20)
                        {
                            var alertDetails = new
                            {
                                CurrentValue = avgVisibility,
                                PreviousValue = previousTrend.AverageVisibilityScore,
                                ChangePercentage = changePercentage,
                                TrendDirection = trendDirection
                            };

                            var alert = new VisibilityAlert
                            {
                                ProjectId = project.Id,
                                AlertType = "VisibilityDrop",
                                Severity = "Critical",
                                Message = $"Visibility score dropped by {Math.Abs(changePercentage):F1}% (from {previousTrend.AverageVisibilityScore:F1} to {avgVisibility:F1})",
                                PromptText = null,
                                Platform = null,
                                DetailsJson = System.Text.Json.JsonSerializer.Serialize(alertDetails),
                                IsRead = false,
                                ReadAt = null,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.VisibilityAlerts.Add(alert);
                            alertsCreated++;

                            _logger.LogWarning(
                                "Visibility alert created for project {ProjectId}: {Message}",
                                project.Id, alert.Message);
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex, "Failed to aggregate trends for project {ProjectId}", project.Id);
                    }
                }

                await _context.SaveChangesAsync();

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalSeconds;

                // Log job execution
                var jobLog = new JobExecutionLog
                {
                    JobName = "VisibilityTrendAggregationJob",
                    JobType = "TrendAggregation",
                    Status = failureCount == 0 ? "Completed" : "PartialSuccess",
                    StartedAt = startTime,
                    CompletedAt = endTime,
                    DurationSeconds = (int)duration,
                    ItemsProcessed = successCount,
                    ItemsSucceeded = successCount,
                    ItemsFailed = failureCount,
                    DetailsJson = $"{{\"summary\":\"Aggregated trends for {successCount} projects, created {alertsCreated} alerts\"}}",
                    ErrorMessage = failureCount > 0 ? $"{failureCount} projects failed" : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobExecutionLogs.Add(jobLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Visibility Trend Aggregation Job completed. Projects: {Success}, Alerts: {Alerts}, Duration: {Duration}s",
                    successCount, alertsCreated, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Visibility Trend Aggregation Job failed");

                var jobLog = new JobExecutionLog
                {
                    JobName = "VisibilityTrendAggregationJob",
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
