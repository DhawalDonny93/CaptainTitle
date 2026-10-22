using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Services.Captions;

/// <summary>Word grouping and line-break rules shared by the export (port of captions.py helpers).</summary>
public static class CaptionLayout
{
    public const string SentenceEndChars = ".!?।॥"; // includes Devanagari danda / double danda

    public static bool EndsSentence(CaptionWordDto word)
    {
        var original = (word.Original ?? "").Trim();
        return original.Length > 0 && SentenceEndChars.Contains(original[^1]);
    }

    public static List<List<CaptionWordDto>> GroupWordsForDisplay(IReadOnlyList<CaptionWordDto> words,
        double minDisplay = 0.35, int maxWords = 3, double maxGap = 0.4, bool strictCount = false)
    {
        var groups = new List<List<CaptionWordDto>>();
        int index = 0, total = words.Count;
        while (index < total)
        {
            var group = new List<CaptionWordDto> { words[index] };
            double groupEnd = words[index].End;
            int next = index + 1;
            while (next < total && group.Count < maxWords && !EndsSentence(group[^1])
                   && words[next].Start - groupEnd < maxGap
                   && (strictCount || groupEnd - words[index].Start < minDisplay))
            {
                group.Add(words[next]);
                groupEnd = words[next].End;
                next++;
            }
            groups.Add(group);
            index = next;
        }
        return groups;
    }

    /// <summary>ASS \an alignment + vertical margin per position preset.</summary>
    public static (int Align, int MarginV) PositionPreset(string position) => position switch
    {
        "top" => (8, 150),
        "middle" => (5, 0),
        _ => (2, 210),
    };

    public static bool[] AutoLineBreaks(IReadOnlyList<CaptionWordDto> group, ThemeStyle theme, int captionLines)
    {
        var breaks = new bool[group.Count];
        if (theme.Tiers != null)
        {
            string? previous = null;
            for (int i = 0; i < group.Count; i++)
            {
                var tier = group[i].Emphasis != null && theme.Tiers.ContainsKey(group[i].Emphasis) ? group[i].Emphasis : "support";
                breaks[i] = i > 0 && tier != previous;
                previous = tier;
            }
            return breaks;
        }
        if (captionLines == 2 && group.Count > 1)
        {
            int midpoint = (group.Count + 1) / 2; // ceil(len/2)
            for (int i = 0; i < group.Count; i++) breaks[i] = i == midpoint;
        }
        return breaks;
    }

    public static bool[] ResolveLineBreaks(IReadOnlyList<CaptionWordDto> group, ThemeStyle theme, int captionLines)
    {
        var auto = AutoLineBreaks(group, theme, captionLines);
        return group.Select((w, i) => w.NewLine ?? auto[i]).ToArray();
    }
}
