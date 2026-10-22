using System.Globalization;

namespace AppBackendBasic.Services.Captions;

/// <summary>Builds the ffmpeg argument lists used for the export render (port of captions.py _render).</summary>
public static class FfmpegRenderArgs
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static (string Preset, string Crf) QualityPreset(string quality) =>
        quality == "draft" ? ("veryfast", "26") : ("slow", "17");

    public static string EscapeFilterPath(string path) => path.Replace('\\', '/').Replace(":", "\\:");

    private static string N(double v) => v.ToString("R", Inv);

    public static string SfxSourceFilter(SfxTone tone, string label)
    {
        double freq = tone.Frequency, sweepTo = tone.SweepTo, duration = tone.Duration;
        var phase = $"2*PI*({N(freq)}+({N(sweepTo)}-{N(freq)})*min(t/{N(duration)},1))*t";
        double attack = Math.Min(0.015, duration / 4);
        double release = duration * 0.6;
        var envelope = $"if(lt(t,{N(attack)}),t/{N(attack)},exp(-5*(t-{N(attack)})/{N(release)}))";
        var expr = $"({envelope})*sin({phase})";
        return $"aevalsrc=exprs='{expr}':s=44100:d={N(duration)}[{label}]";
    }

    public static (List<string> Lines, List<string> Labels) SfxMixFilters(
        IEnumerable<(string Type, double Time)> markers, Func<string, SfxTone> toneFor)
    {
        var lines = new List<string>(); var labels = new List<string>();
        int index = 0;
        foreach (var (type, time) in markers)
        {
            string src = $"sfxsrc{index}", outLabel = $"sfx{index}";
            lines.Add(SfxSourceFilter(toneFor(type), src));
            int delayMs = Math.Max(0, (int)Math.Round(time * 1000, MidpointRounding.ToEven));
            lines.Add($"[{src}]pan=stereo|c0=c0|c1=c0,adelay={delayMs}|{delayMs},volume=0.7[{outLabel}]");
            labels.Add(outLabel);
            index++;
        }
        return (lines, labels);
    }

    public static List<string> Build(string source, string assPath, string output, string fontsDir,
        int audioVolume, bool audioMuted, int brightness, int contrast, int saturation, string quality,
        IEnumerable<(string Type, double Time)> sfxMarkers, Func<string, SfxTone> toneFor)
    {
        // ffmpeg runs from the .ass directory and references both paths relatively so no drive-letter colon is needed.
        var workDir = Path.GetDirectoryName(assPath)!;
        var assName = Path.GetFileName(assPath);
        var fontsRel = Path.GetRelativePath(workDir, fontsDir);
        var subtitles = $"subtitles={EscapeFilterPath(assName)}:fontsdir={EscapeFilterPath(fontsRel)}";
        double volume = audioMuted ? 0 : Math.Max(0, audioVolume) / 100.0;
        // Colour grading runs BEFORE the burn-in so captions stay full-strength regardless of grading.
        var eq = $"eq=brightness={N(brightness / 100.0)}:contrast={N(contrast / 100.0)}:saturation={N(saturation / 100.0)}";
        var videoFilter = $"{eq},{subtitles}";
        var (preset, crf) = QualityPreset(quality);
        var common = new[] { "-c:v", "libx264", "-preset", preset, "-crf", crf, "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", output };

        var (sfxLines, sfxLabels) = SfxMixFilters(sfxMarkers, toneFor);
        var args = new List<string> { "-y", "-i", source };
        if (sfxLabels.Count > 0)
        {
            var mixInputs = string.Concat(new[] { "main_a" }.Concat(sfxLabels).Select(l => $"[{l}]"));
            var filterComplex = $"[0:v]{videoFilter}[vout];[0:a]volume={N(volume)}[main_a];"
                + string.Join(";", sfxLines) + ";"
                + $"{mixInputs}amix=inputs={1 + sfxLabels.Count}:duration=first:dropout_transition=0:normalize=0[aout]";
            args.AddRange(new[] { "-filter_complex", filterComplex, "-map", "[vout]", "-map", "[aout]" });
        }
        else
        {
            args.AddRange(new[] { "-vf", videoFilter, "-af", $"volume={N(volume)}" });
        }
        args.AddRange(common);
        return args;
    }
}
