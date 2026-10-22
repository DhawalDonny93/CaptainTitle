using System.Globalization;
using System.Text;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Services.Captions;

/// <summary>Builds the ASS subtitle script burned into the export (port of captions.py _write_ass).</summary>
public static class AssSubtitleWriter
{
    public const string DefaultEmphasizeAccent = "#EAB308";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

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

    private static int PyRound(double v) => (int)Math.Round(v, MidpointRounding.ToEven);

    public static string Build(IReadOnlyList<CaptionWordDto> words, string themeKey, string fontId, int? wordsPerCaption,
        int captionLines, string captionPosition, IReadOnlyList<CaptionElementDto>? elements, string? variantKey,
        CaptionStyleSet styles)
    {
        var theme = styles.Theme(themeKey);
        var primaryFamily = styles.Font(fontId).Family;
        var accentFamily = theme.AccentFont ?? primaryFamily;
        var variant = styles.Variant(variantKey);

        var sb = new StringBuilder();
        sb.Append("[Script Info]\nScriptType: v4.00+\nPlayResX: 1080\nPlayResY: 1920\n[V4+ Styles]\n");
        sb.Append("Format: Name,Fontname,Fontsize,PrimaryColour,SecondaryColour,OutlineColour,BackColour,Bold,Italic,Underline,StrikeOut,ScaleX,ScaleY,Spacing,Angle,BorderStyle,Outline,Shadow,Alignment,MarginL,MarginR,MarginV,Encoding\n");
        sb.Append($"Style: Default,{primaryFamily},52,&H00FFFFFF,&H00007AFF,&H00000000,&H80000000,-1,0,0,0,100,100,0,0,1,4,1,2,70,70,210,1\n");
        sb.Append("[Events]\nFormat: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text\n");

        var groups = CaptionLayout.GroupWordsForDisplay(words, maxWords: wordsPerCaption ?? 3, strictCount: wordsPerCaption != null);
        foreach (var group in groups)
        {
            var anchor = group[0];
            string positionTag; int marginV;
            if (anchor.PosX != null && anchor.PosY != null)
            {
                positionTag = $"\\an5\\pos({PyRound(anchor.PosX.Value / 100 * 1080)},{PyRound(anchor.PosY.Value / 100 * 1920)})";
                marginV = 0;
            }
            else
            {
                var (align, margin) = CaptionLayout.PositionPreset(captionPosition);
                positionTag = $"\\an{align}";
                marginV = margin;
            }

            var breaks = CaptionLayout.ResolveLineBreaks(group, theme, captionLines);
            var styled = new StringBuilder();
            for (int index = 0; index < group.Count; index++)
            {
                var word = group[index];
                var safe = word.Text.Replace("{", "").Replace("}", "");
                string family;
                if (variant != null) family = variant.Font;
                else if (theme.Tiers != null)
                {
                    var tier = theme.Tiers.ContainsKey(word.Emphasis) ? word.Emphasis : "support";
                    family = theme.Tiers[tier].Font;
                }
                else family = word.Important ? accentFamily : primaryFamily;

                string color = word.Color ?? "#FFFFFF";
                int fontSize = word.FontSize;
                if (word.Emphasize)
                {
                    color = variant?.Accent ?? DefaultEmphasizeAccent;
                    fontSize = PyRound(fontSize * 1.18);
                }
                var tags = $"\\fn{family}\\c{AssColor(color)}\\fs{fontSize}";
                if (variant != null)
                {
                    var outlineHex = variant.Outline ?? color;
                    tags += $"\\3c{AssColor(outlineHex)}\\bord{variant.Border}\\shad{variant.Shadow}\\blur{variant.Blur}";
                }
                if (index == 0) tags = positionTag + tags;
                var separator = index > 0 && breaks[index] ? "\\N" : (index > 0 ? " " : "");
                styled.Append($"{separator}{{{tags}}}{safe}");
            }
            sb.Append($"Dialogue: 0,{AssTime(group[0].Start)},{AssTime(group[^1].End)},Default,,0,0,{marginV},,{styled}\n");
        }

        foreach (var element in elements ?? Array.Empty<CaptionElementDto>())
        {
            if (element.End > element.Start) sb.Append(ElementDialogue(element, styles));
        }
        return sb.ToString();
    }

    private static string ElementDialogue(CaptionElementDto element, CaptionStyleSet styles)
    {
        var safe = (element.Char ?? "").Replace("{", "").Replace("}", "");
        int x = PyRound(element.PosX / 100 * 1080);
        int y = PyRound(element.PosY / 100 * 1920);
        var tags = $"\\an5\\pos({x},{y})\\fs{element.FontSize}\\c{AssColor(element.Color ?? "#FFFFFF")}";
        if (element.Kind == "text") tags = $"\\fn{styles.Font(element.FontId).Family}" + tags;
        return $"Dialogue: 1,{AssTime(element.Start)},{AssTime(element.End)},Default,,0,0,0,,{{{tags}}}{safe}\n";
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

    public static string Build(IReadOnlyList<CaptionWordDto> words)
    {
        var entries = words.Select((w, i) => $"{i + 1}\n{SrtTime(w.Start)} --> {SrtTime(w.End)}\n{w.Text}\n");
        return string.Join("\n", entries);
    }
}
