using System.Globalization;

namespace AppBackendBasic.Services.Captions;

/// <summary>The blur-mask companion scripts of one export (see <see cref="AssScripts"/>) as file names relative to the ffmpeg working directory.</summary>
public sealed record BlurMaskFiles(string? ShadowAlpha, string? ShadowColor, double ShadowBlur, string? GlowAlpha, string? GlowColor, double GlowBlur);

/// <summary>Builds the ffmpeg argument lists used for the export render (port of captions.py _render).</summary>
public static class FfmpegRenderArgs
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>
    /// gblur sigma is real output pixels while Shadow/Glow blur is authored at the 1080-wide canvas scale, and a browser's CSS blur radius is not quite
    /// gblur's sigma: measured empirically (a 20px CSS blur and sigma 22 gave matching edge spread) so ~1.1x reproduces the live preview's softness.
    /// </summary>
    public const double SigmaToCssBlurRadius = 1.1;

    /// <summary>"high" keeps the same near-lossless CRF as the old app but uses medium (not slow) so a real-length export does not feel hung.</summary>
    public static (string Preset, string Crf) QualityPreset(string quality) =>
        quality == "draft" ? ("veryfast", "26") : ("medium", "17");

    public static string EscapeFilterPath(string path) => path.Replace('\\', '/').Replace(":", "\\:");

    private static string N(double v) => v.ToString("R", Inv);

    private static string ToneBurst(double freq, double duration, double attack = 0.01, double decay = 6)
    {
        var phase = $"2*PI*{N(freq)}*t";
        var envelope = $"if(lt(t,{N(attack)}),t/{N(attack)},exp(-{N(decay)}*(t-{N(attack)})/{N(duration)}))";
        return $"({envelope})*sin({phase})";
    }

    /// <summary>
    /// A standalone ffmpeg lavfi chain (no -i input) synthesizing one SFX marker's placeholder sound, ending in output pad [label]. whoosh / pop / shutter
    /// are filtered white noise (a tone sweep reads as a generic beep); bass_drop is a pitch-dropping sine and cash two bell notes, as in the old app.
    /// Any other (admin-defined) SFX type keeps the previous generic behaviour: a tone sweep from its configured frequency / sweep / duration.
    /// </summary>
    public static string SfxSourceFilter(SfxTone tone, string label)
    {
        switch (tone.Key)
        {
            case "whoosh":
                return $"anoisesrc=d=0.3:c=white:r=44100:a=0.6,bandpass=f=1100:width_type=h:w=900,afade=t=in:d=0.02,afade=t=out:st=0.08:d=0.22:curve=exp,volume=1.3[{label}]";
            case "pop":
                return $"anoisesrc=d=0.08:c=white:r=44100:a=0.5,bandpass=f=2200:width_type=h:w=1800,afade=t=in:d=0.002,afade=t=out:st=0.012:d=0.068:curve=exp,volume=1.6[{label}]";
            case "shutter":
                return $"anoisesrc=d=0.05:c=white:r=44100:a=0.5,highpass=f=3500,afade=t=in:d=0.001,afade=t=out:st=0.006:d=0.044:curve=exp,volume=1.3[{label}]";
            case "cash":
            {
                string noteA = $"{label}_a", noteB = $"{label}_b";
                return $"aevalsrc=exprs='{ToneBurst(880, 0.15)}':s=44100:d=0.15[{noteA}];"
                    + $"aevalsrc=exprs='{ToneBurst(1320, 0.15)}':s=44100:d=0.15[{noteB}];"
                    + $"[{noteB}]adelay=70|70[{noteB}d];"
                    + $"[{noteA}][{noteB}d]amix=inputs=2:duration=longest:normalize=0,volume=0.6[{label}]";
            }
            case "bass_drop":
                return "aevalsrc=exprs='(if(lt(t,0.015),t/0.015,exp(-5*(t-0.015)/0.24)))*sin(2*PI*(90+(27-90)*min(t/0.4,1))*t)':s=44100:d=0.4,asoftclip=type=tanh:param=0.7,volume=1.3[" + label + "]";
            default:
            {
                double freq = tone.Frequency, sweepTo = tone.SweepTo, duration = tone.Duration;
                var phase = $"2*PI*({N(freq)}+({N(sweepTo)}-{N(freq)})*min(t/{N(duration)},1))*t";
                double attack = Math.Min(0.015, duration / 4);
                double release = duration * 0.6;
                var envelope = $"if(lt(t,{N(attack)}),t/{N(attack)},exp(-5*(t-{N(attack)})/{N(release)}))";
                return $"aevalsrc=exprs='({envelope})*sin({phase})':s=44100:d={N(duration)}[{label}]";
            }
        }
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

    /// <summary>
    /// filter_complex steps for one genuinely smooth blurred Shadow/Glow layer: a grey copy of the glyphs (brightness = layer opacity) is burned onto an
    /// opaque black canvas and blurred with gblur; its luma then becomes the alpha channel (alphamerge) of an unblurred full-colour pass, so only the
    /// gating shape is blurred. (Burning an .ass straight onto a transparent canvas does not reliably produce an alpha plane.)
    /// </summary>
    public static (List<string> Steps, string Label) BlurMaskSteps(string prefix, string alphaRel, string colorRel, double blurPx, int width, int height, string fontsRel)
    {
        var sigma = Math.Max(0.1, blurPx);
        return (new List<string>
        {
            $"color=c=black:s={width}x{height}:r=25[{prefix}_abg]",
            $"[{prefix}_abg]ass={EscapeFilterPath(alphaRel)}:fontsdir={EscapeFilterPath(fontsRel)},gblur=sigma={N(sigma)}[{prefix}_amask]",
            $"color=c=black:s={width}x{height}:r=25[{prefix}_cbg]",
            $"[{prefix}_cbg]ass={EscapeFilterPath(colorRel)}:fontsdir={EscapeFilterPath(fontsRel)}[{prefix}_cmask]",
            $"[{prefix}_cmask][{prefix}_amask]alphamerge[{prefix}_layer]",
        }, $"{prefix}_layer");
    }

    public static List<string> Build(string source, string assPath, string output, string fontsDir,
        int audioVolume, bool audioMuted, int brightness, int contrast, int saturation, string quality,
        IEnumerable<(string Type, double Time)> sfxMarkers, Func<string, SfxTone> toneFor,
        int threads = 0, (string Preset, string Crf)? encode = null,
        BlurMaskFiles? masks = null, int? videoWidth = null, int? videoHeight = null, bool hasAudio = true)
    {
        // ffmpeg runs from the .ass directory and references every path relatively so no drive-letter colon is needed.
        var workDir = Path.GetDirectoryName(assPath)!;
        var assName = Path.GetFileName(assPath);
        var fontsRel = Path.GetRelativePath(workDir, fontsDir);
        var subtitles = $"subtitles={EscapeFilterPath(assName)}:fontsdir={EscapeFilterPath(fontsRel)}";
        double volume = audioMuted ? 0 : Math.Max(0, audioVolume) / 100.0;
        // Colour grading runs BEFORE the burn-in so captions stay full-strength regardless of grading.
        var eq = $"eq=brightness={N(brightness / 100.0)}:contrast={N(contrast / 100.0)}:saturation={N(saturation / 100.0)}";
        // Defaults reproduce the old app (high = medium/CRF 17). Admin settings can pass cheaper presets; threads caps CPU use.
        var (preset, crf) = encode ?? QualityPreset(quality);
        var common = new List<string> { "-c:v", "libx264", "-preset", preset, "-crf", crf };
        if (threads > 0) { common.Add("-threads"); common.Add(threads.ToString(Inv)); }
        if (hasAudio) common.AddRange(new[] { "-c:a", "aac", "-b:a", "192k" });
        common.AddRange(new[] { "-movflags", "+faststart", output });

        int width = videoWidth ?? 1080, height = videoHeight ?? 1920;
        double canvasScale = width / 1080.0 * SigmaToCssBlurRadius;
        var videoSteps = new List<string> { $"[0:v]{eq}[graded]" };
        var current = "graded";
        // Glow is composited further back than Shadow so a caption can combine a tight shadow for grounding with a wide glow for pop.
        // shortest=1: the mask layers come from an unbounded `color` source, so without it overlay would run for the longer (infinite) input.
        if (masks?.GlowAlpha != null && masks.GlowColor != null)
        {
            var (steps, label) = BlurMaskSteps("glow", masks.GlowAlpha, masks.GlowColor, masks.GlowBlur * canvasScale, width, height, fontsRel);
            videoSteps.AddRange(steps);
            videoSteps.Add($"[{current}][{label}]overlay=0:0:shortest=1[with_glow]");
            current = "with_glow";
        }
        if (masks?.ShadowAlpha != null && masks.ShadowColor != null)
        {
            var (steps, label) = BlurMaskSteps("shadow", masks.ShadowAlpha, masks.ShadowColor, masks.ShadowBlur * canvasScale, width, height, fontsRel);
            videoSteps.AddRange(steps);
            videoSteps.Add($"[{current}][{label}]overlay=0:0:shortest=1[with_shadow]");
            current = "with_shadow";
        }
        videoSteps.Add($"[{current}]{subtitles}[vout]");

        var (sfxLines, sfxLabels) = SfxMixFilters(sfxMarkers, toneFor);
        var audioSteps = new List<string>();
        if (hasAudio)
        {
            if (sfxLabels.Count > 0)
            {
                // SFX are a separate accent layer: normalize=0 stops amix dividing every input by 1/N, so the dialogue keeps its authored volume.
                var mixInputs = string.Concat(new[] { "main_a" }.Concat(sfxLabels).Select(l => $"[{l}]"));
                audioSteps.Add($"[0:a]volume={N(volume)}[main_a]");
                audioSteps.AddRange(sfxLines);
                audioSteps.Add($"{mixInputs}amix=inputs={1 + sfxLabels.Count}:duration=first:dropout_transition=0:normalize=0[aout]");
            }
            else audioSteps.Add($"[0:a]volume={N(volume)}[aout]");
        }

        var args = new List<string> { "-y", "-i", source, "-filter_complex", string.Join(";", videoSteps.Concat(audioSteps)), "-map", "[vout]" };
        if (hasAudio) { args.Add("-map"); args.Add("[aout]"); }
        args.AddRange(common);
        return args;
    }
}
