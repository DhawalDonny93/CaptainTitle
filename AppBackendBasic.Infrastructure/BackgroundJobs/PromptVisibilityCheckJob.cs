using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.BackgroundJobs
{
    public class PromptVisibilityCheckJob
    {
        private readonly IPromptTrackingService _promptTrackingService;
        private readonly ILogger<PromptVisibilityCheckJob> _logger;

        public PromptVisibilityCheckJob(
            IPromptTrackingService promptTrackingService,
            ILogger<PromptVisibilityCheckJob> logger)
        {
            _promptTrackingService = promptTrackingService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting Prompt Visibility Check Job at {Time}", startTime);

            try
            {
                await _promptTrackingService.ProcessAllActivePromptsAsync();

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalSeconds;

                _logger.LogInformation(
                    "Prompt Visibility Check Job completed. Duration: {Duration}s",
                    duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prompt Visibility Check Job failed");
                throw;
            }
        }
    }
}
