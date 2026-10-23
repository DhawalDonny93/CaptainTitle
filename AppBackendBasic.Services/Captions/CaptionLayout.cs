using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Services.Captions;

/// <summary>Word grouping and line-break rules shared by the export (port of captions.py helpers).</summary>
public static class CaptionLayout
{
    public const string SentenceEndChars = ".!?।॥"; // includes Devanagari danda / double danda

    /// <summary>
    /// Percentages of the 1080x1920 canvas the whole caption block is centred on for each position preset. Mirrors CaptionStudio.tsx's
    /// POSITION_DEFAULTS exactly (the preview uses `top:%; transform:translate(-50%,-50%)`).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (double X, double Y)> PositionDefaults =
        new Dictionary<string, (double, double)> { ["top"] = (50, 14), ["middle"] = (50, 50), ["bottom"] = (50, 80) };

    public static bool EndsSentence(CaptionWordDto word)
    {
        var original = (word.Original ?? "").Trim();
        return original.Length > 0 && SentenceEndChars.Contains(original[^1]);
    }

    /// <summary>
    /// Groups consecutive words into on-screen captions. A word's own NewCaption overrides the heuristics: true always starts a new group
    /// at that word (manual "Split"), false always keeps it in the previous group (manual "Merge") even past maxWords / the gap / a sentence end.
    /// </summary>
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
            while (next < total)
            {
                var nxt = words[next];
                bool forcedSplit = nxt.NewCaption == true;
                bool forcedMerge = nxt.NewCaption == false;
                if (forcedSplit) break;
                if (!forcedMerge && !(group.Count < maxWords && !EndsSentence(group[^1])
                                      && nxt.Start - groupEnd < maxGap
                                      && (strictCount || groupEnd - words[index].Start < minDisplay))) break;
                group.Add(nxt);
                groupEnd = nxt.End;
                next++;
            }
            groups.Add(group);
            index = next;
        }
        return groups;
    }

    private static int PyRound(double v) => (int)Math.Round(v, MidpointRounding.ToEven);

    /// <summary>The point (canvas px) the whole caption block is centred on: the first word's manual pos_x/pos_y, else the preset.</summary>
    public static (int X, int Y) GroupCenter(IReadOnlyList<CaptionWordDto> group, string captionPosition)
    {
        var anchor = group[0];
        double x, y;
        if (anchor.PosX != null && anchor.PosY != null) { x = anchor.PosX.Value; y = anchor.PosY.Value; }
        else (x, y) = PositionDefaults.TryGetValue(captionPosition, out var p) ? p : PositionDefaults["bottom"];
        return (PyRound(x / 100 * 1080), PyRound(y / 100 * 1920));
    }

    /// <summary>The Transform panel's Rotation slider, stored on the group's first word. 0 = none.</summary>
    public static double GroupRotation(IReadOnlyList<CaptionWordDto> group) => group[0].Rotation ?? 0;

    public static List<List<CaptionWordDto>> SplitRows(IReadOnlyList<CaptionWordDto> group, IReadOnlyList<bool> breaks)
    {
        var rows = new List<List<CaptionWordDto>> { new() };
        for (int i = 0; i < group.Count; i++)
        {
            if (i > 0 && breaks[i]) rows.Add(new List<CaptionWordDto>());
            rows[^1].Add(group[i]);
        }
        return rows;
    }

    public static bool[] AutoLineBreaks(IReadOnlyList<CaptionWordDto> group, IReadOnlyDictionary<string, TierStyle>? tiers, int captionLines)
    {
        var breaks = new bool[group.Count];
        if (tiers != null)
        {
            string? previous = null;
            for (int i = 0; i < group.Count; i++)
            {
                var tier = group[i].Emphasis != null && tiers.ContainsKey(group[i].Emphasis) ? group[i].Emphasis : "support";
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

    public static bool[] ResolveLineBreaks(IReadOnlyList<CaptionWordDto> group, IReadOnlyDictionary<string, TierStyle>? tiers, int captionLines)
    {
        var auto = AutoLineBreaks(group, tiers, captionLines);
        return group.Select((w, i) => w.NewLine ?? auto[i]).ToArray();
    }
}
