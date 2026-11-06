using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppBackendBasic.Services.Captions;

public interface ICaptionProjectService
{
    Task<CaptionProjectDto> CreateFromUploadAsync(int userId, Stream file, string fileName, string? contentType, long length,
        string theme, string fontId, string speechLanguage, CancellationToken ct = default);
    /// <summary>The user's project library, most recently edited first.</summary>
    Task<CaptionProjectListDto> ListAsync(int userId, string? cursor, int limit, CancellationToken ct = default);
    Task<CaptionProjectDto> GetAsync(int userId, int projectId, CancellationToken ct = default);
    Task EnsureOwnedAsync(int userId, int projectId, CancellationToken ct = default);
    /// <summary>Saves the editable document; 409 with code REVISION_CONFLICT when edit.BaseRevision is out of date.</summary>
    Task<CaptionSaveResultDto> SaveAsync(int userId, int projectId, CaptionProjectEditDto edit, CancellationToken ct = default);
    Task SaveEditorStateAsync(int userId, int projectId, CaptionEditorStateDto state, CancellationToken ct = default);
    /// <summary>Deletes the project and its stored files; 409 while a job is running on it.</summary>
    Task DeleteAsync(int userId, int projectId, CancellationToken ct = default);
    Task<List<CaptionProjectVersionDto>> ListVersionsAsync(int userId, int projectId, CancellationToken ct = default);
    /// <summary>Replaces the document with a restore point (the current document is kept as a new restore point first).</summary>
    Task<CaptionProjectDto> RestoreVersionAsync(int userId, int projectId, int versionId, CancellationToken ct = default);
    Task<string> ExportSrtAsync(int userId, int projectId, CancellationToken ct = default);
    Task<CaptionProjectDto> RetranscribeAsync(int userId, int projectId, string speechLanguage, CancellationToken ct = default);
    Task<CaptionEmphasisResultDto> ReassignEmphasisAsync(int userId, int projectId, CancellationToken ct = default);
    /// <summary>Marks the project as rendering and queues the export; poll the project status for the result.</summary>
    Task<CaptionProjectDto> RequestRenderAsync(int userId, int projectId, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)> OpenMediaAsync(int userId, int projectId, CancellationToken ct = default);
    Task<Stream> OpenRenderAsync(int userId, int projectId, CancellationToken ct = default);
    Task<Stream> OpenThumbnailAsync(int userId, int projectId, CancellationToken ct = default);
}

public class CaptionProjectService : ICaptionProjectService
{
    private static readonly HashSet<string> Positions = new() { "top", "middle", "bottom" };

    private const int MaxPageSize = 50;

    private readonly ICaptionProjectRepository _projects;
    private readonly ICaptionStorage _storage;
    private readonly IFfmpegService _ffmpeg;
    private readonly ICaptionCatalogService _catalog;
    private readonly ICaptionEntitlementService _entitlement;
    private readonly ICaptionJobQueue _queue;
    private readonly IHinglishService _hinglish;
    private readonly ICaptionMediaTokenService _mediaTokens;
    private readonly CaptionOptions _options;
    private readonly ILogger<CaptionProjectService> _logger;

    public CaptionProjectService(ICaptionProjectRepository projects, ICaptionStorage storage, IFfmpegService ffmpeg, ICaptionCatalogService catalog,
        ICaptionEntitlementService entitlement, ICaptionJobQueue queue, IHinglishService hinglish, ICaptionMediaTokenService mediaTokens,
        IOptions<CaptionOptions> options, ILogger<CaptionProjectService> logger)
    {
        _projects = projects; _storage = storage; _ffmpeg = ffmpeg; _catalog = catalog; _entitlement = entitlement; _queue = queue;
        _hinglish = hinglish; _mediaTokens = mediaTokens; _options = options.Value; _logger = logger;
    }

    public static CaptionProjectDto ToDto(CaptionProject p) => new()
    {
        ProjectId = p.Id, Name = p.Name, Duration = p.Duration, Width = p.Width, Height = p.Height, SizeBytes = p.SizeBytes,
        Theme = p.Theme, FontId = p.FontId, WordsPerCaption = p.WordsPerCaption, CaptionLines = p.CaptionLines,
        CaptionPosition = p.CaptionPosition, AudioVolume = p.AudioVolume, AudioMuted = p.AudioMuted, Brightness = p.Brightness,
        Contrast = p.Contrast, Saturation = p.Saturation, RenderQuality = p.RenderQuality, Variant = p.Variant,
        ScriptMode = p.ScriptMode, OutlineEnabled = p.OutlineEnabled, OutlineWidth = p.OutlineWidth, OutlineColor = p.OutlineColor,
        ShadowEnabled = p.ShadowEnabled, ShadowAngle = p.ShadowAngle, ShadowDistance = p.ShadowDistance, ShadowBlur = p.ShadowBlur,
        ShadowOpacity = p.ShadowOpacity, ShadowColor = p.ShadowColor, GlowEnabled = p.GlowEnabled, GlowColor = p.GlowColor,
        GlowBlur = p.GlowBlur, GlowOpacity = p.GlowOpacity, BackgroundEnabled = p.BackgroundEnabled, BackgroundColor = p.BackgroundColor,
        BackgroundOpacity = p.BackgroundOpacity, Bold = p.Bold, Italic = p.Italic,
        Elements = CaptionJson.ParseList<CaptionElementDto>(p.ElementsJson),
        SfxMarkers = CaptionJson.ParseList<CaptionSfxMarkerDto>(p.SfxMarkersJson),
        Words = CaptionJson.ParseList<CaptionWordDto>(p.WordsJson),
        SourceTranscript = p.SourceTranscript, SpeechLanguage = p.SpeechLanguage, TranscriptionMode = p.TranscriptionMode,
        TranscriptionQuality = string.IsNullOrWhiteSpace(p.TranscriptionQualityJson) ? null : JsonDocument.Parse(p.TranscriptionQualityJson).RootElement.Clone(),
        Status = p.Status, Progress = p.Progress, ProcessingLabel = p.ProcessingLabel, ErrorMessage = p.ErrorMessage,
        CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt, LastEditedAt = p.LastEditedAt, Revision = p.Revision, RenderedRevision = p.RenderedRevision,
    };

    /// <summary>The editable document of a project: what autosave writes and what a restore point stores.</summary>
    private static CaptionProjectEditDto ToEditDto(CaptionProject p) => new()
    {
        Theme = p.Theme, FontId = p.FontId, WordsPerCaption = p.WordsPerCaption, CaptionLines = p.CaptionLines, CaptionPosition = p.CaptionPosition,
        AudioVolume = p.AudioVolume, AudioMuted = p.AudioMuted, Brightness = p.Brightness, Contrast = p.Contrast, Saturation = p.Saturation,
        RenderQuality = p.RenderQuality, Variant = p.Variant, ScriptMode = p.ScriptMode,
        Words = CaptionJson.ParseList<CaptionWordDto>(p.WordsJson), Elements = CaptionJson.ParseList<CaptionElementDto>(p.ElementsJson),
        SfxMarkers = CaptionJson.ParseList<CaptionSfxMarkerDto>(p.SfxMarkersJson),
        OutlineEnabled = p.OutlineEnabled, OutlineWidth = p.OutlineWidth, OutlineColor = p.OutlineColor,
        ShadowEnabled = p.ShadowEnabled, ShadowAngle = p.ShadowAngle, ShadowDistance = p.ShadowDistance, ShadowBlur = p.ShadowBlur,
        ShadowOpacity = p.ShadowOpacity, ShadowColor = p.ShadowColor, GlowEnabled = p.GlowEnabled, GlowColor = p.GlowColor,
        GlowBlur = p.GlowBlur, GlowOpacity = p.GlowOpacity, BackgroundEnabled = p.BackgroundEnabled,
        BackgroundColor = p.BackgroundColor, BackgroundOpacity = p.BackgroundOpacity, Bold = p.Bold, Italic = p.Italic,
    };

    private static void ApplyEdit(CaptionProject p, CaptionProjectEditDto edit)
    {
        p.Theme = edit.Theme; p.FontId = edit.FontId; p.WordsPerCaption = edit.WordsPerCaption; p.CaptionLines = edit.CaptionLines;
        p.CaptionPosition = edit.CaptionPosition; p.AudioVolume = edit.AudioVolume; p.AudioMuted = edit.AudioMuted;
        p.Brightness = edit.Brightness; p.Contrast = edit.Contrast; p.Saturation = edit.Saturation; p.RenderQuality = edit.RenderQuality;
        p.WordsJson = CaptionJson.Serialize(edit.Words); p.ElementsJson = CaptionJson.Serialize(edit.Elements);
        p.SfxMarkersJson = CaptionJson.Serialize(edit.SfxMarkers); p.Variant = edit.Variant;
        p.ScriptMode = edit.ScriptMode; p.OutlineEnabled = edit.OutlineEnabled; p.OutlineWidth = edit.OutlineWidth; p.OutlineColor = edit.OutlineColor;
        p.ShadowEnabled = edit.ShadowEnabled; p.ShadowAngle = edit.ShadowAngle; p.ShadowDistance = edit.ShadowDistance; p.ShadowBlur = edit.ShadowBlur;
        p.ShadowOpacity = edit.ShadowOpacity; p.ShadowColor = edit.ShadowColor; p.GlowEnabled = edit.GlowEnabled; p.GlowColor = edit.GlowColor;
        p.GlowBlur = edit.GlowBlur; p.GlowOpacity = edit.GlowOpacity; p.BackgroundEnabled = edit.BackgroundEnabled;
        p.BackgroundColor = edit.BackgroundColor; p.BackgroundOpacity = edit.BackgroundOpacity; p.Bold = edit.Bold; p.Italic = edit.Italic;
    }

    private async Task<CaptionProject> OwnedAsync(int userId, int projectId, bool track, CancellationToken ct) =>
        await _projects.GetOwnedAsync(userId, projectId, track, ct) ?? throw new CaptionServiceException(404, "Project not found");

    private static bool IsGenerating(CaptionProject p) => p.Status is CaptionProjectStatus.Queued or CaptionProjectStatus.Processing;

    /// <summary>A content change: moves the revision (making any stored export out of date) and the library sort key. Job heartbeats don't come through here.</summary>
    private static void MarkEdited(CaptionProject p)
    {
        var now = DateTime.UtcNow;
        p.Revision++; p.LastEditedAt = now; p.UpdatedAt = now;
    }

    private static CaptionServiceException RevisionConflict() =>
        new(409, "This project was changed in another tab or device. Reload to get the latest version.", CaptionServiceException.RevisionConflict);

    private async Task CommitAsync(CancellationToken ct)
    {
        try { await _projects.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw RevisionConflict(); }
    }

    /// <summary>Tracks a restore point of the current document; it commits atomically with the change it precedes. Empty documents aren't worth keeping.</summary>
    private void CaptureVersion(CaptionProject p, string reason)
    {
        var document = ToEditDto(p);
        if (document.Words.Count == 0) return;
        _projects.AddVersion(new CaptionProjectVersion { ProjectId = p.Id, ProjectRevision = p.Revision, Reason = reason, DocumentJson = CaptionJson.Serialize(document) });
    }

    private Task TrimVersionsAsync(int projectId, CancellationToken ct) => _projects.TrimVersionsAsync(projectId, _options.MaxVersionsPerProject, ct);

    private async Task<Stream> OpenStoredAsync(string path, string missingMessage, CancellationToken ct)
    {
        try { return (await _storage.OpenAsync(path, ct)).Stream; }
        catch (FileNotFoundException) { throw new CaptionServiceException(410, missingMessage); }
    }

    public async Task<CaptionProjectDto> CreateFromUploadAsync(int userId, Stream file, string fileName, string? contentType, long length,
        string theme, string fontId, string speechLanguage, CancellationToken ct = default)
    {
        var catalog = await _catalog.GetCatalogAsync(ct);
        if (catalog.Themes.All(t => t.Id != theme)) throw new CaptionServiceException(400, "Unknown caption theme");
        if (catalog.Fonts.All(f => f.Id != fontId)) throw new CaptionServiceException(400, "Unknown caption font");
        if (speechLanguage != "hi" && speechLanguage != "auto") throw new CaptionServiceException(400, "Speech language must be Hindi/Hinglish or Auto");

        var limits = catalog.Limits;
        var safeName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "video.mp4" : fileName);
        if (safeName.Length > 255) safeName = safeName[^255..];
        var suffix = Path.GetExtension(safeName).ToLowerInvariant();
        if (!limits.AllowedExtensions.Contains(suffix))
            throw new CaptionServiceException(415, "Upload an " + string.Join(" or ", limits.AllowedExtensions.Select(e => e.TrimStart('.').ToUpperInvariant())) + " video");
        long maxBytes = (long)limits.MaxUploadMb * 1024 * 1024;
        if (length > maxBytes) throw new CaptionServiceException(413, $"Video must be smaller than {limits.MaxUploadMb} MB");

        var tempDir = Path.Combine(Path.GetTempPath(), "captain-upload-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var tempFile = Path.Combine(tempDir, "source" + suffix);
            long written = 0;
            await using (var dst = File.Create(tempFile))
            {
                var buffer = new byte[81920]; int read;
                while ((read = await file.ReadAsync(buffer, ct)) > 0)
                {
                    written += read;
                    if (written > maxBytes) throw new CaptionServiceException(413, $"Video must be smaller than {limits.MaxUploadMb} MB");
                    await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                }
            }

            VideoProbe probe;
            try { probe = await _ffmpeg.ProbeAsync(tempFile, ct); }
            // Only "ffmpeg found no duration" blames the file. Anything else (e.g. ffmpeg missing) is a server issue, reported by the controller.
            catch (InvalidDataException) { throw new CaptionServiceException(422, "This video could not be read"); }
            if (probe.Duration > limits.AbsoluteMaxSeconds) throw new CaptionServiceException(422, $"Videos can be at most {limits.AbsoluteMaxSeconds} seconds");

            var credit = await _entitlement.DetermineCreditAsync(userId, probe.Duration, ct);
            if (credit == null) throw new CaptionServiceException(402, await _entitlement.DescribeDenialAsync(probe.Duration, ct));

            var storagePath = $"captain-title/uploads/{userId}/{Guid.NewGuid():N}{suffix}";
            try { await _storage.PutFileAsync(storagePath, tempFile, string.IsNullOrWhiteSpace(contentType) ? "video/mp4" : contentType, ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Upload could not be stored at {Path}", storagePath);
                throw new CaptionServiceException(503, CaptionErrors.ServerIssue);
            }

            var project = new CaptionProject
            {
                UserId = userId, Name = safeName, Duration = probe.Duration, Width = probe.Width, Height = probe.Height, SizeBytes = written,
                Theme = theme, FontId = fontId, SpeechLanguage = speechLanguage, SourcePath = storagePath,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "video/mp4" : contentType, CreditType = credit,
                Status = CaptionProjectStatus.Queued, Progress = 5, ProcessingLabel = "Upload complete",
            };
            _projects.Add(project);
            await _projects.SaveChangesAsync(ct);
            _queue.Enqueue(project.Id, consumeCredit: true);
            return ToDto(project);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
        }
    }

    public async Task<CaptionProjectListDto> ListAsync(int userId, string? cursor, int limit, CancellationToken ct = default)
    {
        var (beforeEditedAt, beforeId) = ParseCursor(cursor);
        int take = Math.Clamp(limit, 1, MaxPageSize);
        var rows = await _projects.ListAsync(userId, beforeEditedAt, beforeId, take + 1, ct);
        var page = rows.Take(take).ToList();
        return new CaptionProjectListDto
        {
            Items = page.Select(r => new CaptionProjectSummaryDto
            {
                ProjectId = r.Id, Name = r.Name, Duration = r.Duration, Width = r.Width, Height = r.Height, SizeBytes = r.SizeBytes, Theme = r.Theme,
                Status = r.Status, Progress = r.Progress, ProcessingLabel = r.ProcessingLabel, ErrorMessage = r.ErrorMessage,
                ThumbnailUrl = r.ThumbnailPath == null ? null : $"/api/captions/thumbnail/{r.Id}?t={Uri.EscapeDataString(_mediaTokens.Create(r.Id, userId))}",
                ExportCurrent = r.RenderPath != null && r.RenderedRevision == r.Revision, CreatedAt = r.CreatedAt, LastEditedAt = r.LastEditedAt,
            }).ToList(),
            NextCursor = rows.Count > take ? $"{page[^1].LastEditedAt.Ticks}_{page[^1].Id}" : null,
        };
    }

    private static (DateTime? EditedAt, int? Id) ParseCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return (null, null);
        var parts = cursor.Split('_');
        if (parts.Length != 2 || !long.TryParse(parts[0], out var ticks) || !int.TryParse(parts[1], out var id)
            || ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
            throw new CaptionServiceException(400, "Invalid cursor");
        return (new DateTime(ticks, DateTimeKind.Utc), id);
    }

    public async Task<CaptionProjectDto> GetAsync(int userId, int projectId, CancellationToken ct = default) =>
        await WithEditorStateAsync(await OwnedAsync(userId, projectId, false, ct), ct);

    private async Task<CaptionProjectDto> WithEditorStateAsync(CaptionProject p, CancellationToken ct)
    {
        var dto = ToDto(p);
        var json = await _projects.GetEditorStateJsonAsync(p.Id, ct);
        if (json != null) dto.EditorState = JsonSerializer.Deserialize<CaptionEditorStateDto>(json, CaptionJson.Options);
        return dto;
    }

    public async Task EnsureOwnedAsync(int userId, int projectId, CancellationToken ct = default)
    {
        if (!await _projects.IsOwnedAsync(userId, projectId, ct)) throw new CaptionServiceException(404, "Project not found");
    }

    public async Task<CaptionSaveResultDto> SaveAsync(int userId, int projectId, CaptionProjectEditDto edit, CancellationToken ct = default)
    {
        if (!Positions.Contains(edit.CaptionPosition)) throw new CaptionServiceException(400, "Unknown caption position");

        var p = await OwnedAsync(userId, projectId, true, ct);
        // Saving now would be overwritten by (or overwrite) the transcription job's result.
        if (IsGenerating(p)) throw new CaptionServiceException(409, "Captions are being generated. Please wait until they are ready.");
        if (p.Status == CaptionProjectStatus.Failed) throw new CaptionServiceException(409, "Caption generation failed. Use Re-listen before editing.");
        if (edit.BaseRevision != null && edit.BaseRevision != p.Revision) throw RevisionConflict();

        // A template or font retired after the project was made stays valid on it; otherwise every autosave of an older project would fail.
        var catalog = await _catalog.GetCatalogAsync(ct);
        if (edit.Theme != p.Theme && catalog.Themes.All(t => t.Id != edit.Theme)) throw new CaptionServiceException(400, "Unknown theme");
        if (edit.FontId != p.FontId && catalog.Fonts.All(f => f.Id != edit.FontId)) throw new CaptionServiceException(400, "Unknown caption font");

        var latest = await _projects.GetLatestVersionTimeAsync(p.Id, ct);
        bool capture = latest == null || DateTime.UtcNow - latest >= TimeSpan.FromMinutes(Math.Max(1, _options.VersionIntervalMinutes));
        if (capture) CaptureVersion(p, CaptionVersionReason.Autosave);

        ApplyEdit(p, edit);
        MarkEdited(p);
        await CommitAsync(ct);
        if (capture) await TrimVersionsAsync(p.Id, ct);
        return new CaptionSaveResultDto { Revision = p.Revision, LastEditedAt = p.LastEditedAt };
    }

    public async Task SaveEditorStateAsync(int userId, int projectId, CaptionEditorStateDto state, CancellationToken ct = default)
    {
        if (!await _projects.UpsertEditorStateAsync(userId, projectId, CaptionJson.Serialize(state), ct))
            throw new CaptionServiceException(404, "Project not found");
    }

    public async Task DeleteAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var files = await _projects.GetFilesAsync(userId, projectId, ct) ?? throw new CaptionServiceException(404, "Project not found");
        if (!await _projects.DeleteIfIdleAsync(userId, projectId, ct))
            throw new CaptionServiceException(409, "This project is still processing. Please wait until it finishes before deleting it.");

        // The row is gone and the user asked for it, so a file that can't be removed is logged rather than failing the request.
        foreach (var path in new[] { files.SourcePath, files.PreviewPath, files.RenderPath, files.ThumbnailPath })
        {
            if (string.IsNullOrEmpty(path)) continue;
            try { await _storage.DeleteAsync(path, CancellationToken.None); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete stored file {Path} of deleted project {Id}", path, projectId); }
        }
    }

    public async Task<List<CaptionProjectVersionDto>> ListVersionsAsync(int userId, int projectId, CancellationToken ct = default)
    {
        await EnsureOwnedAsync(userId, projectId, ct);
        return (await _projects.ListVersionsAsync(projectId, ct)).Select(v => new CaptionProjectVersionDto
        {
            VersionId = v.Id, Reason = v.Reason, ProjectRevision = v.ProjectRevision, CreatedAt = v.CreatedAt,
        }).ToList();
    }

    public async Task<CaptionProjectDto> RestoreVersionAsync(int userId, int projectId, int versionId, CancellationToken ct = default)
    {
        var p = await OwnedAsync(userId, projectId, true, ct);
        if (IsGenerating(p)) throw new CaptionServiceException(409, "Captions are being generated. Please wait until they are ready.");
        var json = await _projects.GetVersionDocumentAsync(projectId, versionId, ct) ?? throw new CaptionServiceException(404, "Version not found");
        var document = JsonSerializer.Deserialize<CaptionProjectEditDto>(json, CaptionJson.Options) ?? throw new CaptionServiceException(404, "Version not found");

        CaptureVersion(p, CaptionVersionReason.BeforeRestore);
        ApplyEdit(p, document);
        MarkEdited(p);
        // Restoring is also the way back from a re-listen that failed after wiping the words.
        if (p.Status == CaptionProjectStatus.Failed)
        {
            p.Status = CaptionProjectStatus.Ready; p.Progress = 100; p.ProcessingLabel = "Captions ready"; p.ErrorMessage = null;
        }
        await CommitAsync(ct);
        await TrimVersionsAsync(p.Id, ct);
        return await WithEditorStateAsync(p, ct);
    }

    public async Task<string> ExportSrtAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var p = await OwnedAsync(userId, projectId, false, ct);
        var words = CaptionJson.ParseList<CaptionWordDto>(p.WordsJson);
        if (words.Count == 0) throw new CaptionServiceException(422, "No captions to export yet");
        return SrtWriter.Build(words, p.ScriptMode);
    }

    public async Task<CaptionProjectDto> RetranscribeAsync(int userId, int projectId, string speechLanguage, CancellationToken ct = default)
    {
        if (speechLanguage != "hi" && speechLanguage != "auto") throw new CaptionServiceException(400, "Speech language must be Hindi only or Adaptive Hinglish");
        var p = await OwnedAsync(userId, projectId, true, ct);
        if (IsGenerating(p) || p.Status == CaptionProjectStatus.Rendering)
            throw new CaptionServiceException(409, "This project is still processing. Please wait until it finishes.");
        // Credits are charged on the first successful run. A retry after a failed first run is that first run, so it has to be affordable (and pays then).
        if (p.CreditConsumedAt == null)
        {
            var credit = await _entitlement.DetermineCreditAsync(userId, p.Duration, ct);
            if (credit == null) throw new CaptionServiceException(402, await _entitlement.DescribeDenialAsync(p.Duration, ct));
            p.CreditType = credit;
        }

        CaptureVersion(p, CaptionVersionReason.BeforeRelisten);
        p.SpeechLanguage = speechLanguage; p.Status = CaptionProjectStatus.Queued; p.Progress = 5; p.ProcessingLabel = "Re-listen queued";
        p.ErrorMessage = null; p.WordsJson = "[]"; p.SourceTranscript = "";
        MarkEdited(p);
        await CommitAsync(ct);
        await TrimVersionsAsync(p.Id, ct);
        _queue.Enqueue(p.Id, consumeCredit: true);
        return ToDto(p);
    }

    public async Task<CaptionEmphasisResultDto> ReassignEmphasisAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var p = await OwnedAsync(userId, projectId, true, ct);
        var words = CaptionJson.ParseList<CaptionWordDto>(p.WordsJson);
        if (words.Count == 0) throw new CaptionServiceException(422, "No captions to re-tag yet");
        try { words = await _hinglish.ReassignEmphasisAsync(words, p.Name, ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "AI emphasis re-assignment failed for project {Id}", projectId);
            throw new CaptionServiceException(502, CaptionErrors.ServerIssue);
        }

        // Re-bake colour/size from the (possibly different) tier, exactly like a template switch.
        var theme = (await _catalog.GetStyleSetAsync(ct)).Theme(p.Theme);
        foreach (var w in words) { var (c, s) = theme.BakeFor(w.Emphasis, w.Important); w.Color = c; w.FontSize = s; }
        CaptureVersion(p, CaptionVersionReason.BeforeAiEmphasis);
        p.WordsJson = CaptionJson.Serialize(words);
        MarkEdited(p);
        await CommitAsync(ct);
        await TrimVersionsAsync(p.Id, ct);
        return new CaptionEmphasisResultDto { Words = words, Revision = p.Revision };
    }

    public async Task<CaptionProjectDto> RequestRenderAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var p = await OwnedAsync(userId, projectId, true, ct);
        if (p.Status == CaptionProjectStatus.Queued || p.Status == CaptionProjectStatus.Processing)
            throw new CaptionServiceException(409, "Captions are still being generated. Please wait until they are ready.");
        if (p.Status == CaptionProjectStatus.Failed)
            throw new CaptionServiceException(409, "Caption generation failed. Use Re-listen before exporting.");
        if (p.Status == CaptionProjectStatus.Rendering) return ToDto(p); // already running

        p.Status = CaptionProjectStatus.Rendering; p.Progress = 5; p.ProcessingLabel = "Rendering video";
        p.ErrorMessage = null; p.UpdatedAt = DateTime.UtcNow;
        await CommitAsync(ct);
        _queue.EnqueueRender(p.Id);
        return ToDto(p);
    }

    public async Task<(Stream Stream, string ContentType)> OpenMediaAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var p = await OwnedAsync(userId, projectId, false, ct);
        try
        {
            var (stream, type) = await _storage.OpenAsync(p.PreviewPath ?? p.SourcePath, ct);
            return (stream, p.PreviewContentType ?? type);
        }
        catch (FileNotFoundException) { throw new CaptionServiceException(410, "The video file for this project is no longer available"); }
    }

    public async Task<Stream> OpenRenderAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var files = await _projects.GetFilesAsync(userId, projectId, ct) ?? throw new CaptionServiceException(404, "Project not found");
        if (string.IsNullOrEmpty(files.RenderPath)) throw new CaptionServiceException(404, "Rendered video not found");
        return await OpenStoredAsync(files.RenderPath, "The exported video is no longer available. Please export it again.", ct);
    }

    public async Task<Stream> OpenThumbnailAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var files = await _projects.GetFilesAsync(userId, projectId, ct) ?? throw new CaptionServiceException(404, "Project not found");
        if (string.IsNullOrEmpty(files.ThumbnailPath)) throw new CaptionServiceException(404, "Thumbnail not found");
        return await OpenStoredAsync(files.ThumbnailPath, "Thumbnail not found", ct);
    }
}
