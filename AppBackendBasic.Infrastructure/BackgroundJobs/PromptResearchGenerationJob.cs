using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.BackgroundJobs
{
    public class PromptResearchGenerationJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IPromptResearchService _promptResearchService;
        private readonly ILogger<PromptResearchGenerationJob> _logger;

        public PromptResearchGenerationJob(
            ApplicationDbContext context,
            IPromptResearchService promptResearchService,
            ILogger<PromptResearchGenerationJob> logger)
        {
            _context = context;
            _promptResearchService = promptResearchService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting Prompt Research Generation Job at {Time}", startTime);

            try
            {
                var projects = await _context.Projects.ToListAsync();

                _logger.LogInformation("Generating prompt research for {Count} projects", projects.Count);

                var successCount = 0;
                var failureCount = 0;
                var totalPromptsGenerated = 0;

                foreach (var project in projects)
                {
                    try
                    {
                        // Get existing topics for this project
                        var existingTopics = await _context.PromptResearchDatas
                            .Where(p => p.ProjectId == project.Id)
                            .Select(p => p.Topic)
                            .Distinct()
                            .ToListAsync();

                        // Generate topic based on project industry or use default
                        var topic = !string.IsNullOrEmpty(project.Industry) 
                            ? $"{project.Industry} trends" 
                            : "Industry insights";

                        var audience = !string.IsNullOrEmpty(project.TargetAudience)
                            ? project.TargetAudience
                            : "General audience";

                        _logger.LogInformation(
                            "Generating prompt research for project {ProjectId}: Topic='{Topic}', Audience='{Audience}'",
                            project.Id, topic, audience);

                        // Generate 20 new prompt suggestions
                        var prompts = await _promptResearchService.GeneratePromptResearchAsync(
                            projectId: project.Id,
                            topic: topic,
                            industry: project.Industry ?? "General",
                            audience: audience,
                            count: 20
                        );

                        totalPromptsGenerated += prompts.Count;
                        successCount++;

                        _logger.LogInformation(
                            "Generated {Count} prompt suggestions for project {ProjectId}",
                            prompts.Count, project.Id);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex, "Failed to generate prompt research for project {ProjectId}", project.Id);
                    }

                    // Delay between projects to avoid rate limiting
                    await Task.Delay(2000);
                }

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalSeconds;

                // Log job execution
                var jobLog = new Core.Entities.JobExecutionLog
                {
                    JobName = "PromptResearchGenerationJob",
                    JobType = "PromptResearch",
                    Status = failureCount == 0 ? "Completed" : "PartialSuccess",
                    StartedAt = startTime,
                    CompletedAt = endTime,
                    DurationSeconds = (int)duration,
                    ItemsProcessed = successCount,
                    ItemsSucceeded = successCount,
                    ItemsFailed = failureCount,
                    DetailsJson = $"{{\"summary\":\"Generated {totalPromptsGenerated} prompt suggestions for {successCount} projects\"}}",
                    ErrorMessage = failureCount > 0 ? $"{failureCount} projects failed" : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobExecutionLogs.Add(jobLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Prompt Research Generation Job completed. Projects: {Success}, Prompts: {Total}, Duration: {Duration}s",
                    successCount, totalPromptsGenerated, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prompt Research Generation Job failed");

                var jobLog = new Core.Entities.JobExecutionLog
                {
                    JobName = "PromptResearchGenerationJob",
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
