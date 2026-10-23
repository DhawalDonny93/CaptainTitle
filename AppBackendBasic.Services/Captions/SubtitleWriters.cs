using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Services.Captions;

/// <summary>Project-wide text styling that reaches the export (old ProjectEdit fields).</summary>
public sealed record AssStyleOptions
{
    public string ScriptMode { get; init; } = "roman";
    public bool Bold { get; init; } = true;
    public bool Italic { get; init; }
    public bool OutlineEnabled { get; init; }
    public double OutlineWidth { get; init; } = 2;
    public string OutlineColor { get; init; } = "#000000";
    public bool ShadowEnabled { get; init; } = true;
    public double ShadowAngle { get; init; }
    public double ShadowDistance { get; init; } = 2;
    public double ShadowBlur { get; init; } = 2;
    public double ShadowOpacity { get; init; } = 0.5;
    public string ShadowColor { get; init; } = "#000000";
    public bool GlowEnabled { get; init; }
    public string? GlowColor { get; init; }
    public double GlowBlur { get; init; } = 8;
    public double GlowOpacity { get; init; } = 0.6;
    public bool BackgroundEnabled { get; init; }
    public string BackgroundColor { get; init; } = "#000000";
    public double BackgroundOpacity { get; init; } = 0.6;
}

/// <summary>
/// The ASS scripts for one export. <see cref="Main"/> is the crisp caption layer burned with the subtitles filter; the four optional mask scripts
/// are tiny companion .ass files burned onto black canvases and blurred with ffmpeg's gblur to get a genuinely smooth Shadow / Glow
/// (libass' own \blur is much coarser). A mask script is null when its layer produced no content.
/// </summary>
public sealed record AssScripts(string Main, string? ShadowAlpha, string? ShadowColor, string? GlowAlpha, string? GlowColor);

/// <summary>Builds the ASS subtitle scripts burned into the export (port of captions.py _write_ass and its helpers).</summary>
public static class AssSubtitleWriter
{
    public const string DefaultEmphasizeAccent = "#EAB308";
    /// <summary>None of the Latin display fonts carry Devanagari glyphs; native-script text is forced onto this font for glyph coverage.</summary>
    public const string DevanagariFont = "Noto Sans Devanagari";
    public static readonly IReadOnlySet<string> DevanagariCapableFonts = new HashSet<string>
    {
        DevanagariFont, "Noto Sans Devanagari Black", "Baloo 2 ExtraBold", "Hind", "Mukta ExtraBold", "Rozha One", "Yatra One", "Tiro Devanagari Hindi", "Kalam",
    };
    /// <summary>Only these bundled families have a genuine second, bolder weight file - everything else would be synthetically emboldened (and differ from the browser preview).</summary>
    public static readonly IReadOnlySet<string> BoldCapableFonts = new HashSet<string> { "Poppins", "Caveat" };

    // "Active word" pop timing/scale - mirrors .caption-overlay span.active's scale(1.1) animated over the word's own window.
    public const int PopInMs = 120;
    public const int PopOutMs = 150;
    public const int PopScale = 112;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly Regex DevanagariRegex = new("[ऀ-ॿ]", RegexOptions.Compiled);

    public static bool HasDevanagari(string? text) => DevanagariRegex.IsMatch(text ?? "");

    public static string AssTime(double seconds)
    {
        int hours = (int)(seconds / 3600);
        int minutes = (int)((seconds % 3600) / 60);
        double secs = seconds % 60;
        return $"{hours}:{minutes:00}:{secs.ToString("00.00", Inv)}";
    }

    public static string AssColor(string hex)
    {
        var value = hex.TrimStart('#').PadRight(6, 'F');
        value = value.Substring(0, 6);
        return $"&H00{value.Substring(4, 2)}{value.Substring(2, 2)}{value.Substring(0, 2)}&";
    }

    /// <summary>Same as <see cref="AssColor"/> with an explicit alpha. ASS alpha is inverted (00 = opaque, FF = transparent).</summary>
    public static string AssColorAlpha(string hex, double opacity = 1.0)
    {
        var value = hex.TrimStart('#').PadRight(6, 'F');
        value = value.Substring(0, 6);
        int alpha = PyRound((1 - Math.Max(0.0, Math.Min(1.0, opacity))) * 255);
        return $"&H{alpha:X2}{value.Substring(4, 2)}{value.Substring(2, 2)}{value.Substring(0, 2)}&";
    }

    /// <summary>Clockwise-from-straight-down angle + distance -> ASS \xshad/\yshad pixel offsets (0 = down, like CSS text-shadow's default direction).</summary>
    public static (double X, double Y) ShadowOffset(double angleDeg, double distance)
    {
        var radians = angleDeg * Math.PI / 180;
        return (distance * Math.Sin(radians), distance * Math.Cos(radians));
    }

    private static int PyRound(double v) => (int)Math.Round(v, MidpointRounding.ToEven);
    private static string N(double v) => v.ToString("R", Inv);
    private static string G(double v) => v.ToString("G6", Inv);
    private static string Grey(double opacity) { var g = PyRound(Math.Max(0.0, Math.Min(1.0, opacity)) * 255); return $"{g:X2}{g:X2}{g:X2}"; }

    private const string StyleFormat =
        "Format: Name,Fontname,Fontsize,PrimaryColour,SecondaryColour,OutlineColour,BackColour,Bold,Italic,Underline,StrikeOut,ScaleX,ScaleY,Spacing,Angle,BorderStyle,Outline,Shadow,Alignment,MarginL,MarginR,MarginV,Encoding\n";
    private const string EventsFormat = "[Events]\nFormat: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text\n";

    public static AssScripts Build(IReadOnlyList<CaptionWordDto> words, string themeKey, string fontId, int? wordsPerCaption,
        int captionLines, string captionPosition, IReadOnlyList<CaptionElementDto>? elements, AssStyleOptions style,
        CaptionStyleSet styles, ITextMeasurer measurer)
    {
        var theme = styles.Theme(themeKey);
        var primaryFamily = styles.Font(fontId).Family;
        var tiers = theme.Tiers;
        var scriptMode = style.ScriptMode;

        // Background and Outline/Shadow are mutually exclusive: BorderStyle (ASS's box-vs-outline switch) is a Style-level property, not per-word.
        bool useBackground = style.BackgroundEnabled;
        bool useShadowLayer = !useBackground && style.ShadowEnabled;
        var (shadowX, shadowY) = useShadowLayer ? ShadowOffset(style.ShadowAngle, style.ShadowDistance) : (0.0, 0.0);
        bool useGlowLayer = !useBackground && style.GlowEnabled;
        int italicField = style.Italic ? -1 : 0;

        string borderStyle, outlineField, shadowField, outlineColourField, backColourField;
        if (useBackground)
        {
            // BorderStyle=3 ("opaque box"): libass sizes the box from each line's own text metrics; Outline is the padding, BackColour the fill.
            borderStyle = "3"; outlineField = "14"; shadowField = "0";
            var boxFill = AssColorAlpha(style.BackgroundColor, style.BackgroundOpacity).TrimEnd('&');
            outlineColourField = boxFill; backColourField = boxFill;
        }
        else
        {
            borderStyle = "1"; outlineField = "0"; shadowField = "0";
            outlineColourField = "&H00000000"; backColourField = "&H80000000";
        }
        // WrapStyle 2: no automatic word-wrap, only our own explicit \N breaks.
        var header = "[Script Info]\nScriptType: v4.00+\nPlayResX: 1080\nPlayResY: 1920\nWrapStyle: 2\n[V4+ Styles]\n" + StyleFormat
            + $"Style: Default,{primaryFamily},52,&H00FFFFFF,&H00007AFF,{outlineColourField},{backColourField},0,{italicField},0,0,100,100,0,0,{borderStyle},{outlineField},{shadowField},2,70,70,210,1\n"
            + EventsFormat;

        var lines = new StringBuilder(header);
        var shadowAlphaLines = new StringBuilder(); var shadowColorLines = new StringBuilder();
        var glowAlphaLines = new StringBuilder(); var glowColorLines = new StringBuilder();

        // Both fixed templates need several words flowing together per cue; the generic 3-word cap / 0.35s min-display would cut them off at one word.
        bool isFixed = FixedTemplates.IsFixed(themeKey);
        int defaultMaxWords = isFixed ? 6 : 3;
        double minDisplay = isFixed ? 1.5 : 0.35;
        var groups = CaptionLayout.GroupWordsForDisplay(words, minDisplay, wordsPerCaption ?? defaultMaxWords, strictCount: wordsPerCaption != null);

        foreach (var group in groups)
        {
            var (centerX, centerY) = CaptionLayout.GroupCenter(group, captionPosition);
            // Street Glow is LINE-based (two independently animated roles), so it never goes through the word-tag path below. Anything without
            // tiers falls back to it too, so a legacy/unknown flat template on an older project still renders instead of crashing.
            if (tiers == null)
            {
                EmitStreetGlowGroup(lines, shadowAlphaLines, shadowColorLines, glowAlphaLines, glowColorLines, group, centerX, centerY, scriptMode);
                continue;
            }

            double rotation = CaptionLayout.GroupRotation(group);
            // Doodle Marker uses ONE font size for every tier, so words flow on one line instead of breaking at every tier change.
            var breakTiers = themeKey == FixedTemplates.DoodleMarker ? null : tiers;
            var breaks = CaptionLayout.ResolveLineBreaks(group, breakTiers, captionLines);
            var rows = CaptionLayout.SplitRows(group, breaks);

            // (primary, shadowAlpha, shadowColor, glowAlpha, glowColor)
            (string Primary, string? ShadowAlpha, string? ShadowColor, string? GlowAlpha, string? GlowColor) WordTags(CaptionWordDto word, string displayText)
            {
                var tier = tiers.ContainsKey(word.Emphasis) ? word.Emphasis : "support";
                var family = tiers[tier].Font;
                // Glyph coverage overrides style choice - but only when it has to (the family already chosen may already cover Devanagari).
                if (HasDevanagari(displayText) && !DevanagariCapableFonts.Contains(family)) family = DevanagariFont;
                var color = word.Color ?? "#FFFFFF";
                int fontSize = word.FontSize;
                // "FOR THIS MOMENT" Emphasize toggle: fixed accent highlight plus a size bump.
                if (word.Emphasize) { color = DefaultEmphasizeAccent; fontSize = PyRound(fontSize * 1.18); }
                // \fscx100\fscy100 explicit so each word starts its own pop animation from a clean baseline (override tags carry forward).
                var baseTags = $"\\fn{family}\\fs{fontSize}\\fscx100\\fscy100";
                var boldTag = style.Bold && BoldCapableFonts.Contains(family) ? "\\b1" : "";
                int relOn = Math.Max(0, PyRound((word.Start - group[0].Start) * 1000));
                int relOff = Math.Max(relOn + 1, PyRound((word.End - group[0].Start) * 1000));
                int popInEnd = Math.Min(relOff, relOn + PopInMs);
                int popOutStart = Math.Max(popInEnd, relOff - PopOutMs);
                var popScaleOnly = $"\\t({relOn},{popInEnd},\\fscx{PopScale}\\fscy{PopScale})\\t({popOutStart},{relOff},\\fscx100\\fscy100)";

                if (useBackground)
                    return ($"{baseTags}\\c{AssColor(color)}{boldTag}" + popScaleOnly, null, null, null, null);

                // Outline stays crisp and unblurred; Shadow/Glow are built as separate blur-mask files, not ASS override tags.
                var outlineTags = style.OutlineEnabled ? $"\\3c{AssColor(style.OutlineColor)}\\bord{N(style.OutlineWidth)}" : "\\bord0";
                var primary = $"{baseTags}\\c{AssColor(color)}{boldTag}{outlineTags}\\shad0\\blur0" + popScaleOnly;
                var maskBase = $"{baseTags}{boldTag}\\bord0\\shad0\\blur0" + popScaleOnly;

                string? shadowAlphaTags = null, shadowColorTags = null, glowAlphaTags = null, glowColorTags = null;
                if (useShadowLayer)
                {
                    shadowAlphaTags = $"\\c&H{Grey(style.ShadowOpacity)}&{maskBase}";
                    shadowColorTags = $"\\c{AssColor(style.ShadowColor)}{maskBase}";
                }
                if (useGlowLayer)
                {
                    glowAlphaTags = $"\\c&H{Grey(style.GlowOpacity)}&{maskBase}";
                    // glow_color null = this word's own colour (a self-coloured neon glow), resolved per word.
                    glowColorTags = $"\\c{AssColor(style.GlowColor ?? color)}{maskBase}";
                }
                return (primary, shadowAlphaTags, shadowColorTags, glowAlphaTags, glowColorTags);
            }

            // One Dialogue per caption group, rows joined by \N, so libass stacks lines with the font's native metrics - the same thing the
            // browser preview's CSS line-height relies on (two hand-rolled approximations drifted apart).
            var wordParts = new StringBuilder();
            var shadowAlphaParts = new StringBuilder(); var shadowColorParts = new StringBuilder();
            var glowAlphaParts = new StringBuilder(); var glowColorParts = new StringBuilder();
            bool firstWord = true;
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                if (rowIndex > 0)
                {
                    wordParts.Append("\\N");
                    if (useShadowLayer) { shadowAlphaParts.Append("\\N"); shadowColorParts.Append("\\N"); }
                    if (useGlowLayer) { glowAlphaParts.Append("\\N"); glowColorParts.Append("\\N"); }
                }
                var row = rows[rowIndex];
                for (int index = 0; index < row.Count; index++)
                {
                    var word = row[index];
                    var displayText = scriptMode == "native" && !string.IsNullOrEmpty(word.Original) ? word.Original : word.Text;
                    var safe = displayText.Replace("{", "").Replace("}", "");
                    var (primaryTags, shadowAlphaTags, shadowColorTags, glowAlphaTags, glowColorTags) = WordTags(word, displayText);
                    if (firstWord)
                    {
                        // \org pins the rotation pivot to the whole block's centre so a multi-row caption rotates as one rigid block.
                        var frz = rotation != 0 ? $"\\org({centerX},{centerY})\\frz{G(rotation)}" : "";
                        primaryTags = $"\\an5\\pos({centerX},{centerY}){frz}" + primaryTags;
                        if (shadowAlphaTags != null)
                        {
                            int shadowCx = PyRound(centerX + shadowX), shadowCy = PyRound(centerY + shadowY);
                            var shadowFrz = rotation != 0 ? $"\\org({shadowCx},{shadowCy})\\frz{G(rotation)}" : "";
                            var shadowPos = $"\\an5\\pos({shadowCx},{shadowCy}){shadowFrz}";
                            shadowAlphaTags = shadowPos + shadowAlphaTags;
                            shadowColorTags = shadowPos + shadowColorTags;
                        }
                        if (glowAlphaTags != null)
                        {
                            // No offset - glow sits directly behind the primary glyph.
                            var glowPos = $"\\an5\\pos({centerX},{centerY}){frz}";
                            glowAlphaTags = glowPos + glowAlphaTags;
                            glowColorTags = glowPos + glowColorTags;
                        }
                        firstWord = false;
                    }
                    var separator = index > 0 ? " " : "";
                    wordParts.Append($"{separator}{{{primaryTags}}}{safe}");
                    if (shadowAlphaTags != null)
                    {
                        shadowAlphaParts.Append($"{separator}{{{shadowAlphaTags}}}{safe}");
                        shadowColorParts.Append($"{separator}{{{shadowColorTags}}}{safe}");
                    }
                    if (glowAlphaTags != null)
                    {
                        glowAlphaParts.Append($"{separator}{{{glowAlphaTags}}}{safe}");
                        glowColorParts.Append($"{separator}{{{glowColorTags}}}{safe}");
                    }
                }
            }
            var dialogueTime = $"{AssTime(group[0].Start)},{AssTime(group[^1].End)}";
            lines.Append($"Dialogue: 0,{dialogueTime},Default,,0,0,0,,{wordParts}\n");
            if (shadowAlphaParts.Length > 0)
            {
                shadowAlphaLines.Append($"Dialogue: 0,{dialogueTime},Default,,0,0,0,,{shadowAlphaParts}\n");
                shadowColorLines.Append($"Dialogue: 0,{dialogueTime},Default,,0,0,0,,{shadowColorParts}\n");
            }
            if (glowAlphaParts.Length > 0)
            {
                glowAlphaLines.Append($"Dialogue: 0,{dialogueTime},Default,,0,0,0,,{glowAlphaParts}\n");
                glowColorLines.Append($"Dialogue: 0,{dialogueTime},Default,,0,0,0,,{glowColorParts}\n");
            }
            if (themeKey == FixedTemplates.DoodleMarker)
                EmitDoodleDecorations(lines, group, rows, centerX, centerY, scriptMode, measurer);
        }

        foreach (var element in elements ?? Array.Empty<CaptionElementDto>())
            if (element.End > element.Start) lines.Append(ElementDialogue(element, styles));

        // Mask files are only produced when their layer actually has content, so the renderer builds only the blur stages it needs.
        var maskHeader = "[Script Info]\nScriptType: v4.00+\nPlayResX: 1080\nPlayResY: 1920\n[V4+ Styles]\n" + StyleFormat
            + $"Style: Default,{primaryFamily},52,&H00FFFFFF,&H00007AFF,&H00000000,&H00000000,0,{italicField},0,0,100,100,0,0,1,0,0,2,70,70,210,1\n"
            + EventsFormat;
        string? Mask(StringBuilder sb) => sb.Length > 0 ? maskHeader + sb : null;
        return new AssScripts(lines.ToString(), Mask(shadowAlphaLines), Mask(shadowColorLines), Mask(glowAlphaLines), Mask(glowColorLines));
    }

    private static string ElementDialogue(CaptionElementDto element, CaptionStyleSet styles)
    {
        var safe = (element.Char ?? "").Replace("{", "").Replace("}", "");
        int x = PyRound(element.PosX / 100 * 1080);
        int y = PyRound(element.PosY / 100 * 1920);
        // Matches .element-overlay's CSS text-shadow (0 2px 6px rgba(0,0,0,.7)) rather than inheriting the Style line's Outline/Shadow.
        var tags = $"\\an5\\pos({x},{y})\\fs{element.FontSize}\\c{AssColor(element.Color ?? "#FFFFFF")}\\bord0\\shad2\\blur3";
        if (element.Kind == "text") tags = $"\\fn{styles.Font(element.FontId).Family}" + tags;
        // Standard z-index staging only (no subject matting): "behind" draws behind the caption text (Layer 0), "front" above it.
        int layer = element.Layer == "behind" ? -1 : 2;
        return $"Dialogue: {layer},{AssTime(element.Start)},{AssTime(element.End)},Default,,0,0,0,,{{{tags}}}{safe}\n";
    }

    // ---------------------------------------------------------------- Street Glow

    private static (string Alpha, string Color) MaskDialoguePair(string tags, string text, string dialogueTime, double opacity, string color) =>
        ($"Dialogue: 0,{dialogueTime},Default,,0,0,0,,{{{tags}\\c&H{Grey(opacity)}&}}{text}\n",
         $"Dialogue: 0,{dialogueTime},Default,,0,0,0,,{{{tags}\\c{AssColor(color)}}}{text}\n");

    private static string StreetGlowText(IEnumerable<CaptionWordDto> words, string scriptMode, bool upper) =>
        string.Join(" ", words.Select(w =>
        {
            var text = scriptMode == "native" && !string.IsNullOrEmpty(w.Original) ? w.Original : w.Text;
            text = text.Replace("{", "").Replace("}", "");
            return upper ? text.ToUpperInvariant() : text;
        }));

    /// <summary>
    /// Street Glow: each of its two lines independently animates (line1 hard-pops, line2 slides up and fades in), which ASS can only express with
    /// each line as its OWN Dialogue. Vertical placement is a fixed, hand-picked pair of offsets (not derived from font metrics).
    /// </summary>
    private static void EmitStreetGlowGroup(StringBuilder lines, StringBuilder shadowAlpha, StringBuilder shadowColor,
        StringBuilder glowAlpha, StringBuilder glowColor, IReadOnlyList<CaptionWordDto> group, int centerX, int centerY, string scriptMode)
    {
        var rows = CaptionLayout.SplitRows(group, CaptionLayout.ResolveLineBreaks(group, null, 2));
        var line1Words = rows[0];
        var line2Words = rows.Count > 1 ? rows[1] : new List<CaptionWordDto>();
        int row2Height = line2Words.Count > 0 ? FixedTemplates.StreetGlowLine2Size : 0;
        int gap = line2Words.Count > 0 ? FixedTemplates.StreetGlowGapPx : 0;
        int totalHeight = FixedTemplates.StreetGlowLine1Size + row2Height + gap;
        double top = centerY - totalHeight / 2.0;
        int cy1 = PyRound(top + FixedTemplates.StreetGlowLine1Size / 2.0);
        int cy2 = PyRound(top + FixedTemplates.StreetGlowLine1Size + gap + row2Height / 2.0);
        var dialogueTime = $"{AssTime(group[0].Start)},{AssTime(group[^1].End)}";

        var line1Text = StreetGlowText(line1Words, scriptMode, upper: true);
        var line1Geometry = $"\\an5\\pos({centerX},{cy1})\\fn{FixedTemplates.StreetGlowLine1Font}\\fs{FixedTemplates.StreetGlowLine1Size}"
            + "\\fscx100\\fscy100\\bord0\\shad0\\blur0\\t(0,160,\\fscx108\\fscy108)\\t(160,240,\\fscx100\\fscy100)";
        lines.Append($"Dialogue: 0,{dialogueTime},Default,,0,0,0,,{{{line1Geometry}\\c{AssColor(FixedTemplates.StreetGlowLine1Color)}}}{line1Text}\n");
        // Line 1's own always-on neon glow, hardcoded to this template's red (it owns its look, not the project's Shadow/Glow panel).
        var glow = MaskDialoguePair(line1Geometry, line1Text, dialogueTime, FixedTemplates.StreetGlowGlowOpacity, FixedTemplates.StreetGlowLine1Color);
        glowAlpha.Append(glow.Alpha); glowColor.Append(glow.Color);

        if (line2Words.Count == 0) return;
        var line2Text = StreetGlowText(line2Words, scriptMode, upper: false);
        int moveFromY = cy2 + 18;
        var line2Geometry = $"\\an5\\move({centerX},{moveFromY},{centerX},{cy2},0,220)\\fad(120,80)"
            + $"\\fn{FixedTemplates.StreetGlowLine2Font}\\fs{FixedTemplates.StreetGlowLine2Size}\\fscx100\\fscy100\\bord0\\shad0\\blur0";
        lines.Append($"Dialogue: 0,{dialogueTime},Default,,0,0,0,,{{{line2Geometry}\\c{AssColor(FixedTemplates.StreetGlowLine2Color)}}}{line2Text}\n");
        var shadow = MaskDialoguePair(line2Geometry, line2Text, dialogueTime, FixedTemplates.StreetGlowShadowOpacity, "#000000");
        shadowAlpha.Append(shadow.Alpha); shadowColor.Append(shadow.Color);
    }

    // ---------------------------------------------------------------- Doodle Marker

    private static int DoodleRowCenterY(int centerY, int rowIndex, int numRows) =>
        PyRound(centerY + (rowIndex - (numRows - 1) / 2.0) * FixedTemplates.DoodleFontSize * FixedTemplates.DoodleNativeLineHeight);

    /// <summary>Filled, asymmetric rounded blob (ASS \p drawing) approximating the spec's organic CSS border-radius; the left edge stays at -hw so stepping rightX up reads as growing left-to-right.</summary>
    internal static string DoodlePillPath(double hw, double hh, double rightX)
    {
        double grown = Math.Max(1.0, rightX - (-hw));
        double rTl = hh * 0.55, rBl = hh * 0.22;
        double rTr = Math.Min(hh * 0.18, grown / 2), rBr = Math.Min(hh * 0.5, grown / 2);
        int R(double v) => PyRound(v);
        return $"m {R(-hw + rTl)} {R(-hh)} "
            + $"l {R(rightX - rTr)} {R(-hh)} "
            + $"b {R(rightX)} {R(-hh)} {R(rightX)} {R(-hh)} {R(rightX)} {R(-hh + rTr)} "
            + $"l {R(rightX)} {R(hh - rBr)} "
            + $"b {R(rightX)} {R(hh)} {R(rightX)} {R(hh)} {R(rightX - rBr)} {R(hh)} "
            + $"l {R(-hw + rBl)} {R(hh)} "
            + $"b {R(-hw)} {R(hh)} {R(-hw)} {R(hh)} {R(-hw)} {R(hh - rBl)} "
            + $"l {R(-hw)} {R(-hh + rTl)} "
            + $"b {R(-hw)} {R(-hh)} {R(-hw)} {R(-hh)} {R(-hw + rTl)} {R(-hh)}";
    }

    /// <summary>Hand-drawn-looking, deliberately not-quite-closed ellipse ring (stroked, not filled), re-projected from the spec's SVG path (viewBox 100x40).</summary>
    internal static string DoodleLoopPath(double hw, double hh)
    {
        double sx = hw / 50, sy = hh / 20;
        int R(double v) => PyRound(v);
        return $"m {R(-40 * sx)} 0 "
            + $"b {R(-40 * sx)} {R(-12 * sy)} {R(40 * sx)} {R(-14 * sy)} {R(42 * sx)} 0 "
            + $"b {R(44 * sx)} {R(14 * sy)} {R(-42 * sx)} {R(16 * sy)} {R(-36 * sx)} {R(-2 * sy)}";
    }

    /// <summary>
    /// Pill (key) / loop (feature) decorations behind the already-emitted text Dialogue (Layer -1), stepped in over a few short Dialogues to read as a
    /// draw-on. Each word's position is estimated from real glyph-width measurement against the same bundled font libass renders plus a row-height estimate.
    /// </summary>
    private static void EmitDoodleDecorations(StringBuilder lines, IReadOnlyList<CaptionWordDto> group, List<List<CaptionWordDto>> rows,
        int centerX, int centerY, string scriptMode, ITextMeasurer measurer)
    {
        int numRows = rows.Count;
        int size = FixedTemplates.DoodleFontSize;
        for (int rowIndex = 0; rowIndex < numRows; rowIndex++)
        {
            var row = rows[rowIndex];
            int rowCy = DoodleRowCenterY(centerY, rowIndex, numRows);
            var texts = row.Select(w => scriptMode == "native" && !string.IsNullOrEmpty(w.Original) ? w.Original : w.Text).ToList();
            var widths = texts.Select(t => measurer.MeasureWidth(t, FixedTemplates.DoodleFont, size)).ToList();
            double spaceW = measurer.MeasureWidth(" ", FixedTemplates.DoodleFont, size);
            if (spaceW == 0) spaceW = size * 0.28;
            double rowWidth = widths.Sum() + spaceW * Math.Max(0, row.Count - 1);
            double cursor = centerX - rowWidth / 2;
            for (int i = 0; i < row.Count; i++)
            {
                var word = row[i];
                double width = widths[i];
                int wordCx = PyRound(cursor + width / 2);
                cursor += width + spaceW;
                var tier = word.Emphasis == "key" || word.Emphasis == "feature" ? word.Emphasis : "support";
                if (tier != "key" && tier != "feature") continue;
                var wordEnd = AssTime(group[^1].End);
                double hw, hh, revealMs; string styleTags;
                if (tier == "key")
                {
                    hw = width / 2 + 14; hh = size * 0.42; revealMs = 180;
                    styleTags = $"\\1a&H00&\\1c{AssColor(FixedTemplates.DoodlePillColor)}\\3a&HFF&\\bord0\\shad0\\blur0";
                }
                else
                {
                    hw = width / 2 + 10; hh = hw * 0.4; revealMs = 220;
                    styleTags = $"\\1a&HFF&\\3a&H00&\\3c{AssColor(FixedTemplates.DoodleLoopColor)}\\bord3\\shad0\\blur0";
                }
                int steps = FixedTemplates.DoodleRevealSteps;
                double stepDur = revealMs / steps / 1000;
                for (int step = 1; step <= steps; step++)
                {
                    double stepStart = word.Start + (step - 1) * stepDur;
                    var startTime = AssTime(stepStart);
                    var endTime = step == steps ? wordEnd : AssTime(stepStart + stepDur);
                    double fraction = (double)step / steps;
                    string drawing; double bboxMaxX, bboxMaxY;
                    if (tier == "key")
                    {
                        double rightX = -hw + 2 * hw * fraction;
                        drawing = DoodlePillPath(hw, hh, rightX);
                        bboxMaxX = rightX; bboxMaxY = hh;
                    }
                    else
                    {
                        double stepHw = hw * fraction, stepHh = hh * fraction;
                        drawing = DoodleLoopPath(stepHw, stepHh);
                        bboxMaxX = stepHw; bboxMaxY = stepHh;
                    }
                    // libass centres a \p drawing's \an5 \pos by subtracting half its OWN bounding box, wherever that box starts; sending
                    // (desired + bbox_max) cancels it for paths whose local origin is their true centre.
                    int posX = PyRound(wordCx + bboxMaxX), posY = PyRound(rowCy + bboxMaxY);
                    var tags = $"\\an5\\pos({posX},{posY}){styleTags}\\p1";
                    lines.Append($"Dialogue: -1,{startTime},{endTime},Default,,0,0,0,,{{{tags}}}{drawing}{{\\p0}}\n");
                }
            }
        }
    }
}

public static class SrtWriter
{
    /// <summary>
    /// 00:00:00,000 time. The original rounded the fractional part separately and could emit ",1000";
    /// this version rounds total milliseconds so the carry is correct.
    /// </summary>
    public static string SrtTime(double seconds)
    {
        long totalMs = (long)Math.Round(seconds * 1000, MidpointRounding.ToEven);
        long hours = totalMs / 3_600_000; totalMs %= 3_600_000;
        long minutes = totalMs / 60_000; totalMs %= 60_000;
        long secs = totalMs / 1000; long ms = totalMs % 1000;
        return $"{hours:00}:{minutes:00}:{secs:00},{ms:000}";
    }

    /// <summary>scriptMode "native" exports each word's native-script text (word.original) when it has one.</summary>
    public static string Build(IReadOnlyList<CaptionWordDto> words, string scriptMode = "roman")
    {
        var entries = words.Select((w, i) =>
        {
            var text = scriptMode == "native" && !string.IsNullOrEmpty(w.Original) ? w.Original : w.Text;
            return $"{i + 1}\n{SrtTime(w.Start)} --> {SrtTime(w.End)}\n{text}\n";
        });
        return string.Join("\n", entries);
    }
}
