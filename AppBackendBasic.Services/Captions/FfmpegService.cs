using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppBackendBasic.Services.Captions;

public sealed record FfmpegResult(int ExitCode, string StdErr, string StdOut = "");

public sealed record VideoProbe(double Duration, int? Width, int? Height, string? VideoCodec = null, string? AudioCodec = null)
{
    /// <summary>True when every mainstream browser can play the file as-is (H.264 video, AAC/MP3 or no audio) - no preview transcode needed.</summary>
    public bool IsBrowserPlayable => string.Equals(VideoCodec, "h264", StringComparison.OrdinalIgnoreCase)
        && (AudioCodec == null || AudioCodec.Equals("aac", StringComparison.OrdinalIgnoreCase) || AudioCodec.Equals("mp3", StringComparison.OrdinalIgnoreCase));
}

public sealed record FfmpegCapability(bool Available, string Path, string? Version, bool HasLibx264, bool HasLibass, string? Problem);

public interface IFfmpegService
{
    Task<FfmpegResult> RunAsync(IEnumerable<string> args, string? workingDirectory = null, CancellationToken ct = default);
    Task<VideoProbe> ProbeAsync(string path, CancellationToken ct = default);
    Task ExtractAudioAsync(string source, string wavOut, CancellationToken ct = default);
    /// <summary>Creates a 720p H.264/AAC MP4 preview (only needed when the source is not browser-playable, e.g. iPhone HEVC).</summary>
    Task<bool> CreateBrowserPreviewAsync(string source, string output, CancellationToken ct = default);
    /// <summary>Checks the ffmpeg binary can run and has the encoders/filters the pipeline needs. Used by the admin health view.</summary>
    Task<FfmpegCapability> CheckAsync(CancellationToken ct = default);
    Task<List<double>> SilenceMidpointsAsync(string audioPath, CancellationToken ct = default);
}

public class FfmpegService : IFfmpegService
{
    private static readonly Regex DurationRegex = new(@"Duration:\s*(\d+):(\d+):([\d.]+)", RegexOptions.Compiled);
    private static readonly Regex ResolutionRegex = new(@"Video:.*?(\d{2,5})x(\d{2,5})", RegexOptions.Compiled);
    private static readonly Regex VideoCodecRegex = new(@"Video:\s*([A-Za-z0-9_]+)", RegexOptions.Compiled);
    private static readonly Regex AudioCodecRegex = new(@"Audio:\s*([A-Za-z0-9_]+)", RegexOptions.Compiled);

    private readonly CaptionOptions _options;
    private readonly ILogger<FfmpegService> _logger;

    public FfmpegService(IOptions<CaptionOptions> options, ILogger<FfmpegService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FfmpegResult> RunAsync(IEnumerable<string> args, string? workingDirectory = null, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(_options.ResolveFfmpegPath())
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (workingDirectory != null) psi.WorkingDirectory = workingDirectory;
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi };
        var stderr = new StringBuilder();
        var stdout = new StringBuilder();
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        try { process.Start(); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            throw new InvalidOperationException($"ffmpeg could not be started ('{_options.FfmpegPath}'). Install ffmpeg, bundle it under Tools/ffmpeg/<rid>/ or set Captions:FfmpegPath.", ex);
        }
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        try { await process.WaitForExitAsync(ct); }
        catch (OperationCanceledException) { try { process.Kill(true); } catch { /* already gone */ } throw; }
        process.WaitForExit(); // flush async readers
        return new FfmpegResult(process.ExitCode, stderr.ToString(), stdout.ToString());
    }

    public async Task<VideoProbe> ProbeAsync(string path, CancellationToken ct = default)
    {
        // `ffmpeg -i file` prints stream info and exits non-zero without decoding the whole file.
        var result = await RunAsync(new[] { "-hide_banner", "-i", path }, ct: ct);
        var m = DurationRegex.Match(result.StdErr);
        if (!m.Success) throw new InvalidDataException("Video duration unavailable");
        double duration = int.Parse(m.Groups[1].Value) * 3600 + int.Parse(m.Groups[2].Value) * 60
                          + double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        var r = ResolutionRegex.Match(result.StdErr);
        var vc = VideoCodecRegex.Match(result.StdErr);
        var ac = AudioCodecRegex.Match(result.StdErr);
        return new VideoProbe(Math.Round(duration, 2, MidpointRounding.ToEven),
            r.Success ? int.Parse(r.Groups[1].Value) : null, r.Success ? int.Parse(r.Groups[2].Value) : null,
            vc.Success ? vc.Groups[1].Value : null, ac.Success ? ac.Groups[1].Value : null);
    }

    public async Task ExtractAudioAsync(string source, string wavOut, CancellationToken ct = default)
    {
        var result = await RunAsync(new[] { "-y", "-i", source, "-vn", "-ac", "1", "-ar", "16000", "-c:a", "pcm_s16le", wavOut }, ct: ct);
        if (result.ExitCode != 0)
        {
            _logger.LogError("Audio extraction failed: {Err}", Tail(result.StdErr, 1200));
            throw new InvalidOperationException("Audio extraction failed");
        }
    }

    public async Task<bool> CreateBrowserPreviewAsync(string source, string output, CancellationToken ct = default)
    {
        // H.264/AAC MP4 (+faststart for Range requests). Much cheaper than the old VP9 realtime transcode and plays everywhere.
        var args = new List<string> { "-y", "-i", source, "-vf", "scale=720:-2:force_original_aspect_ratio=decrease", "-c:v", "libx264", "-preset", "veryfast", "-crf", "28" };
        if (_options.FfmpegThreads > 0) { args.Add("-threads"); args.Add(_options.FfmpegThreads.ToString(CultureInfo.InvariantCulture)); }
        args.AddRange(new[] { "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "96k", "-movflags", "+faststart", output });
        var result = await RunAsync(args, ct: ct);
        if (result.ExitCode != 0)
        {
            _logger.LogWarning("Browser preview conversion failed: {Err}", Tail(result.StdErr, 1200));
            return false;
        }
        return true;
    }

    public async Task<FfmpegCapability> CheckAsync(CancellationToken ct = default)
    {
        var path = _options.ResolveFfmpegPath();
        try
        {
            var version = await RunAsync(new[] { "-hide_banner", "-version" }, ct: ct);
            if (version.ExitCode != 0) return new FfmpegCapability(false, path, null, false, false, "ffmpeg returned an error");
            var first = (version.StdOut.Length > 0 ? version.StdOut : version.StdErr).Split('\n').FirstOrDefault()?.Trim();
            var encoders = await RunAsync(new[] { "-hide_banner", "-encoders" }, ct: ct);
            var filters = await RunAsync(new[] { "-hide_banner", "-filters" }, ct: ct);
            bool x264 = encoders.StdErr.Contains("libx264") || encoders.StdOut.Contains("libx264");
            bool ass = filters.StdErr.Contains(" subtitles ") || filters.StdOut.Contains(" subtitles ");
            return new FfmpegCapability(true, path, first, x264, ass,
                x264 && ass ? null : "This ffmpeg build lacks libx264 or libass (subtitles filter); captions cannot be rendered");
        }
        catch (Exception ex)
        {
            return new FfmpegCapability(false, path, null, false, false, ex.Message);
        }
    }

    public async Task<List<double>> SilenceMidpointsAsync(string audioPath, CancellationToken ct = default)
    {
        var result = await RunAsync(new[] { "-i", audioPath, "-af", "silencedetect=noise=-35dB:d=0.3", "-f", "null", "-" }, ct: ct);
        return TranscriptionAlgorithms.SilenceMidpoints(result.StdErr);
    }

    private static string Tail(string s, int n) => s.Length <= n ? s : s[^n..];
}
