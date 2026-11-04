using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

/// <summary>A library row: deliberately excludes the jsonb documents so listing never loads them.</summary>
public sealed record CaptionProjectListRow(int Id, string Name, double Duration, int? Width, int? Height, long SizeBytes, string Theme,
    string Status, int Progress, string ProcessingLabel, string? ErrorMessage, string? ThumbnailPath, int Revision, int? RenderedRevision,
    string? RenderPath, DateTime CreatedAt, DateTime LastEditedAt);

public sealed record CaptionProjectFiles(string SourcePath, string? PreviewPath, string? RenderPath, string? ThumbnailPath);

/// <summary>Persistence for caption projects. Every read and write here is scoped to the owning user or to an already-owned project.</summary>
public interface ICaptionProjectRepository
{
    void Add(CaptionProject project);
    void AddVersion(CaptionProjectVersion version);
    /// <summary>Commits tracked changes. Throws DbUpdateConcurrencyException when the project's revision moved since it was loaded.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<CaptionProject?> GetOwnedAsync(int userId, int projectId, bool track, CancellationToken ct = default);
    Task<bool> IsOwnedAsync(int userId, int projectId, CancellationToken ct = default);
    /// <summary>Most recently edited first. Keyset paged: (beforeEditedAt, beforeId) is the last row of the previous page.</summary>
    Task<List<CaptionProjectListRow>> ListAsync(int userId, DateTime? beforeEditedAt, int? beforeId, int take, CancellationToken ct = default);
    Task<CaptionProjectFiles?> GetFilesAsync(int userId, int projectId, CancellationToken ct = default);
    /// <summary>Deletes unless a job is running on the project. Returns false when it was not deleted. Children cascade in the database.</summary>
    Task<bool> DeleteIfIdleAsync(int userId, int projectId, CancellationToken ct = default);

    Task<string?> GetEditorStateJsonAsync(int projectId, CancellationToken ct = default);
    /// <summary>Single-statement upsert that also proves ownership. Returns false when the user does not own the project.</summary>
    Task<bool> UpsertEditorStateAsync(int userId, int projectId, string stateJson, CancellationToken ct = default);

    Task<DateTime?> GetLatestVersionTimeAsync(int projectId, CancellationToken ct = default);
    Task<List<CaptionProjectVersion>> ListVersionsAsync(int projectId, CancellationToken ct = default);
    Task<string?> GetVersionDocumentAsync(int projectId, int versionId, CancellationToken ct = default);
    Task TrimVersionsAsync(int projectId, int keep, CancellationToken ct = default);
}
