using System.Diagnostics;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Captions;
using Xunit;

namespace AppBackendBasic.Captions.Tests;

/// <summary>The studio's Ratio / Color / Audio panels in the export: frame sizes, the ASS canvas, and the ffmpeg filters (including a real run).</summary>
public class FrameLayoutTests
{
    [Theory]
    [InlineData("original", 720, 1280, 720, 1280)]
    [InlineData("original", null, null, 1080, 1920)]
    [InlineData("9:16", 1920, 1080, 1080, 1920)]
    [InlineData("1:1", 1920, 1080, 1080, 1080)]
    [InlineData("4:5", 1080, 1920, 1080, 1350)]
    [InlineData("16:9", 1080, 1920, 1920, 1080)]
    public void Output_size_follows_the_ratio(string ratio, int? srcW, int? srcH, int w, int h) =>
        Assert.Equal((w, h), FrameLayout.OutputSize(ratio, srcW, srcH));

    [Theory]
    [InlineData(1080, 1920, 1920)]
    [InlineData(1080, 1080, 1080)]
    [InlineData(1080, 1350, 1350)]
    [InlineData(1920, 1080, 608)]
    [InlineData(0, 0, 1920)]
    public void Canvas_is_1080_wide_with_the_frame_aspect(int w, int h, int canvasHeight) =>
        Assert.Equal(canvasHeight, FrameLayout.CanvasHeight(w, h));

    [Fact]
    public void Defaults_keep_the_previous_export_filters()
    {
        var args = Build(new RenderLook());
        var graph = args[args.IndexOf("-filter_complex") + 1];
        Assert.StartsWith("[0:v]eq=brightness=0:contrast=1:saturation=1[graded];[graded]subtitles=", graph);
        Assert.EndsWith("[0:a]volume=1[aout]", graph);
    }

    [Fact]
    public void Look_adds_white_balance_reframe_and_audio_chain()
    {
        var args = Build(new RenderLook(Warmth: 50, Tint: -50, FadeIn: 1, FadeOut: 2, Normalize: true, Duration: 10, AspectRatio: "1:1", FrameFit: "color", FrameColor: "#112233"));
        var graph = args[args.IndexOf("-filter_complex") + 1];
        Assert.Contains("colorchannelmixer=rr=1.12:gg=1.1:bb=0.88", graph);
        Assert.Contains("[graded]scale=1080:1080:force_original_aspect_ratio=decrease,pad=1080:1080:(ow-iw)/2:(oh-ih)/2:color=0x112233,setsar=1[framed]", graph);
        Assert.Contains("[framed]subtitles=", graph);
        Assert.Contains("volume=1,loudnorm=I=-14:TP=-1.5:LRA=11,aresample=48000,afade=t=in:st=0:d=1,afade=t=out:st=8:d=2[aout]", graph);
    }

    [Theory]
    [InlineData("original", 720, 1280, 720, 1280)]
    [InlineData("1080", 720, 1280, 1080, 1920)]
    [InlineData("720", 1080, 1920, 720, 1280)]
    [InlineData("480", 1080, 1920, 480, 854)]
    [InlineData("720", 1920, 1080, 1280, 720)]
    [InlineData("480", 1080, 1350, 480, 600)]
    [InlineData("nonsense", 1080, 1920, 1080, 1920)]
    public void Scaled_size_sets_the_short_side_and_keeps_even_dimensions(string resolution, int w, int h, int outW, int outH) =>
        Assert.Equal((outW, outH), FrameLayout.ScaledSize(resolution, w, h));

    [Fact]
    public void Resolution_scales_the_finished_picture_after_the_captions()
    {
        var graph = Build(new RenderLook(AspectRatio: "9:16", Resolution: "720"));
        Assert.Contains(",scale=720:1280:flags=lanczos,setsar=1[vout]", graph[graph.IndexOf("-filter_complex") + 1]);
        Assert.DoesNotContain("flags=lanczos", string.Join(" ", Build(new RenderLook(AspectRatio: "9:16", Resolution: "original"))));
    }

    [Theory]
    [InlineData("x/abc.mp4", "mp4", "video/mp4")]
    [InlineData("x/abc.MOV", "mov", "video/quicktime")]
    [InlineData(null, "mp4", "video/mp4")]
    public void Render_format_comes_from_the_stored_file(string? path, string format, string contentType)
    {
        Assert.Equal(format, RenderFormats.FromPath(path));
        Assert.Equal(contentType, RenderFormats.ContentType(format));
    }

    [Fact]
    public void Muted_audio_is_not_normalized()
    {
        var graph = Build(new RenderLook(Normalize: true), muted: true);
        Assert.DoesNotContain("loudnorm", string.Join(" ", graph));
    }

    [Fact]
    public void Ass_canvas_height_moves_caption_and_element_positions()
    {
        var words = new List<CaptionWordDto> { new() { Index = 0, Text = "hello", Original = "hello", Start = 0, End = 1 } };
        var elements = new List<CaptionElementDto> { new() { Id = "e", Kind = "sticker", Char = "*", PosX = 50, PosY = 50, Start = 0, End = 1 } };
        var styles = CaptionStyleFactory.FromSeed();
        var measurer = new TrueTypeTextMeasurer(SchemaTests.RepoFile("AppBackendBasic.API/Assets/captions/fonts"));
        var square = AssSubtitleWriter.Build(words, "viral", "poppins", null, 1, "middle", elements, new AssStyleOptions(), styles, measurer, 1080).Main;
        Assert.Contains("PlayResY: 1080", square);
        Assert.Contains("\\pos(540,540)", square);
        var tall = AssSubtitleWriter.Build(words, "viral", "poppins", null, 1, "middle", elements, new AssStyleOptions(), styles, measurer).Main;
        Assert.Contains("PlayResY: 1920", tall);
        Assert.Contains("\\pos(540,960)", tall);
    }

    [Theory]
    [InlineData("blur", "original", "mp4", "1080,1350")]
    [InlineData("color", "original", "mp4", "1080,1350")]
    [InlineData("crop", "original", "mp4", "1080,1350")]
    [InlineData("blur", "480", "mov", "480,600")]
    public void Real_ffmpeg_exports_the_reframed_size(string fit, string resolution, string format, string expectedSize)
    {
        if (!FfmpegReady()) return;
        var work = Path.Combine(Path.GetTempPath(), "ct-frame-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var source = Path.Combine(work, "source.mp4");
            Assert.Equal(0, Run(work, "ffmpeg", "-y", "-f", "lavfi", "-i", "testsrc=s=640x360:r=25:d=2", "-f", "lavfi", "-i", "sine=f=440:d=2",
                "-c:v", "libx264", "-pix_fmt", "yuv420p", "-c:a", "aac", "-shortest", source).Code);
            var ass = Path.Combine(work, "captions.ass");
            File.WriteAllText(ass, "[Script Info]\nScriptType: v4.00+\nPlayResX: 1080\nPlayResY: 1350\n");
            var output = Path.Combine(work, "out." + format);
            var look = new RenderLook(Warmth: 20, Tint: 10, FadeIn: 0.5, FadeOut: 0.5, Normalize: true, Duration: 2, AspectRatio: "4:5", FrameFit: fit, FrameColor: "#ff0000",
                Resolution: resolution);
            var args = FfmpegRenderArgs.Build(source, ass, output, work, 100, false, 0, 100, 100, "draft", Array.Empty<(string, double)>(),
                type => new SfxTone(type, 440, 440, 0.2), videoWidth: 640, videoHeight: 360, look: look);
            var (code, err) = Run(work, "ffmpeg", args.ToArray());
            Assert.True(code == 0, err.Length > 1500 ? err[^1500..] : err);
            var (_, size) = Run(work, "ffprobe", "-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height", "-of", "csv=p=0", output);
            Assert.Equal(expectedSize, size.Trim());
            var (_, container) = Run(work, "ffprobe", "-v", "error", "-show_entries", "format=format_name", "-of", "csv=p=0", output);
            Assert.Contains(format, container);
        }
        finally { try { Directory.Delete(work, true); } catch { } }
    }

    private static List<string> Build(RenderLook look, bool muted = false) =>
        FfmpegRenderArgs.Build("/tmp/w/source.mp4", "/tmp/w/captions.ass", "/tmp/w/out.mp4", "/tmp/w/fonts", 100, muted, 0, 100, 100, "high",
            Array.Empty<(string, double)>(), type => new SfxTone(type, 440, 440, 0.2), videoWidth: 640, videoHeight: 360, look: look);

    private static (int Code, string Out) Run(string workDir, string exe, params string[] args)
    {
        var psi = new ProcessStartInfo(exe) { RedirectStandardError = true, RedirectStandardOutput = true, WorkingDirectory = workDir };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEndAsync();
        var err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout.Result + err);
    }

    private static bool FfmpegReady()
    {
        try
        {
            var (code, filters) = Run(Path.GetTempPath(), "ffmpeg", "-hide_banner", "-filters");
            var (_, encoders) = Run(Path.GetTempPath(), "ffmpeg", "-hide_banner", "-encoders");
            return code == 0 && filters.Contains(" subtitles ") && filters.Contains(" loudnorm ") && encoders.Contains("libx264");
        }
        catch { return false; }
    }
}
