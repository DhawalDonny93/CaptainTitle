namespace AppBackendBasic.Services.Captions;

/// <summary>
/// The exported frame (Ratio panel) and the white-balance matrix (Color panel). The studio's src/features/captions/lib/frame.ts mirrors this file:
/// keep the sizes, the canvas-height rule and the matrix coefficients identical so the live preview matches the MP4.
/// </summary>
public static class FrameLayout
{
    public const string Original = "original";

    /// <summary>Fixed output sizes per ratio: 1080 on the short side (1920 on the long side for 9:16 / 16:9), the sizes social platforms expect.</summary>
    private static readonly Dictionary<string, (int W, int H)> Sizes = new()
    {
        ["9:16"] = (1080, 1920),
        ["1:1"] = (1080, 1080),
        ["4:5"] = (1080, 1350),
        ["16:9"] = (1920, 1080),
    };

    private static readonly HashSet<string> Fits = new() { "blur", "color", "crop" };

    public static bool IsKnownRatio(string? ratio) => ratio == Original || (ratio != null && Sizes.ContainsKey(ratio));
    public static bool IsKnownFit(string? fit) => fit != null && Fits.Contains(fit);

    /// <summary>The exported frame size. "original" (or an unknown ratio) keeps the source size; a missing source size falls back to 1080x1920.</summary>
    public static (int W, int H) OutputSize(string? ratio, int? sourceWidth, int? sourceHeight)
    {
        if (ratio != null && Sizes.TryGetValue(ratio, out var size)) return size;
        return (sourceWidth ?? 1080, sourceHeight ?? 1920);
    }

    /// <summary>Export dialog resolutions: "original" keeps the frame; the others are the frame's SHORT side in pixels (1080p / 720p / 480p).</summary>
    public static readonly IReadOnlyList<string> Resolutions = new[] { Original, "1080", "720", "480" };

    public static bool IsKnownResolution(string? resolution) => resolution != null && Resolutions.Contains(resolution);

    /// <summary>
    /// The final encoded size: the frame (<see cref="OutputSize"/>) scaled so its short side is the chosen resolution, aspect kept, both sides even
    /// (libx264 with yuv420p needs even dimensions). "original" or an unknown value returns the frame unchanged.
    /// </summary>
    public static (int W, int H) ScaledSize(string? resolution, int frameWidth, int frameHeight)
    {
        if (resolution == null || resolution == Original || !int.TryParse(resolution, out var shortSide) || frameWidth <= 0 || frameHeight <= 0)
            return (frameWidth, frameHeight);
        static int Even(double v) => Math.Max(2, (int)Math.Round(v / 2, MidpointRounding.AwayFromZero) * 2);
        return frameWidth <= frameHeight
            ? (Even(shortSide), Even((double)frameHeight * shortSide / frameWidth))
            : (Even((double)frameWidth * shortSide / frameHeight), Even(shortSide));
    }

    /// <summary>True when the source has to be scaled/cropped/padded into a different frame before the captions are burned in.</summary>
    public static bool Reframes(string? ratio) => ratio != null && Sizes.ContainsKey(ratio);

    /// <summary>
    /// The ASS canvas height for an output frame. Captions are authored on a 1080-wide canvas (font sizes, wrap widths, the preview's frameWidth/1080
    /// scale), so the canvas keeps width 1080 and takes the frame's aspect: 1920 for 9:16 (the historical value), 1080 for 1:1, 608 for 16:9.
    /// Matching the frame's aspect is what stops libass stretching glyphs on non-vertical exports.
    /// </summary>
    public static int CanvasHeight(int outputWidth, int outputHeight) =>
        outputWidth <= 0 || outputHeight <= 0 ? 1920 : (int)Math.Round(1080.0 * outputHeight / outputWidth, MidpointRounding.AwayFromZero);

    /// <summary>Warmth (-50 cool .. 50 warm) and Tint (-50 green .. 50 magenta) as per-channel gains: (red, green, blue).</summary>
    public static (double R, double G, double B) WhiteBalance(int warmth, int tint)
    {
        double w = Math.Clamp(warmth, -50, 50) / 50.0 * 0.12;
        double t = Math.Clamp(tint, -50, 50) / 50.0 * 0.10;
        return (1 + w, 1 - t, 1 - w);
    }
}

/// <summary>The export containers. Both carry the same H.264 video + AAC audio; only the file wrapper (and its MIME type) differs.</summary>
public static class RenderFormats
{
    public const string Mp4 = "mp4";
    public const string Mov = "mov";

    public static bool IsKnown(string? format) => format == Mp4 || format == Mov;

    /// <summary>The format, falling back to mp4 for anything unknown (old rows, hand-edited data).</summary>
    public static string Normalize(string? format) => format == Mov ? Mov : Mp4;

    public static string ContentType(string format) => Normalize(format) == Mov ? "video/quicktime" : "video/mp4";

    /// <summary>The format of a stored render, read from its file extension (renders stored before the format option are .mp4).</summary>
    public static string FromPath(string? path) => string.Equals(Path.GetExtension(path), ".mov", StringComparison.OrdinalIgnoreCase) ? Mov : Mp4;
}
