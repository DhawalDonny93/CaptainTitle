using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Services.Captions;

public sealed record RepetitionMetrics(double UniqueRatio, double BigramRatio, int NgramRepeat, double Penalty);

public sealed record WhisperCandidate(List<CaptionWordDto> Words, bool Trustworthy, double AverageLogprob);

public enum AlignKind { Match, SkipWhisper, SkipSarvam }

public readonly record struct AlignOp(AlignKind Kind, int WhisperIndex, int SarvamIndex);

/// <summary>
/// Pure ports of the pure functions in the old backend/caption_ai.py (transcript scoring, chunking,
/// Whisper-timing / Sarvam-text stitching). Behaviour is locked by golden tests generated from the
/// original Python (see tests/Captions/golden).
/// </summary>
public static class TranscriptionAlgorithms
{
    private static readonly Regex TokenRegex = new(@"[\p{L}\p{N}_ऀ-ॿ]+", RegexOptions.Compiled);
    private static readonly Regex SilenceStart = new(@"silence_start:\s*([\d.]+)", RegexOptions.Compiled);
    private static readonly Regex SilenceEnd = new(@"silence_end:\s*([\d.]+)", RegexOptions.Compiled);
    private static readonly Regex WhitespaceSplit = new(@"[^\s]+", RegexOptions.Compiled);

    public static List<string> Tokens(string text) =>
        TokenRegex.Matches(text.ToLowerInvariant()).Select(m => m.Value).ToList();

    public static RepetitionMetrics RepetitionMetricsFor(IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0) return new RepetitionMetrics(0.0, 1.0, 99, 4.0);

        double uniqueRatio = (double)tokens.Distinct().Count() / tokens.Count;
        int bigramCount = Math.Max(0, tokens.Count - 1);
        int mostCommonBigram = bigramCount == 0 ? 0 :
            Enumerable.Range(0, bigramCount).GroupBy(i => (tokens[i], tokens[i + 1])).Max(g => g.Count());
        double bigramRatio = (double)mostCommonBigram / Math.Max(1, bigramCount);
        int fourCount = Math.Max(0, tokens.Count - 3);
        int ngramRepeat = fourCount == 0 ? 0 :
            Enumerable.Range(0, fourCount).GroupBy(i => (tokens[i], tokens[i + 1], tokens[i + 2], tokens[i + 3])).Max(g => g.Count());

        double penalty = 0.0;
        if (tokens.Count >= 12 && uniqueRatio < 0.35) penalty += (0.35 - uniqueRatio) * 5;
        if (mostCommonBigram >= 4) penalty += bigramRatio * 3;
        if (ngramRepeat >= 3) penalty += 2.5;
        return new RepetitionMetrics(uniqueRatio, bigramRatio, ngramRepeat, penalty);
    }

    /// <summary>The shared "is this transcript repetitive hallucination" test used per chunk and overall.</summary>
    public static bool LooksRepetitive(RepetitionMetrics m, int tokenCount) =>
        (tokenCount >= 12 && (m.UniqueRatio < .24 || m.BigramRatio > .30)) || m.NgramRepeat >= 3;

    public static List<double> SilenceMidpoints(string ffmpegStderr)
    {
        var starts = SilenceStart.Matches(ffmpegStderr).Select(m => double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        var ends = SilenceEnd.Matches(ffmpegStderr).Select(m => double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        return starts.Zip(ends, (s, e) => (s + e) / 2).ToList();
    }

    public static List<double> ChunkBoundaries(double totalDuration, IReadOnlyList<double> silencePoints, int seconds, double snapWindow)
    {
        var boundaries = new List<double> { 0.0 };
        double cursor = 0.0;
        while (cursor < totalDuration)
        {
            double target = cursor + seconds;
            var nearby = silencePoints
                .Where(p => cursor < p && p <= Math.Min(target + snapWindow, totalDuration) && p >= target - snapWindow)
                .ToList();
            double boundary = nearby.Count > 0
                ? nearby.OrderBy(p => Math.Abs(p - target)).First() // stable: first minimal, like Python min()
                : Math.Min(target, totalDuration);
            if (boundary <= cursor) break;
            boundaries.Add(boundary);
            cursor = boundary;
        }
        return boundaries;
    }

    public static void NormalizeWordTimings(List<CaptionWordDto> words)
    {
        double previousStart = 0.0;
        for (int index = 0; index < words.Count; index++)
        {
            var word = words[index];
            double start = Math.Max(word.Start, previousStart);
            double end = Math.Max(word.End, start + 0.06);
            if (index + 1 < words.Count)
            {
                double nextStart = words[index + 1].Start;
                if (nextStart >= start + 0.06) end = Math.Min(end, nextStart);
            }
            word.Start = PyRound(start, 3);
            word.End = PyRound(end, 3);
            previousStart = start;
        }
    }

    /// <summary>Python round(x, n): banker's rounding.</summary>
    public static double PyRound(double value, int digits) => Math.Round(value, digits, MidpointRounding.ToEven);

    public static WhisperCandidate BuildWhisperCandidate(JsonElement response, double skipBefore = 0.0)
    {
        var acceptedSegments = new List<JsonElement>();
        if (response.TryGetProperty("segments", out var segs) && segs.ValueKind == JsonValueKind.Array)
        {
            foreach (var segment in segs.EnumerateArray())
            {
                var text = GetString(segment, "text").Trim();
                if (text.Length == 0) continue;
                // Whisper's first stretch of ANY buffer gets an inflated no_speech_prob (no context to calibrate on). The exemption is keyed off
                // elapsed time vs skipBefore (where real content starts in this chunk: the 0.5s lead-in for chunk 0, the overlap priming audio for
                // later ones), not segment index 0 - the lead-in can push the genuine opening speech to index 1, which used to get it dropped.
                if (GetDouble(segment, "start", 0) >= skipBefore + 1.0 && GetDouble(segment, "no_speech_prob", 0) >= 0.65) continue;
                acceptedSegments.Add(segment);
            }
        }
        var acceptedRanges = acceptedSegments.Select(s => (GetDouble(s, "start", 0), GetDouble(s, "end", 0))).ToList();

        var words = new List<CaptionWordDto>();
        if (response.TryGetProperty("words", out var srcWords) && srcWords.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in srcWords.EnumerateArray())
            {
                var text = (GetStringOrNull(item, "word") ?? GetStringOrNull(item, "text") ?? "").Trim();
                double start = GetDouble(item, "start", 0);
                if (text.Length > 0 && (acceptedRanges.Count == 0 || acceptedRanges.Any(r => r.Item1 - .1 <= start && start <= r.Item2 + .1)))
                {
                    words.Add(new CaptionWordDto
                    {
                        Index = words.Count, Original = text, Text = text, Start = start,
                        End = GetDouble(item, "end", start + .35)
                    });
                }
            }
        }
        if (words.Count == 0)
        {
            foreach (var segment in acceptedSegments)
            {
                var tokens = WhitespaceSplit.Matches(GetString(segment, "text").Trim()).Select(m => m.Value).ToList();
                if (tokens.Count == 0) continue;
                double start = GetDouble(segment, "start", 0);
                double end = GetDouble(segment, "end", start + .5);
                double step = Math.Max(.08, (end - start) / tokens.Count);
                for (int offset = 0; offset < tokens.Count; offset++)
                {
                    words.Add(new CaptionWordDto
                    {
                        Index = words.Count, Original = tokens[offset], Text = tokens[offset],
                        Start = PyRound(start + offset * step, 3),
                        End = PyRound(Math.Min(end, start + (offset + 1) * step), 3)
                    });
                }
            }
        }

        var scoring = skipBefore != 0.0 ? words.Where(w => w.Start >= skipBefore).ToList() : words;
        var tokenList = Tokens(string.Join(" ", scoring.Select(w => w.Original)));
        var repetition = RepetitionMetricsFor(tokenList);
        var avgLogs = acceptedSegments.Select(s => GetDouble(s, "avg_logprob", -2)).ToList();
        double averageLogprob = avgLogs.Count > 0 ? avgLogs.Average() : (words.Count > 0 ? -0.55 : -2.0);
        bool trustworthy = words.Count > 0 && averageLogprob > -1.45;
        if (LooksRepetitive(repetition, tokenList.Count)) trustworthy = false;
        return new WhisperCandidate(words, trustworthy, PyRound(averageLogprob, 3));
    }

    /// <summary>Needleman-Wunsch style global alignment of Whisper vs Sarvam token sequences.</summary>
    public static List<AlignOp> AlignWords(IReadOnlyList<string> whisperTokens, IReadOnlyList<string> sarvamTokens)
    {
        int n = whisperTokens.Count, m = sarvamTokens.Count;
        const double gap = -0.6;
        var dp = new double[n + 1, m + 1];
        var choice = new AlignKind?[n + 1, m + 1];
        for (int i = 1; i <= n; i++) { dp[i, 0] = dp[i - 1, 0] + gap; choice[i, 0] = AlignKind.SkipWhisper; }
        for (int j = 1; j <= m; j++) { dp[0, j] = dp[0, j - 1] + gap; choice[0, j] = AlignKind.SkipSarvam; }
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                double matchScore = DifflibRatio.Ratio(whisperTokens[i - 1].ToLowerInvariant(), sarvamTokens[j - 1].ToLowerInvariant()) - 0.4;
                double best = dp[i - 1, j - 1] + matchScore;
                var bestChoice = AlignKind.Match;
                if (dp[i - 1, j] + gap > best) { best = dp[i - 1, j] + gap; bestChoice = AlignKind.SkipWhisper; }
                if (dp[i, j - 1] + gap > best) { best = dp[i, j - 1] + gap; bestChoice = AlignKind.SkipSarvam; }
                dp[i, j] = best;
                choice[i, j] = bestChoice;
            }
        }
        var ops = new List<AlignOp>();
        int a = n, b = m;
        while (a > 0 || b > 0)
        {
            var kind = choice[a, b];
            if (kind == AlignKind.Match) { ops.Add(new AlignOp(AlignKind.Match, a - 1, b - 1)); a--; b--; }
            else if (kind == AlignKind.SkipWhisper) { ops.Add(new AlignOp(AlignKind.SkipWhisper, a - 1, -1)); a--; }
            else { ops.Add(new AlignOp(AlignKind.SkipSarvam, -1, b - 1)); b--; }
        }
        ops.Reverse();
        return ops;
    }

    private static void EmitInterpolated(List<CaptionWordDto> result, List<string> texts, double start, double end)
    {
        end = Math.Max(end, start + 0.1);
        double step = Math.Max(0.08, (end - start) / texts.Count);
        for (int offset = 0; offset < texts.Count; offset++)
        {
            double wordStart = start + offset * step;
            result.Add(new CaptionWordDto
            {
                Original = texts[offset],
                Start = PyRound(wordStart, 3),
                End = PyRound(Math.Min(end, wordStart + step), 3)
            });
        }
    }

    /// <summary>Replace Whisper's words with Sarvam's recognized words while keeping Whisper's timing.</summary>
    public static List<CaptionWordDto> StitchChunk(IReadOnlyList<CaptionWordDto> whisperWords, string sarvamText)
    {
        var sarvamTokens = sarvamText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (whisperWords.Count == 0 || sarvamTokens.Count == 0) return new List<CaptionWordDto>();
        var ops = AlignWords(whisperWords.Select(w => w.Original).ToList(), sarvamTokens);
        var result = new List<CaptionWordDto>();
        var pending = new List<string>();
        double lastEnd = whisperWords[0].Start;
        foreach (var op in ops)
        {
            if (op.Kind == AlignKind.Match)
            {
                var w = whisperWords[op.WhisperIndex];
                if (pending.Count > 0)
                {
                    EmitInterpolated(result, pending, lastEnd, w.Start);
                    pending = new List<string>();
                }
                result.Add(new CaptionWordDto { Original = sarvamTokens[op.SarvamIndex], Start = w.Start, End = w.End });
                lastEnd = w.End;
            }
            else if (op.Kind == AlignKind.SkipWhisper)
            {
                // A Whisper word with no Sarvam counterpart - drop the word, keep its timing as an anchor.
                lastEnd = Math.Max(lastEnd, whisperWords[op.WhisperIndex].End);
            }
            else
            {
                pending.Add(sarvamTokens[op.SarvamIndex]);
            }
        }
        if (pending.Count > 0)
        {
            double endAnchor = Math.Max(whisperWords[^1].End, lastEnd + 0.1 * pending.Count);
            EmitInterpolated(result, pending, lastEnd, endAnchor);
        }
        return result;
    }

    private static string GetString(JsonElement e, string name) => GetStringOrNull(e, name) ?? "";

    private static string? GetStringOrNull(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double GetDouble(JsonElement e, string name, double fallback) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : fallback;
}
