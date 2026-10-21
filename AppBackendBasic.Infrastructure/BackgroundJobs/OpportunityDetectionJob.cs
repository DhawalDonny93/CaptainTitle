using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.BackgroundJobs
{
    public class OpportunityDetectionJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IOpportunityDetectionService _opportunityService;
        private readonly ILogger<OpportunityDetectionJob> _logger;

        public OpportunityDetectionJob(
            ApplicationDbContext context,
            IOpportunityDetectionService opportunityService,
            ILogger<OpportunityDetectionJob> logger)
        {
            _context = context;
            _opportunityService = opportunityService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting Opportunity Detection Job at {Time}", startTime);

            try
            {
                // Get all projects with opportunity detection enabled
                var projects = await _context.Projects
                    .Where(p => p.EnableOpportunityDetection == true)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} projects with opportunity detection enabled", projects.Count);

                var successCount = 0;
                var failureCount = 0;
                var totalOpportunities = 0;

                foreach (var project in projects)
                {
                    try
                    {
                        _logger.LogInformation("Detecting opportunities for project {ProjectId}", project.Id);

                        // Detect content gaps
                        var contentGaps = await _opportunityService.DetectContentGapsAsync(project.Id);
                        _logger.LogDebug("Found {Count} content gap opportunities for project {ProjectId}", 
                            contentGaps.Count, project.Id);

                        // Detect SEO opportunities
                        var seoOpps = await _opportunityService.DetectSeoOpportunitiesAsync(project.Id);
                        _logger.LogDebug("Found {Count} SEO opportunities for project {ProjectId}", 
                            seoOpps.Count, project.Id);

                        // Detect visibility opportunities
                        var visibilityOpps = await _opportunityService.DetectVisibilityOpportunitiesAsync(project.Id);
                        _logger.LogDebug("Found {Count} visibility opportunities for project {ProjectId}", 
                            visibilityOpps.Count, project.Id);

                        var projectTotal = contentGaps.Count + seoOpps.Count + visibilityOpps.Count;
                        totalOpportunities += projectTotal;
                        successCount++;

                        _logger.LogInformation("Detected {Total} total opportunities for project {ProjectId}", 
                            projectTotal, project.Id);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex, "Failed to detect opportunities for project {ProjectId}", project.Id);
                    }

                    // Delay between projects
                    await Task.Delay(2000);
                }

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalSeconds;

                // Log job execution
                var jobLog = new Core.Entities.JobExecutionLog
                {
                    JobName = "OpportunityDetectionJob",
                    JobType = "OpportunityDetection",
                    Status = failureCount == 0 ? "Completed" : "PartialSuccess",
                    StartedAt = startTime,
                    CompletedAt = endTime,
                    DurationSeconds = (int)duration,
                    ItemsProcessed = successCount,
                    ItemsSucceeded = successCount,
                    ItemsFailed = failureCount,
                    DetailsJson = $"{{\"summary\":\"Detected {totalOpportunities} total opportunities\"}}",
                    ErrorMessage = failureCount > 0 ? $"{failureCount} projects failed" : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobExecutionLogs.Add(jobLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Opportunity Detection Job completed. Projects: {Success}, Opportunities: {Total}, Duration: {Duration}s",
                    successCount, totalOpportunities, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Opportunity Detection Job failed");
                
                var jobLog = new Core.Entities.JobExecutionLog
                {
                    JobName = "OpportunityDetectionJob",
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
