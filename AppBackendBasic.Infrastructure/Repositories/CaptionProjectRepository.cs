using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.Infrastructure.Repositories;

public class CaptionProjectRepository : ICaptionProjectRepository
{
    private readonly ApplicationDbContext _context;

    public CaptionProjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Add(CaptionProject project) => _context.CaptionProjects.Add(project);

    public void AddVersion(CaptionProjectVersion version) => _context.CaptionProjectVersions.Add(version);

    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

    public Task<CaptionProject?> GetOwnedAsync(int userId, int projectId, bool track, CancellationToken ct = default)
    {
        var query = track ? _context.CaptionProjects : _context.CaptionProjects.AsNoTracking();
        return query.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct);
    }

    public Task<bool> IsOwnedAsync(int userId, int projectId, CancellationToken ct = default) =>
        _context.CaptionProjects.AnyAsync(p => p.Id == projectId && p.UserId == userId, ct);

    public Task<List<CaptionProjectListRow>> ListAsync(int userId, DateTime? beforeEditedAt, int? beforeId, int take, CancellationToken ct = default)
    {
        var query = _context.CaptionProjects.AsNoTracking().Where(p => p.UserId == userId);
        if (beforeEditedAt != null && beforeId != null)
            query = query.Where(p => p.LastEditedAt < beforeEditedAt || (p.LastEditedAt == beforeEditedAt && p.Id < beforeId));
        return query.OrderByDescending(p => p.LastEditedAt).ThenByDescending(p => p.Id).Take(take)
            .Select(p => new CaptionProjectListRow(p.Id, p.Name, p.Duration, p.Width, p.Height, p.SizeBytes, p.Theme, p.Status, p.Progress,
                p.ProcessingLabel, p.ErrorMessage, p.ThumbnailPath, p.Revision, p.RenderedRevision, p.RenderPath, p.CreatedAt, p.LastEditedAt))
            .ToListAsync(ct);
    }

    public Task<CaptionProjectFiles?> GetFilesAsync(int userId, int projectId, CancellationToken ct = default) =>
        _context.CaptionProjects.AsNoTracking().Where(p => p.Id == projectId && p.UserId == userId)
            .Select(p => new CaptionProjectFiles(p.SourcePath, p.PreviewPath, p.RenderPath, p.ThumbnailPath))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> DeleteIfIdleAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var deleted = await _context.CaptionProjects
            .Where(p => p.Id == projectId && p.UserId == userId && p.Status != CaptionProjectStatus.Queued
                && p.Status != CaptionProjectStatus.Processing && p.Status != CaptionProjectStatus.Rendering)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public Task<string?> GetEditorStateJsonAsync(int projectId, CancellationToken ct = default) =>
        _context.CaptionProjectEditorStates.AsNoTracking().Where(s => s.ProjectId == projectId).Select(s => s.StateJson).FirstOrDefaultAsync(ct);

    public async Task<bool> UpsertEditorStateAsync(int userId, int projectId, string stateJson, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "CaptionProjectEditorStates" ("ProjectId", "StateJson", "UpdatedAt")
            SELECT p."Id", {stateJson}::jsonb, {now} FROM "CaptionProjects" p WHERE p."Id" = {projectId} AND p."UserId" = {userId}
            ON CONFLICT ("ProjectId") DO UPDATE SET "StateJson" = EXCLUDED."StateJson", "UpdatedAt" = EXCLUDED."UpdatedAt"
            """, ct);
        return rows > 0;
    }

    public Task<DateTime?> GetLatestVersionTimeAsync(int projectId, CancellationToken ct = default) =>
        _context.CaptionProjectVersions.AsNoTracking().Where(v => v.ProjectId == projectId)
            .OrderByDescending(v => v.Id).Select(v => (DateTime?)v.CreatedAt).FirstOrDefaultAsync(ct);

    // Projected field by field so the documents stay in the database until one is actually restored.
    public Task<List<CaptionProjectVersion>> ListVersionsAsync(int projectId, CancellationToken ct = default) =>
        _context.CaptionProjectVersions.AsNoTracking().Where(v => v.ProjectId == projectId).OrderByDescending(v => v.Id)
            .Select(v => new CaptionProjectVersion { Id = v.Id, ProjectId = v.ProjectId, ProjectRevision = v.ProjectRevision, Reason = v.Reason, CreatedAt = v.CreatedAt })
            .ToListAsync(ct);

    public Task<string?> GetVersionDocumentAsync(int projectId, int versionId, CancellationToken ct = default) =>
        _context.CaptionProjectVersions.AsNoTracking().Where(v => v.Id == versionId && v.ProjectId == projectId)
            .Select(v => v.DocumentJson).FirstOrDefaultAsync(ct);

    public Task TrimVersionsAsync(int projectId, int keep, CancellationToken ct = default) =>
        _context.CaptionProjectVersions.Where(v => v.ProjectId == projectId).OrderByDescending(v => v.Id).Skip(Math.Max(1, keep)).ExecuteDeleteAsync(ct);
}
