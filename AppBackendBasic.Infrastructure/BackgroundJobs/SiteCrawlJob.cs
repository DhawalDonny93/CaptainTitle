using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.BackgroundJobs
{
    public class SiteCrawlJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ISiteAuditorService _siteAuditorService;
        private readonly ILogger<SiteCrawlJob> _logger;

        public SiteCrawlJob(
            ApplicationDbContext context,
            ISiteAuditorService siteAuditorService,
            ILogger<SiteCrawlJob> logger)
        {
            _context = context;
            _siteAuditorService = siteAuditorService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting Site Crawl Job at {Time}", startTime);

            try
            {
                // Get all projects with auto-crawl enabled
                var projects = await _context.Projects
                    .Where(p => p.EnableAutoCrawl == true && !string.IsNullOrEmpty(p.WebsiteUrl))
                    .ToListAsync();

                _logger.LogInformation("Found {Count} projects with auto-crawl enabled", projects.Count);

                var successCount = 0;
                var failureCount = 0;

                foreach (var project in projects)
                {
                    try
                    {
                        var options = new CrawlOptions
                        {
                            MaxPages = project.CrawlMaxPages ?? 100,
                            MaxDepth = project.CrawlMaxDepth ?? 3,
                            DelayMs = 1000,
                            FollowExternalLinks = false,
                            RespectRobotsTxt = true
                        };

                        _logger.LogInformation("Starting crawl for project {ProjectId}: {Url}", 
                            project.Id, project.WebsiteUrl);

                        var crawlRun = await _siteAuditorService.StartCrawlAsync(
                            project.Id, 
                            project.WebsiteUrl!, 
                            options,
                            project.UserId);

                        successCount++;
                        _logger.LogInformation("Crawl completed for project {ProjectId}. Pages: {Pages}", 
                            project.Id, crawlRun.TotalPages);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex, "Failed to crawl project {ProjectId}", project.Id);
                    }

                    // Delay between projects to avoid overwhelming the system
                    await Task.Delay(5000);
                }

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalSeconds;

                // Log job execution
                var jobLog = new Core.Entities.JobExecutionLog
                {
                    JobName = "SiteCrawlJob",
                    Status = failureCount == 0 ? "Success" : "PartialSuccess",
                    StartedAt = startTime,
                    CompletedAt = endTime,
                    DurationSeconds = (int)duration,
                    ItemsProcessed = successCount,
                    ItemsFailed = failureCount,
                    ErrorMessage = failureCount > 0 ? $"{failureCount} projects failed to crawl" : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobExecutionLogs.Add(jobLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Site Crawl Job completed. Processed: {Success}, Failed: {Failed}, Duration: {Duration}s",
                    successCount, failureCount, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Site Crawl Job failed");
                
                var jobLog = new Core.Entities.JobExecutionLog
                {
                    JobName = "SiteCrawlJob",
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
