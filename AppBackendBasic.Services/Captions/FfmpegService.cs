using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppBackendBasic.Services.Captions;

public sealed record FfmpegResult(int ExitCode, string StdErr);

public sealed record VideoProbe(double Duration, int? Width, int? Height);

public interface IFfmpegService
{
    Task<FfmpegResult> RunAsync(IEnumerable<string> args, string? workingDirectory = null, CancellationToken ct = default);
    Task<VideoProbe> ProbeAsync(string path, CancellationToken ct = default);
    Task ExtractAudioAsync(string source, string wavOut, CancellationToken ct = default);
    /// <summary>Creates the 720p VP9/Opus WebM the editor plays (browsers cannot play every MOV). Null when conversion fails.</summary>
    Task<bool> CreateBrowserPreviewAsync(string source, string output, CancellationToken ct = default);
    Task<List<double>> SilenceMidpointsAsync(string audioPath, CancellationToken ct = default);
}

public class FfmpegService : IFfmpegService
{
    private static readonly Regex DurationRegex = new(@"Duration:\s*(\d+):(\d+):([\d.]+)", RegexOptions.Compiled);
    private static readonly Regex ResolutionRegex = new(@"Video:.*?(\d{2,5})x(\d{2,5})", RegexOptions.Compiled);

    private readonly CaptionOptions _options;
    private readonly ILogger<FfmpegService> _logger;

    public FfmpegService(IOptions<CaptionOptions> options, ILogger<FfmpegService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FfmpegResult> RunAsync(IEnumerable<string> args, string? workingDirectory = null, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(_options.FfmpegPath)
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
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        process.OutputDataReceived += (_, _) => { };
        try { process.Start(); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            throw new InvalidOperationException($"ffmpeg could not be started ('{_options.FfmpegPath}'). Install ffmpeg or set Captions:FfmpegPath.", ex);
        }
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        try { await process.WaitForExitAsync(ct); }
        catch (OperationCanceledException) { try { process.Kill(true); } catch { /* already gone */ } throw; }
        process.WaitForExit(); // flush async readers
        return new FfmpegResult(process.ExitCode, stderr.ToString());
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
        return new VideoProbe(Math.Round(duration, 2, MidpointRounding.ToEven),
            r.Success ? int.Parse(r.Groups[1].Value) : null, r.Success ? int.Parse(r.Groups[2].Value) : null);
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
        var result = await RunAsync(new[]
        {
            "-y", "-i", source, "-vf", "scale=720:-2", "-c:v", "libvpx-vp9", "-deadline", "realtime", "-cpu-used", "8",
            "-b:v", "1100k", "-c:a", "libopus", "-b:a", "96k", output,
        }, ct: ct);
        if (result.ExitCode != 0)
        {
            _logger.LogWarning("Browser preview conversion failed: {Err}", Tail(result.StdErr, 1200));
            return false;
        }
        return true;
    }

    public async Task<List<double>> SilenceMidpointsAsync(string audioPath, CancellationToken ct = default)
    {
        var result = await RunAsync(new[] { "-i", audioPath, "-af", "silencedetect=noise=-35dB:d=0.3", "-f", "null", "-" }, ct: ct);
        return TranscriptionAlgorithms.SilenceMidpoints(result.StdErr);
    }

    private static string Tail(string s, int n) => s.Length <= n ? s : s[^n..];
}
