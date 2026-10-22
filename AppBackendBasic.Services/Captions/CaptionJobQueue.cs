using System.Collections.Concurrent;
using System.Threading.Channels;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppBackendBasic.Services.Captions;

public readonly record struct CaptionJob(int ProjectId, bool ConsumeCredit);

public interface ICaptionJobQueue
{
    /// <summary>Queues a caption run. A project already queued/running is not queued twice (old active_jobs set).</summary>
    bool Enqueue(int projectId, bool consumeCredit);
}

/// <summary>
/// In-process background queue replacing the old asyncio.create_task + resume_pending_projects.
/// On startup projects left queued/processing (process restart) are re-enqueued, like the original.
/// </summary>
public class CaptionJobWorker : BackgroundService, ICaptionJobQueue
{
    private readonly Channel<CaptionJob> _channel = Channel.CreateUnbounded<CaptionJob>();
    private readonly ConcurrentDictionary<int, byte> _active = new();
    private readonly IServiceScopeFactory _scopes;
    private readonly CaptionOptions _options;
    private readonly ILogger<CaptionJobWorker> _logger;

    public CaptionJobWorker(IServiceScopeFactory scopes, IOptions<CaptionOptions> options, ILogger<CaptionJobWorker> logger)
    {
        _scopes = scopes; _options = options.Value; _logger = logger;
    }

    public bool Enqueue(int projectId, bool consumeCredit)
    {
        if (!_active.TryAdd(projectId, 0)) return false;
        if (_channel.Writer.TryWrite(new CaptionJob(projectId, consumeCredit))) return true;
        _active.TryRemove(projectId, out _);
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ResumePendingAsync(stoppingToken);
        var workers = Enumerable.Range(0, Math.Max(1, _options.MaxConcurrentJobs)).Select(_ => WorkLoopAsync(stoppingToken));
        await Task.WhenAll(workers);
    }

    private async Task ResumePendingAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pending = await db.CaptionProjects.AsNoTracking()
                .Where(p => p.Status == CaptionProjectStatus.Queued || p.Status == CaptionProjectStatus.Processing)
                .OrderBy(p => p.Id).Take(100).Select(p => p.Id).ToListAsync(ct);
            foreach (var id in pending) Enqueue(id, consumeCredit: true);
            if (pending.Count > 0) _logger.LogInformation("Resumed {Count} pending caption project(s)", pending.Count);
        }
        catch (Exception ex)
        {
            // Never block startup: the DB may not be set up yet (run Database_Migrations/22 then 21 manually).
            _logger.LogWarning(ex, "Resuming pending caption projects skipped");
        }
    }

    private async Task MarkFailedAsync(int projectId)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.CaptionProjects.Where(p => p.Id == projectId).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, CaptionProjectStatus.Failed).SetProperty(p => p.Progress, 100)
                .SetProperty(p => p.ProcessingLabel, "Processing failed")
                .SetProperty(p => p.ErrorMessage, "Caption processing could not start. Please try Re-listen or contact support.")
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
        }
        catch (Exception ex) { _logger.LogError(ex, "Could not mark project {Id} as failed", projectId); }
    }

    private async Task WorkLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var job in _channel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ICaptionJobRunner>().RunAsync(job.ProjectId, job.ConsumeCredit, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    // The runner could not even start (e.g. a dependency failed to resolve): never leave the project "queued" forever.
                    _logger.LogError(ex, "Unhandled caption job error for project {Id}", job.ProjectId);
                    await MarkFailedAsync(job.ProjectId);
                }
                finally { _active.TryRemove(job.ProjectId, out _); }
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
    }
}
