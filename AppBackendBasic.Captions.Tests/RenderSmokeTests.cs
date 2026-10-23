using System.Diagnostics;
using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Captions;
using Xunit;

namespace AppBackendBasic.Captions.Tests;

/// <summary>
/// Runs the REAL ffmpeg filtergraph (blur masks, ASS burn-in, SFX mix) for both fixed templates on a synthetic clip. Skipped when ffmpeg with
/// libass + libx264 is not installed. Proves the generated filter_complex is valid, not just that it equals the Python strings.
/// </summary>
public class RenderSmokeTests
{
    private static (int Code, string Err) Run(string workDir, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo("ffmpeg") { RedirectStandardError = true, RedirectStandardOutput = true, WorkingDirectory = workDir };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEndAsync();
        var err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, err + stdout.Result);
    }

    private static bool FfmpegReady()
    {
        try
        {
            var (code, err) = Run(Path.GetTempPath(), new[] { "-hide_banner", "-filters" });
            if (code != 0) return false;
            var (_, enc) = Run(Path.GetTempPath(), new[] { "-hide_banner", "-encoders" });
            return err.Contains(" subtitles ") && enc.Contains("libx264");
        }
        catch { return false; }
    }

    [Theory]
    [InlineData("street_glow")]
    [InlineData("doodle_marker")]
    public void Export_renders_with_real_ffmpeg(string theme)
    {
        if (!FfmpegReady())
        {
            // No ffmpeg+libass here: nothing to prove, unless the run demands it (CT_REQUIRE_FFMPEG=1, e.g. CI with ffmpeg installed).
            Assert.True(Environment.GetEnvironmentVariable("CT_REQUIRE_FFMPEG") != "1", "ffmpeg with libass+libx264 is required but not available");
            return;
        }
        var work = Path.Combine(Path.GetTempPath(), "ct-render-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var fonts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AppBackendBasic.API", "Assets", "captions", "fonts"));
            Directory.CreateSymbolicLink(Path.Combine(work, "fonts"), fonts);
            var source = Path.Combine(work, "source.mp4");
            var gen = Run(work, new[] { "-y", "-f", "lavfi", "-i", "testsrc=s=360x640:r=25:d=3", "-f", "lavfi", "-i", "sine=f=440:d=3", "-shortest",
                "-c:v", "libx264", "-pix_fmt", "yuv420p", "-c:a", "aac", source });
            Assert.Equal(0, gen.Code);

            var golden = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "golden", "golden.json"))).RootElement;
            var words = JsonSerializer.Deserialize<List<CaptionWordDto>>(golden.GetProperty("ass").GetProperty("words").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.Take(8).ToList();
            var style = new AssStyleOptions { ShadowEnabled = true, ShadowBlur = 3, GlowEnabled = true, GlowBlur = 8, OutlineEnabled = false };
            var scripts = AssSubtitleWriter.Build(words, theme, "poppins", null, 1, "bottom", null, style, CaptionStyleFactory.FromSeed(), new TrueTypeTextMeasurer(fonts));
            File.WriteAllText(Path.Combine(work, "captions.ass"), scripts.Main);
            string? W(string n, string? c) { if (c == null) return null; File.WriteAllText(Path.Combine(work, n), c); return n; }
            var masks = new BlurMaskFiles(W("shadow_alpha.ass", scripts.ShadowAlpha), W("shadow_color.ass", scripts.ShadowColor), 3,
                W("glow_alpha.ass", scripts.GlowAlpha), W("glow_color.ass", scripts.GlowColor), 8);
            var args = FfmpegRenderArgs.Build(source, Path.Combine(work, "captions.ass"), Path.Combine(work, "out.mp4"), Path.Combine(work, "fonts"),
                100, false, 0, 100, 100, "draft", new[] { ("whoosh", 0.5), ("cash", 1.0), ("pop", 1.5), ("shutter", 2.0), ("bass_drop", 2.4) },
                type => new SfxTone(type, 440, 440, 0.2), masks: masks, videoWidth: 360, videoHeight: 640);
            var (code, err) = Run(work, args);
            Assert.True(code == 0, err.Length > 1500 ? err[^1500..] : err);
            Assert.True(new FileInfo(Path.Combine(work, "out.mp4")).Length > 1000);
        }
        finally { try { Directory.Delete(work, true); } catch { } }
    }
}
