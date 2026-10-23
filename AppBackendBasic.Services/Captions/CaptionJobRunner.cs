using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Captions;

public interface ICaptionJobRunner
{
    Task RunAsync(int projectId, bool consumeCredit, CancellationToken ct = default);
}

/// <summary>One caption generation run (port of captions.py _run_caption_job).</summary>
public class CaptionJobRunner : ICaptionJobRunner
{
    private readonly ApplicationDbContext _db;
    private readonly ICaptionStorage _storage;
    private readonly IFfmpegService _ffmpeg;
    private readonly ICaptionTranscriptionService _transcription;
    private readonly IHinglishService _hinglish;
    private readonly ICaptionCatalogService _catalog;
    private readonly ICaptionEntitlementService _entitlement;
    private readonly ILogger<CaptionJobRunner> _logger;

    public CaptionJobRunner(ApplicationDbContext db, ICaptionStorage storage, IFfmpegService ffmpeg,
        ICaptionTranscriptionService transcription, IHinglishService hinglish, ICaptionCatalogService catalog,
        ICaptionEntitlementService entitlement, ILogger<CaptionJobRunner> logger)
    {
        _db = db; _storage = storage; _ffmpeg = ffmpeg; _transcription = transcription; _hinglish = hinglish;
        _catalog = catalog; _entitlement = entitlement; _logger = logger;
    }

    private Task SetProgressAsync(int id, int progress, string label, CancellationToken ct) =>
        _db.CaptionProjects.Where(p => p.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(p => p.Progress, progress).SetProperty(p => p.ProcessingLabel, label).SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);

    public async Task RunAsync(int projectId, bool consumeCredit, CancellationToken ct = default)
    {
        string? tempDir = null;
        try
        {
            // Claim the project with a compare-and-set so only ONE instance/worker ever runs it (safe when the API is scaled out).
            var claimed = await _db.CaptionProjects.Where(p => p.Id == projectId && p.Status == CaptionProjectStatus.Queued).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, CaptionProjectStatus.Processing).SetProperty(p => p.Progress, 12)
                .SetProperty(p => p.ProcessingLabel, "Preparing audio").SetProperty(p => p.ErrorMessage, (string?)null)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
            if (claimed == 0) return;
            var project = await _db.CaptionProjects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct);
            if (project == null) return;

            tempDir = Path.Combine(Path.GetTempPath(), "captain-job-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var suffix = Path.GetExtension(project.Name).ToLowerInvariant();
            if (string.IsNullOrEmpty(suffix)) suffix = ".mp4";
            var source = Path.Combine(tempDir, $"source{suffix}");
            var audio = Path.Combine(tempDir, "speech.wav");
            var previewFile = Path.Combine(tempDir, "preview.mp4");

            await using (var src = (await _storage.OpenAsync(project.SourcePath, ct)).Stream)
            await using (var dst = File.Create(source))
                await src.CopyToAsync(dst, ct);

            await _ffmpeg.ExtractAudioAsync(source, audio, ct);
            // Only transcode a preview when the browser cannot play the upload (e.g. iPhone HEVC); H.264/AAC plays as-is = zero cost.
            var probe = await _ffmpeg.ProbeAsync(source, ct);
            var previewTask = probe.IsBrowserPlayable ? Task.FromResult(false) : _ffmpeg.CreateBrowserPreviewAsync(source, previewFile, ct);
            await SetProgressAsync(projectId, 28, "Listening word by word", ct);

            List<CaptionWordDto> words; TranscriptionDiagnostics diagnostics;
            try
            {
                (words, diagnostics) = await _transcription.TranscribeWordsAsync(audio, project.SpeechLanguage, ct);
            }
            catch
            {
                try { await previewTask; } catch { /* observed; the real failure is rethrown */ }
                throw;
            }
            var sourceTranscript = string.Join(" ", words.Select(w => w.Original));
            await SetProgressAsync(projectId, 78, "Creating Roman Hinglish", ct);
            words = await _hinglish.ConvertToHinglishAsync(words, project.Name, ct);

            string? previewPath = null;
            if (await previewTask)
            {
                previewPath = $"captain-title/previews/{project.UserId}/{Guid.NewGuid():N}.mp4";
                await _storage.PutFileAsync(previewPath, previewFile, "video/mp4", ct);
            }

            var styles = await _catalog.GetStyleSetAsync(ct);
            var theme = styles.Theme(project.Theme);
            foreach (var word in words)
            {
                var (color, size) = theme.BakeFor(word.Emphasis, word.Important);
                word.Color = color; word.FontSize = size;
            }

            var wordsJson = CaptionJson.Serialize(words);
            var qualityJson = CaptionJson.Serialize(new { mode = diagnostics.Mode, chunks_used = diagnostics.ChunksUsed,
                unique_ratio = diagnostics.UniqueRatio, bigram_ratio = diagnostics.BigramRatio, ngram_repeat = diagnostics.NgramRepeat });
            await _db.CaptionProjects.Where(p => p.Id == projectId).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.WordsJson, wordsJson).SetProperty(p => p.SourceTranscript, sourceTranscript)
                .SetProperty(p => p.TranscriptionMode, diagnostics.Mode).SetProperty(p => p.TranscriptionQualityJson, qualityJson)
                .SetProperty(p => p.Status, CaptionProjectStatus.Ready).SetProperty(p => p.Progress, 100)
                .SetProperty(p => p.ProcessingLabel, "Captions ready")
                .SetProperty(p => p.PreviewPath, previewPath ?? project.PreviewPath)
                .SetProperty(p => p.PreviewContentType, previewPath != null ? "video/mp4" : project.PreviewContentType)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);

            if (consumeCredit && project.CreditType != null)
                await _entitlement.ConsumeAsync(project.UserId, project.CreditType, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // shutdown: the project stays queued/processing and is resumed on next start
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background caption job failed for {ProjectId}", projectId);
            var message = ex is NoTrustworthyTranscriptException
                ? "No trustworthy transcript was found. Try clearer speech or less background music."
                : "Caption processing could not finish. Please try Re-listen.";
            await _db.CaptionProjects.Where(p => p.Id == projectId).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, CaptionProjectStatus.Failed).SetProperty(p => p.Progress, 100)
                .SetProperty(p => p.ProcessingLabel, "Processing failed").SetProperty(p => p.ErrorMessage, message)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), CancellationToken.None);
        }
        finally
        {
            if (tempDir != null) try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
        }
    }
}
