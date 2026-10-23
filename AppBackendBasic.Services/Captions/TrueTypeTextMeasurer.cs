using System.Buffers.Binary;
using System.Collections.Concurrent;

namespace AppBackendBasic.Services.Captions;

/// <summary>Measures rendered text width for the Doodle Marker decorations (old _measure_text_width, which used PIL/FreeType).</summary>
public interface ITextMeasurer
{
    double MeasureWidth(string text, string family, int size);
}

/// <summary>
/// Minimal TrueType reader: sums per-glyph advance widths from the bundled "{family}-Bold.ttf" (cmap + hmtx), rounding each glyph's advance to
/// whole pixels like PIL's hinted basic layout. It only positions a loosely-fit decorative pill/loop behind a word, so a pixel or two of drift is
/// invisible. Falls back to a rough per-character estimate when the font file is missing (as the old code did on OSError).
/// </summary>
public sealed class TrueTypeTextMeasurer : ITextMeasurer
{
    private readonly string _fontsDir;
    private static readonly ConcurrentDictionary<string, TrueTypeAdvances?> Cache = new();

    public TrueTypeTextMeasurer(string fontsDir) => _fontsDir = fontsDir;

    public double MeasureWidth(string text, string family, int size)
    {
        var path = Path.Combine(_fontsDir, $"{family}-Bold.ttf");
        var font = Cache.GetOrAdd(path, p => TrueTypeAdvances.TryLoad(p));
        if (font == null) return text.Length * size * 0.52;
        double total = 0;
        foreach (var rune in text.EnumerateRunes())
            total += Math.Round(font.AdvanceUnits(rune.Value) * (double)size / font.UnitsPerEm, MidpointRounding.AwayFromZero);
        return total;
    }
}

internal sealed class TrueTypeAdvances
{
    public int UnitsPerEm { get; private set; } = 1000;
    private ushort[] _advances = Array.Empty<ushort>();
    private Func<int, int> _glyphFor = _ => 0;

    public int AdvanceUnits(int codepoint)
    {
        if (_advances.Length == 0) return 0;
        var gid = _glyphFor(codepoint);
        if (gid >= _advances.Length) gid = _advances.Length - 1; // glyphs past numberOfHMetrics repeat the last advance
        return _advances[gid];
    }

    public static TrueTypeAdvances? TryLoad(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var d = File.ReadAllBytes(path);
            int numTables = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(4));
            var tables = new Dictionary<string, (int Offset, int Length)>();
            for (int i = 0; i < numTables; i++)
            {
                int rec = 12 + i * 16;
                var tag = System.Text.Encoding.ASCII.GetString(d, rec, 4);
                tables[tag] = ((int)BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(rec + 8)), (int)BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(rec + 12)));
            }
            var font = new TrueTypeAdvances();
            font.UnitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(tables["head"].Offset + 18));
            int hMetrics = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(tables["hhea"].Offset + 34));
            var hmtx = tables["hmtx"].Offset;
            font._advances = new ushort[hMetrics];
            for (int i = 0; i < hMetrics; i++) font._advances[i] = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(hmtx + i * 4));
            font._glyphFor = BuildCmap(d, tables["cmap"].Offset);
            return font;
        }
        catch
        {
            return null;
        }
    }

    private static Func<int, int> BuildCmap(byte[] d, int cmap)
    {
        int count = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(cmap + 2));
        int best = -1, bestScore = -1;
        for (int i = 0; i < count; i++)
        {
            int rec = cmap + 4 + i * 8;
            int platform = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(rec));
            int encoding = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(rec + 2));
            int offset = cmap + (int)BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(rec + 4));
            int format = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(offset));
            int score = (platform, encoding, format) switch
            {
                (3, 10, 12) => 4, (0, _, 12) => 3, (3, 1, 4) => 2, (0, _, 4) => 1, _ => -1,
            };
            if (score > bestScore) { bestScore = score; best = offset; }
        }
        if (best < 0) return _ => 0;
        int fmt = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(best));
        return fmt == 12 ? Format12(d, best) : Format4(d, best);
    }

    private static Func<int, int> Format12(byte[] d, int o)
    {
        int groups = (int)BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(o + 12));
        return cp =>
        {
            for (int g = 0; g < groups; g++)
            {
                int rec = o + 16 + g * 12;
                uint start = BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(rec));
                uint end = BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(rec + 4));
                if (cp >= start && cp <= end) return (int)(BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(rec + 8)) + (uint)cp - start);
            }
            return 0;
        };
    }

    private static Func<int, int> Format4(byte[] d, int o)
    {
        int segX2 = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(o + 6));
        int seg = segX2 / 2;
        int endCodes = o + 14, startCodes = endCodes + segX2 + 2, deltas = startCodes + segX2, rangeOffsets = deltas + segX2;
        return cp =>
        {
            if (cp > 0xFFFF) return 0;
            for (int s = 0; s < seg; s++)
            {
                int end = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(endCodes + s * 2));
                if (cp > end) continue;
                int start = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(startCodes + s * 2));
                if (cp < start) return 0;
                int delta = BinaryPrimitives.ReadInt16BigEndian(d.AsSpan(deltas + s * 2));
                int ro = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(rangeOffsets + s * 2));
                if (ro == 0) return (cp + delta) & 0xFFFF;
                int glyphAddr = rangeOffsets + s * 2 + ro + (cp - start) * 2;
                int gid = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(glyphAddr));
                return gid == 0 ? 0 : (gid + delta) & 0xFFFF;
            }
            return 0;
        };
    }
}
