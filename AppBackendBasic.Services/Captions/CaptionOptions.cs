using System.Runtime.InteropServices;

namespace AppBackendBasic.Services.Captions;

/// <summary>Bound from the "Captions" configuration section. Secrets belong in user-secrets / env vars.</summary>
public class CaptionOptions
{
    public const string SectionName = "Captions";

    /// <summary>Where uploads, previews and renders live. Swap ICaptionStorage for S3/R2 before going multi-node.</summary>
    public string StorageRoot { get; set; } = "App_Data/caption_storage";

    /// <summary>Directory with the bundled .ttf files libass resolves caption fonts from.</summary>
    public string FontsDir { get; set; } = "Assets/captions/fonts";

    /// <summary>
    /// ffmpeg executable. Resolution order: this value when it is an existing absolute path, then a bundled binary under
    /// <c>Tools/ffmpeg/&lt;rid&gt;/</c> next to the app (rid = linux-x64, linux-arm64, win-x64, osx-arm64, osx-x64), then plain
    /// "ffmpeg" on PATH (Docker/apt/brew installs).
    /// </summary>
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>Caption jobs (transcription + render) processed in parallel inside this API process.</summary>
    public int MaxConcurrentJobs { get; set; } = 2;

    /// <summary>CPU threads each ffmpeg encode may use. Keeps one render from starving the API; 0 = ffmpeg default.</summary>
    public int FfmpegThreads { get; set; } = 2;

    /// <summary>Hard ceiling on one ffmpeg export (old RENDER_TIMEOUT_SECONDS). A stuck process otherwise leaves "Exporting..." spinning forever.</summary>
    public int RenderTimeoutSeconds { get; set; } = 300;

    public int MediaTokenMinutes { get; set; } = 120;

    public string WhisperModel { get; set; } = "whisper-1";
    public string SarvamModel { get; set; } = "saaras:v4";
    public string SarvamBaseUrl { get; set; } = "https://api.sarvam.ai";
    public string? SarvamApiKey { get; set; }

    /// <summary>Optional override; by default the OpenAI key managed in Admin ▸ LLM Providers is used.</summary>
    public string? OpenAiApiKey { get; set; }
    public string OpenAiBaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>Origins Stripe/Razorpay may send the browser back to (open-redirect protection).</summary>
    public string[] ReturnOrigins { get; set; } = { "http://localhost:3000", "http://localhost:5173" };

    /// <summary>Razorpay subscription length in billing cycles (monthly plan: 12 = one year, renewable).</summary>
    public int RazorpaySubscriptionCycles { get; set; } = 12;

    /// <summary>Absolute fonts directory: as configured, else relative to the app base directory (published output).</summary>
    public string ResolveFontsDir()
    {
        if (Path.IsPathRooted(FontsDir)) return FontsDir;
        var cwd = Path.GetFullPath(FontsDir);
        return Directory.Exists(cwd) ? cwd : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, FontsDir));
    }

    public static string CurrentRuntimeId()
    {
        var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"win-{arch}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return $"osx-{arch}";
        return $"linux-{arch}";
    }

    public string ResolveFfmpegPath()
    {
        if (Path.IsPathRooted(FfmpegPath) && File.Exists(FfmpegPath)) return FfmpegPath;
        var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
        var bundled = Path.Combine(AppContext.BaseDirectory, "Tools", "ffmpeg", CurrentRuntimeId(), exe);
        if (File.Exists(bundled)) return bundled;
        return FfmpegPath; // PATH lookup
    }
}
