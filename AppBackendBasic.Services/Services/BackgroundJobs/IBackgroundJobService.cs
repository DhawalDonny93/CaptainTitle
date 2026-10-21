namespace AppBackendBasic.Services.BackgroundJobs;

public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueue a job to be executed in the background
    /// </summary>
    string EnqueueJob(string jobName, Action job);
    
    /// <summary>
    /// Schedule a job to run after a delay
    /// </summary>
    string ScheduleJob(string jobName, Action job, TimeSpan delay);
    
    /// <summary>
    /// Schedule a recurring job with cron expression
    /// </summary>
    void AddOrUpdateRecurringJob(string jobId, string jobName, Action job, string cronExpression);
    
    /// <summary>
    /// Remove a recurring job
    /// </summary>
    void RemoveRecurringJob(string jobId);
    
    /// <summary>
    /// Delete a job by ID
    /// </summary>
    bool DeleteJob(string jobId);
}
