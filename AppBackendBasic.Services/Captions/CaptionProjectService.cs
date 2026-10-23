using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppBackendBasic.Services.Captions;

public interface ICaptionProjectService
{
    Task<CaptionProjectDto> CreateFromUploadAsync(int userId, Stream file, string fileName, string? contentType, long length,
        string theme, string fontId, string speechLanguage, CancellationToken ct = default);
    Task<CaptionProjectDto> GetAsync(int userId, int projectId, CancellationToken ct = default);
    Task SaveAsync(int userId, int projectId, CaptionProjectEditDto edit, CancellationToken ct = default);
    Task<string> ExportSrtAsync(int userId, int projectId, CancellationToken ct = default);
    Task<CaptionProjectDto> RetranscribeAsync(int userId, int projectId, string speechLanguage, CancellationToken ct = default);
    Task<List<CaptionWordDto>> ReassignEmphasisAsync(int userId, int projectId, CancellationToken ct = default);
    /// <summary>Marks the project as rendering and queues the export; poll the project status for the result.</summary>
    Task<CaptionProjectDto> RequestRenderAsync(int userId, int projectId, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)> OpenMediaAsync(int userId, int projectId, CancellationToken ct = default);
    Task<Stream> OpenRenderAsync(int userId, int projectId, CancellationToken ct = default);
}

public class CaptionProjectService : ICaptionProjectService
{
    private static readonly HashSet<string> Positions = new() { "top", "middle", "bottom" };

    private readonly ApplicationDbContext _db;
    private readonly ICaptionStorage _storage;
    private readonly IFfmpegService _ffmpeg;
    private readonly ICaptionCatalogService _catalog;
    private readonly ICaptionEntitlementService _entitlement;
    private readonly ICaptionJobQueue _queue;
    private readonly IHinglishService _hinglish;
    private readonly CaptionOptions _options;

    public CaptionProjectService(ApplicationDbContext db, ICaptionStorage storage, IFfmpegService ffmpeg, ICaptionCatalogService catalog,
        ICaptionEntitlementService entitlement, ICaptionJobQueue queue, IHinglishService hinglish, IOptions<CaptionOptions> options)
    {
        _db = db; _storage = storage; _ffmpeg = ffmpeg; _catalog = catalog; _entitlement = entitlement; _queue = queue;
        _hinglish = hinglish; _options = options.Value;
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
        CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
    };

    private async Task<CaptionProject> OwnedAsync(int userId, int projectId, bool track, CancellationToken ct)
    {
        var q = track ? _db.CaptionProjects : _db.CaptionProjects.AsNoTracking();
        return await q.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct)
               ?? throw new CaptionServiceException(404, "Project not found");
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
            catch (InvalidOperationException) { throw; } // ffmpeg missing -> surfaces as 500 with a clear log
            catch (Exception) { throw new CaptionServiceException(422, "This video could not be read"); }
            if (probe.Duration > limits.AbsoluteMaxSeconds) throw new CaptionServiceException(422, $"Videos can be at most {limits.AbsoluteMaxSeconds} seconds");

            var credit = await _entitlement.DetermineCreditAsync(userId, probe.Duration, ct);
            if (credit == null) throw new CaptionServiceException(402, await _entitlement.DescribeDenialAsync(probe.Duration, ct));

            var storagePath = $"captain-title/uploads/{userId}/{Guid.NewGuid():N}{suffix}";
            try { await _storage.PutFileAsync(storagePath, tempFile, string.IsNullOrWhiteSpace(contentType) ? "video/mp4" : contentType, ct); }
            catch (Exception) { throw new CaptionServiceException(503, "Video upload could not be stored. Please try again."); }

            var project = new CaptionProject
            {
                UserId = userId, Name = safeName, Duration = probe.Duration, Width = probe.Width, Height = probe.Height, SizeBytes = written,
                Theme = theme, FontId = fontId, SpeechLanguage = speechLanguage, SourcePath = storagePath,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "video/mp4" : contentType, CreditType = credit,
                Status = CaptionProjectStatus.Queued, Progress = 5, ProcessingLabel = "Upload complete",
            };
            _db.CaptionProjects.Add(project);
            await _db.SaveChangesAsync(ct);
            _queue.Enqueue(project.Id, consumeCredit: true);
            return ToDto(project);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
        }
    }

    public async Task<CaptionProjectDto> GetAsync(int userId, int projectId, CancellationToken ct = default) =>
        ToDto(await OwnedAsync(userId, projectId, false, ct));

    public async Task SaveAsync(int userId, int projectId, CaptionProjectEditDto edit, CancellationToken ct = default)
    {
        var catalog = await _catalog.GetCatalogAsync(ct);
        if (catalog.Themes.All(t => t.Id != edit.Theme)) throw new CaptionServiceException(400, "Unknown theme");
        if (catalog.Fonts.All(f => f.Id != edit.FontId)) throw new CaptionServiceException(400, "Unknown caption font");
        if (!Positions.Contains(edit.CaptionPosition)) throw new CaptionServiceException(400, "Unknown caption position");

        var p = await OwnedAsync(userId, projectId, true, ct);
        p.Theme = edit.Theme; p.FontId = edit.FontId; p.WordsPerCaption = edit.WordsPerCaption; p.CaptionLines = edit.CaptionLines;
        p.CaptionPosition = edit.CaptionPosition; p.AudioVolume = edit.AudioVolume; p.AudioMuted = edit.AudioMuted;
        p.Brightness = edit.Brightness; p.Contrast = edit.Contrast; p.Saturation = edit.Saturation; p.RenderQuality = edit.RenderQuality;
        p.WordsJson = CaptionJson.Serialize(edit.Words); p.ElementsJson = CaptionJson.Serialize(edit.Elements);
        p.SfxMarkersJson = CaptionJson.Serialize(edit.SfxMarkers); p.Variant = edit.Variant; p.UpdatedAt = DateTime.UtcNow;
        p.ScriptMode = edit.ScriptMode; p.OutlineEnabled = edit.OutlineEnabled; p.OutlineWidth = edit.OutlineWidth; p.OutlineColor = edit.OutlineColor;
        p.ShadowEnabled = edit.ShadowEnabled; p.ShadowAngle = edit.ShadowAngle; p.ShadowDistance = edit.ShadowDistance; p.ShadowBlur = edit.ShadowBlur;
        p.ShadowOpacity = edit.ShadowOpacity; p.ShadowColor = edit.ShadowColor; p.GlowEnabled = edit.GlowEnabled; p.GlowColor = edit.GlowColor;
        p.GlowBlur = edit.GlowBlur; p.GlowOpacity = edit.GlowOpacity; p.BackgroundEnabled = edit.BackgroundEnabled;
        p.BackgroundColor = edit.BackgroundColor; p.BackgroundOpacity = edit.BackgroundOpacity; p.Bold = edit.Bold; p.Italic = edit.Italic;
        await _db.SaveChangesAsync(ct);
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
        p.SpeechLanguage = speechLanguage; p.Status = CaptionProjectStatus.Queued; p.Progress = 5; p.ProcessingLabel = "Re-listen queued";
        p.ErrorMessage = null; p.WordsJson = "[]"; p.SourceTranscript = ""; p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        _queue.Enqueue(p.Id, consumeCredit: false);
        return ToDto(p);
    }

    public async Task<List<CaptionWordDto>> ReassignEmphasisAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var p = await OwnedAsync(userId, projectId, true, ct);
        var words = CaptionJson.ParseList<CaptionWordDto>(p.WordsJson);
        if (words.Count == 0) throw new CaptionServiceException(422, "No captions to re-tag yet");
        try { words = await _hinglish.ReassignEmphasisAsync(words, p.Name, ct); }
        catch (Exception) { throw new CaptionServiceException(502, "AI emphasis re-assignment failed. Please try again."); }

        // Re-bake colour/size from the (possibly different) tier, exactly like a template switch.
        var theme = (await _catalog.GetStyleSetAsync(ct)).Theme(p.Theme);
        foreach (var w in words) { var (c, s) = theme.BakeFor(w.Emphasis, w.Important); w.Color = c; w.FontSize = s; }
        p.WordsJson = CaptionJson.Serialize(words); p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return words;
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
        await _db.SaveChangesAsync(ct);
        _queue.EnqueueRender(p.Id);
        return ToDto(p);
    }

    public async Task<(Stream Stream, string ContentType)> OpenMediaAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var p = await OwnedAsync(userId, projectId, false, ct);
        var (stream, type) = await _storage.OpenAsync(p.PreviewPath ?? p.SourcePath, ct);
        return (stream, p.PreviewContentType ?? type);
    }

    public async Task<Stream> OpenRenderAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var p = await OwnedAsync(userId, projectId, false, ct);
        if (string.IsNullOrEmpty(p.RenderPath)) throw new CaptionServiceException(404, "Rendered video not found");
        return (await _storage.OpenAsync(p.RenderPath, ct)).Stream;
    }
}
