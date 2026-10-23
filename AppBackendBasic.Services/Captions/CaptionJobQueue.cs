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

public enum CaptionJobKind { Process, Render }

public readonly record struct CaptionJob(int ProjectId, CaptionJobKind Kind, bool ConsumeCredit);

public interface ICaptionJobQueue
{
    /// <summary>Queues a transcription run. A project already queued/running is not queued twice (old active_jobs set).</summary>
    bool Enqueue(int projectId, bool consumeCredit);

    /// <summary>Queues an export render (runs in the background; the editor polls the project status).</summary>
    bool EnqueueRender(int projectId);
}

/// <summary>
/// In-process background queue replacing the old asyncio.create_task + resume_pending_projects. CPU-heavy work (ffmpeg) never
/// runs inside an HTTP request, so a slow render cannot hit proxy timeouts. Safe with several API instances: a job is CLAIMED with a
/// compare-and-set UPDATE in the database, so two instances never run the same project; stale work left by a crashed instance is
/// re-queued on startup. For heavy traffic run a dedicated worker instance (set Captions:MaxConcurrentJobs, scale it separately).
/// </summary>
public class CaptionJobWorker : BackgroundService, ICaptionJobQueue
{
    /// <summary>A job with no heartbeat (UpdatedAt) for this long is considered abandoned by a crashed instance.</summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(15);

    private readonly Channel<CaptionJob> _channel = Channel.CreateUnbounded<CaptionJob>();
    private readonly ConcurrentDictionary<int, byte> _active = new();
    private readonly IServiceScopeFactory _scopes;
    private readonly CaptionOptions _options;
    private readonly ILogger<CaptionJobWorker> _logger;

    public CaptionJobWorker(IServiceScopeFactory scopes, IOptions<CaptionOptions> options, ILogger<CaptionJobWorker> logger)
    {
        _scopes = scopes; _options = options.Value; _logger = logger;
    }

    public bool Enqueue(int projectId, bool consumeCredit) => Write(new CaptionJob(projectId, CaptionJobKind.Process, consumeCredit));

    public bool EnqueueRender(int projectId) => Write(new CaptionJob(projectId, CaptionJobKind.Render, false));

    private bool Write(CaptionJob job)
    {
        if (!_active.TryAdd(job.ProjectId, 0)) return false;
        if (_channel.Writer.TryWrite(job)) return true;
        _active.TryRemove(job.ProjectId, out _);
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
            var stale = DateTime.UtcNow - StaleAfter;

            // Work abandoned by a crashed instance: transcription goes back to the queue; an interrupted render returns to "ready".
            await db.CaptionProjects.Where(p => p.Status == CaptionProjectStatus.Processing && (p.UpdatedAt == null || p.UpdatedAt < stale))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, CaptionProjectStatus.Queued), ct);
            await db.CaptionProjects.Where(p => p.Status == CaptionProjectStatus.Rendering && (p.UpdatedAt == null || p.UpdatedAt < stale))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, CaptionProjectStatus.Ready)
                    .SetProperty(p => p.ProcessingLabel, "Captions ready")
                    .SetProperty(p => p.ErrorMessage, "The export was interrupted. Please export again."), ct);

            var pending = await db.CaptionProjects.AsNoTracking()
                .Where(p => p.Status == CaptionProjectStatus.Queued)
                .OrderBy(p => p.Id).Take(100).Select(p => p.Id).ToListAsync(ct);
            foreach (var id in pending) Enqueue(id, consumeCredit: true);
            if (pending.Count > 0) _logger.LogInformation("Resumed {Count} queued caption project(s)", pending.Count);
        }
        catch (Exception ex)
        {
            // Never block startup: the DB may not be set up yet (run the SQL in Database_Migrations/ manually).
            _logger.LogWarning(ex, "Resuming pending caption projects skipped");
        }
    }

    private async Task MarkFailedAsync(CaptionJob job)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (job.Kind == CaptionJobKind.Render)
                await db.CaptionProjects.Where(p => p.Id == job.ProjectId).ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, CaptionProjectStatus.Ready).SetProperty(p => p.ProcessingLabel, "Captions ready")
                    .SetProperty(p => p.ErrorMessage, "Video rendering could not start. Please try again.")
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
            else
                await db.CaptionProjects.Where(p => p.Id == job.ProjectId).ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, CaptionProjectStatus.Failed).SetProperty(p => p.Progress, 100)
                    .SetProperty(p => p.ProcessingLabel, "Processing failed")
                    .SetProperty(p => p.ErrorMessage, "Caption processing could not start. Please try Re-listen or contact support.")
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
        }
        catch (Exception ex) { _logger.LogError(ex, "Could not mark project {Id} as failed", job.ProjectId); }
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
                    if (job.Kind == CaptionJobKind.Render)
                        await scope.ServiceProvider.GetRequiredService<ICaptionRenderService>().RunAsync(job.ProjectId, ct);
                    else
                        await scope.ServiceProvider.GetRequiredService<ICaptionJobRunner>().RunAsync(job.ProjectId, job.ConsumeCredit, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    // The runner could not even start (e.g. a dependency failed to resolve): never leave the project stuck.
                    _logger.LogError(ex, "Unhandled caption job error for project {Id}", job.ProjectId);
                    await MarkFailedAsync(job);
                }
                finally { _active.TryRemove(job.ProjectId, out _); }
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
    }
}
