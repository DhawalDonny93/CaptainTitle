using Hangfire;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.BackgroundJobs;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        ILogger<BackgroundJobService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    public string EnqueueJob(string jobName, Action job)
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue(() => job());
            _logger.LogInformation("Enqueued job '{JobName}' with ID: {JobId}", jobName, jobId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue job '{JobName}'", jobName);
            throw;
        }
    }

    public string ScheduleJob(string jobName, Action job, TimeSpan delay)
    {
        try
        {
            var jobId = _backgroundJobClient.Schedule(() => job(), delay);
            _logger.LogInformation("Scheduled job '{JobName}' with ID: {JobId} to run after {Delay}", 
                jobName, jobId, delay);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule job '{JobName}'", jobName);
            throw;
        }
    }

    public void AddOrUpdateRecurringJob(string jobId, string jobName, Action job, string cronExpression)
    {
        try
        {
            // TODO: Uncomment RecurringJobManager call once Hangfire background job infrastructure is fully tested
            // Current: Placeholder implementation that logs job addition but doesn't actually schedule
            // Action: Uncomment line below, ensure _recurringJobManager is properly initialized, verify cron expression format
            // _recurringJobManager.AddOrUpdate(jobId, job, cronExpression, new RecurringJobOptions());
            _logger.LogInformation("Added/Updated recurring job '{JobName}' with ID: {JobId} and cron: {Cron}", 
                jobName, jobId, cronExpression);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add/update recurring job '{JobName}'", jobName);
            throw;
        }
    }

    public void RemoveRecurringJob(string jobId)
    {
        try
        {
            _recurringJobManager.RemoveIfExists(jobId);
            _logger.LogInformation("Removed recurring job with ID: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove recurring job with ID: {JobId}", jobId);
            throw;
        }
    }

    public bool DeleteJob(string jobId)
    {
        try
        {
            var result = _backgroundJobClient.Delete(jobId);
            _logger.LogInformation("Deleted job with ID: {JobId}, Result: {Result}", jobId, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete job with ID: {JobId}", jobId);
            return false;
        }
    }
}
