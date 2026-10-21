using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.BackgroundJobs
{
    public class CitationAggregationJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CitationAggregationJob> _logger;

        public CitationAggregationJob(
            ApplicationDbContext context,
            ILogger<CitationAggregationJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting Citation Aggregation Job at {Time}", startTime);

            try
            {
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                var projects = await _context.Projects.ToListAsync();

                _logger.LogInformation("Aggregating citations for {Count} projects", projects.Count);

                var successCount = 0;
                var failureCount = 0;

                foreach (var project in projects)
                {
                    try
                    {
                        // Get yesterday's citations
                        var citations = await _context.Citations
                            .Where(c => c.ProjectId == project.Id
                                && c.DiscoveredAt >= yesterday
                                && c.DiscoveredAt < yesterday.AddDays(1))
                            .ToListAsync();

                        if (!citations.Any())
                        {
                            _logger.LogDebug("No citations found for project {ProjectId} on {Date}", 
                                project.Id, yesterday);
                            continue;
                        }

                        // Calculate basic citation metrics for logging
                        var totalCitations = citations.Count;
                        var uniqueDomains = citations.Select(c => c.SourceDomain).Distinct().Count();
                        var directLinks = citations.Count(c => c.CitationType == "DirectLink");
                        var references = citations.Count(c => c.CitationType == "Reference");
                        var inlineCitations = citations.Count(c => c.CitationType == "Inline");

                        // Get top domains for logging
                        var topDomains = citations
                            .GroupBy(c => c.SourceDomain)
                            .OrderByDescending(g => g.Count())
                            .Take(5)
                            .Select(g => new { Domain = g.Key, Count = g.Count() })
                            .ToList();

                        // Note: CitationMetric entity is designed for individual citation tracking,
                        // not for aggregated daily metrics. If aggregation is needed, a new entity
                        // like CitationDailyAggregate should be created with proper schema.

                        _logger.LogInformation(
                            "Aggregated {Citations} citations from {Domains} domains for project {ProjectId}. Direct: {Direct}, References: {Refs}, Inline: {Inline}",
                            totalCitations, uniqueDomains, project.Id, directLinks, references, inlineCitations);

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex, "Failed to aggregate citations for project {ProjectId}", project.Id);
                    }
                }

                await _context.SaveChangesAsync();

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalSeconds;

                // Log job execution
                var jobLog = new JobExecutionLog
                {
                    JobName = "CitationAggregationJob",
                    Status = failureCount == 0 ? "Success" : "PartialSuccess",
                    StartedAt = startTime,
                    CompletedAt = endTime,
                    DurationSeconds = (int)duration,
                    ItemsProcessed = successCount,
                    ItemsFailed = failureCount,
                    DetailsJson = $"{{\"summary\":\"Aggregated citations for {successCount} projects\"}}",
                    ErrorMessage = failureCount > 0 ? $"{failureCount} projects failed" : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobExecutionLogs.Add(jobLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Citation Aggregation Job completed. Projects: {Success}, Duration: {Duration}s",
                    successCount, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Citation Aggregation Job failed");

                var jobLog = new JobExecutionLog
                {
                    JobName = "CitationAggregationJob",
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
