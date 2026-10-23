using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppBackendBasic.Services.Captions;

/// <summary>Raised when an export exceeds the configured ceiling; its message is safe to show to the user.</summary>
public sealed class CaptionRenderTimeoutException : Exception
{
    public CaptionRenderTimeoutException(string message) : base(message) { }
}

public interface ICaptionRenderService
{
    /// <summary>Renders the project export (ASS burn-in + grading + audio) in the background and stores the MP4.</summary>
    Task RunAsync(int projectId, CancellationToken ct = default);
}

/// <summary>The export render (port of captions.py render endpoint), now a background job instead of a blocking HTTP request.</summary>
public class CaptionRenderService : ICaptionRenderService
{
    private readonly ApplicationDbContext _db;
    private readonly ICaptionStorage _storage;
    private readonly IFfmpegService _ffmpeg;
    private readonly ICaptionCatalogService _catalog;
    private readonly SystemSettingsService _settings;
    private readonly CaptionOptions _options;
    private readonly ILogger<CaptionRenderService> _logger;

    public CaptionRenderService(ApplicationDbContext db, ICaptionStorage storage, IFfmpegService ffmpeg, ICaptionCatalogService catalog,
        SystemSettingsService settings, IOptions<CaptionOptions> options, ILogger<CaptionRenderService> logger)
    {
        _db = db; _storage = storage; _ffmpeg = ffmpeg; _catalog = catalog; _settings = settings; _options = options.Value; _logger = logger;
    }

    private async Task<(string Preset, string Crf)> EncodeSettingsAsync(string quality)
    {
        bool draft = quality == "draft";
        var preset = await _settings.GetSettingValueAsync(draft ? CaptionSettingKeys.RenderDraftPreset : CaptionSettingKeys.RenderHighPreset);
        var crf = await _settings.GetIntSettingAsync(draft ? CaptionSettingKeys.RenderDraftCrf : CaptionSettingKeys.RenderHighCrf, draft ? 28 : 18);
        var allowed = new HashSet<string> { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" };
        if (string.IsNullOrWhiteSpace(preset) || !allowed.Contains(preset)) preset = draft ? "ultrafast" : "veryfast";
        return (preset, Math.Clamp(crf, 14, 40).ToString());
    }

    public async Task RunAsync(int projectId, CancellationToken ct = default)
    {
        string? tempDir = null;
        try
        {
            var p = await _db.CaptionProjects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projectId, ct);
            if (p == null || p.Status != CaptionProjectStatus.Rendering) return;

            var styles = await _catalog.GetStyleSetAsync(ct);
            var tones = (await _catalog.GetSfxTonesAsync(ct)).ToDictionary(t => t.Key);
            // An unrecognised type falls back to bass_drop, like the old _sfx_source_filter default.
            SfxTone ToneFor(string type) => tones.TryGetValue(type, out var t) ? t : new SfxTone("bass_drop", 90, 27, 0.4);

            tempDir = Path.Combine(Path.GetTempPath(), "captain-render-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var source = Path.Combine(tempDir, "source.mp4");
            var ass = Path.Combine(tempDir, "captions.ass");
            var output = Path.Combine(tempDir, "captain-title.mp4");
            await using (var src = (await _storage.OpenAsync(p.SourcePath, ct)).Stream)
            await using (var dst = File.Create(source))
                await src.CopyToAsync(dst, ct);

            await _db.CaptionProjects.Where(x => x.Id == projectId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Progress, 30).SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);

            var words = CaptionJson.ParseList<CaptionWordDto>(p.WordsJson);
            var elements = CaptionJson.ParseList<CaptionElementDto>(p.ElementsJson);
            var markers = CaptionJson.ParseList<CaptionSfxMarkerDto>(p.SfxMarkersJson).Select(m => (m.Type, m.Time)).ToList();
            // Street Glow bakes its own shadow/glow into the template, so the blur STRENGTH comes from the template, not the project's panel.
            bool streetGlow = p.Theme == FixedTemplates.StreetGlow;
            double shadowBlur = streetGlow ? FixedTemplates.StreetGlowShadowBlur : p.ShadowBlur;
            double glowBlur = streetGlow ? FixedTemplates.StreetGlowGlowBlur : p.GlowBlur;
            var style = new AssStyleOptions
            {
                ScriptMode = p.ScriptMode, Bold = p.Bold, Italic = p.Italic,
                OutlineEnabled = p.OutlineEnabled, OutlineWidth = p.OutlineWidth, OutlineColor = p.OutlineColor,
                ShadowEnabled = p.ShadowEnabled, ShadowAngle = p.ShadowAngle, ShadowDistance = p.ShadowDistance, ShadowBlur = shadowBlur,
                ShadowOpacity = p.ShadowOpacity, ShadowColor = p.ShadowColor,
                GlowEnabled = p.GlowEnabled, GlowColor = p.GlowColor, GlowBlur = glowBlur, GlowOpacity = p.GlowOpacity,
                BackgroundEnabled = p.BackgroundEnabled, BackgroundColor = p.BackgroundColor, BackgroundOpacity = p.BackgroundOpacity,
            };
            var scripts = AssSubtitleWriter.Build(words, p.Theme, p.FontId, p.WordsPerCaption, p.CaptionLines, p.CaptionPosition, elements, style,
                styles, new TrueTypeTextMeasurer(_options.ResolveFontsDir()));
            var utf8 = new System.Text.UTF8Encoding(false);
            await File.WriteAllTextAsync(ass, scripts.Main, utf8, ct);
            string? Write(string name, string? content)
            {
                if (content == null) return null;
                File.WriteAllText(Path.Combine(tempDir, name), content, utf8);
                return name;
            }
            var masks = new BlurMaskFiles(Write("shadow_alpha.ass", scripts.ShadowAlpha), Write("shadow_color.ass", scripts.ShadowColor), shadowBlur,
                Write("glow_alpha.ass", scripts.GlowAlpha), Write("glow_color.ass", scripts.GlowColor), glowBlur);

            var probe = await _ffmpeg.ProbeAsync(source, ct);
            var args = FfmpegRenderArgs.Build(source, ass, output, _options.ResolveFontsDir(), p.AudioVolume, p.AudioMuted,
                p.Brightness, p.Contrast, p.Saturation, p.RenderQuality, markers, ToneFor, _options.FfmpegThreads, await EncodeSettingsAsync(p.RenderQuality),
                masks, p.Width, p.Height, hasAudio: probe.AudioCodec != null);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.RenderTimeoutSeconds));
            FfmpegResult result;
            try { result = await _ffmpeg.RunAsync(args, tempDir, timeout.Token); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new CaptionRenderTimeoutException($"Rendering took longer than {_options.RenderTimeoutSeconds}s and was stopped. Try a shorter clip, or Draft quality for a faster export.");
            }
            if (result.ExitCode != 0)
            {
                _logger.LogError("Render failed for project {Id}: {Err}", projectId, result.StdErr.Length > 1500 ? result.StdErr[^1500..] : result.StdErr);
                throw new InvalidOperationException("ffmpeg render failed");
            }

            var outputPath = $"captain-title/renders/{p.UserId}/{Guid.NewGuid():N}.mp4";
            await _storage.PutFileAsync(outputPath, output, "video/mp4", ct);
            await _db.CaptionProjects.Where(x => x.Id == projectId).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.RenderPath, outputPath).SetProperty(x => x.Status, CaptionProjectStatus.Rendered)
                .SetProperty(x => x.Progress, 100).SetProperty(x => x.ProcessingLabel, "Export ready")
                .SetProperty(x => x.ErrorMessage, (string?)null).SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed for project {Id}", projectId);
            var message = ex is CaptionRenderTimeoutException ? ex.Message : "Video rendering failed. Please try again.";
            await _db.CaptionProjects.Where(x => x.Id == projectId && x.Status == CaptionProjectStatus.Rendering).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, CaptionProjectStatus.Ready).SetProperty(x => x.ProcessingLabel, "Captions ready")
                .SetProperty(x => x.ErrorMessage, message).SetProperty(x => x.UpdatedAt, DateTime.UtcNow), CancellationToken.None);
        }
        finally
        {
            if (tempDir != null) try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
        }
    }
}
