namespace AppBackendBasic.Services.Captions;

/// <summary>Bound from the "Captions" configuration section. Secrets belong in user-secrets / env vars.</summary>
public class CaptionOptions
{
    public const string SectionName = "Captions";

    /// <summary>Where uploads, previews and renders live. Swap ICaptionStorage for S3/R2 before going multi-node.</summary>
    public string StorageRoot { get; set; } = "App_Data/caption_storage";

    /// <summary>Directory with the bundled .ttf files libass resolves caption fonts from.</summary>
    public string FontsDir { get; set; } = "Assets/captions/fonts";

    /// <summary>ffmpeg executable (must be on PATH, or an absolute path). The old project bundled imageio-ffmpeg.</summary>
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>Caption jobs processed in parallel inside this API process.</summary>
    /// <summary>Absolute fonts directory: as configured, else relative to the app base directory (published output).</summary>
    public string ResolveFontsDir()
    {
        if (Path.IsPathRooted(FontsDir)) return FontsDir;
        var cwd = Path.GetFullPath(FontsDir);
        return Directory.Exists(cwd) ? cwd : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, FontsDir));
    }

    public int MaxConcurrentJobs { get; set; } = 2;

    public int MediaTokenMinutes { get; set; } = 120;

    public string WhisperModel { get; set; } = "whisper-1";
    public string SarvamModel { get; set; } = "saaras:v4";
    public string SarvamBaseUrl { get; set; } = "https://api.sarvam.ai";
    public string? SarvamApiKey { get; set; }

    /// <summary>Optional override; by default the OpenAI key managed in Admin ▸ LLM Providers is used.</summary>
    public string? OpenAiApiKey { get; set; }
    public string OpenAiBaseUrl { get; set; } = "https://api.openai.com/v1";
}
