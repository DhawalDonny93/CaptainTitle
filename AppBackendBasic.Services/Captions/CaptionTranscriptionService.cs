using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Captions;

public sealed class NoTrustworthyTranscriptException : Exception
{
    public NoTrustworthyTranscriptException()
        : base("No trustworthy transcript was found. The audio may be unclear, music-heavy, or repetitive.") { }
}

public sealed record TranscriptionDiagnostics(string Mode, int ChunksUsed, double UniqueRatio, double BigramRatio, int NgramRepeat, int SarvamFailedChunks = 0);

public interface ICaptionTranscriptionService
{
    Task<(List<CaptionWordDto> Words, TranscriptionDiagnostics Diagnostics)> TranscribeWordsAsync(
        string audioPath, string speechLanguage, CancellationToken ct = default);
}

/// <summary>
/// Hybrid transcription (port of caption_ai.transcribe_words): Whisper supplies per-word TIMING, Sarvam supplies the
/// recognised TEXT; the two are stitched per ~20s chunk, with repetition/hallucination guards.
/// </summary>
public class CaptionTranscriptionService : ICaptionTranscriptionService
{
    private static readonly Regex Delimiters = new(@"[,;]", RegexOptions.Compiled);

    private readonly IWhisperClient _whisper;
    private readonly ISarvamClient _sarvam;
    private readonly IFfmpegService _ffmpeg;
    private readonly ILogger<CaptionTranscriptionService> _logger;

    public CaptionTranscriptionService(IWhisperClient whisper, ISarvamClient sarvam, IFfmpegService ffmpeg,
        ILogger<CaptionTranscriptionService> logger)
    {
        _whisper = whisper;
        _sarvam = sarvam;
        _ffmpeg = ffmpeg;
        _logger = logger;
    }

    private sealed record Chunk(string Path, double Offset, double ContentStart);

    /// <summary>
    /// Sarvam hard-caps each request at 30s; worst case chunk = seconds + snapWindow + overlap = 23.5s.
    /// First chunk gets a silence lead-in; later chunks are primed with real audio from the previous chunk's tail.
    /// </summary>
    private async Task<List<Chunk>> SplitWavAsync(string audioPath, string outputDir, CancellationToken ct,
        int seconds = 20, double snapWindow = 2.0, double overlapSeconds = 1.5)
    {
        const double leadInSeconds = 0.5;
        var silencePoints = await _ffmpeg.SilenceMidpointsAsync(audioPath, ct);
        var wav = WavAudio.Read(audioPath);
        var boundaries = TranscriptionAlgorithms.ChunkBoundaries(wav.DurationSeconds, silencePoints, seconds, snapWindow);
        var silencePrefix = new byte[(int)(wav.SampleRate * leadInSeconds) * wav.BytesPerFrame];
        var chunks = new List<Chunk>();
        for (int index = 0; index < boundaries.Count - 1; index++)
        {
            double start = boundaries[index], end = boundaries[index + 1];
            byte[] prefix; double prefixDuration;
            if (index == 0) { prefix = silencePrefix; prefixDuration = leadInSeconds; }
            else
            {
                double prefixStart = Math.Max(0.0, start - overlapSeconds);
                prefix = wav.FrameSlice(prefixStart, start);
                prefixDuration = start - prefixStart;
            }
            var path = Path.Combine(outputDir, $"speech-{index}.wav");
            wav.WriteChunk(path, prefix, wav.FrameSlice(start, end));
            chunks.Add(new Chunk(path, start - prefixDuration, start));
        }
        return chunks;
    }

    public async Task<(List<CaptionWordDto> Words, TranscriptionDiagnostics Diagnostics)> TranscribeWordsAsync(
        string audioPath, string speechLanguage, CancellationToken ct = default)
    {
        string? whisperLanguage = speechLanguage == "hi" ? "hi" : null;
        string sarvamLanguageCode = speechLanguage == "hi" ? "hi-IN" : "unknown";
        var selectedChunks = new List<List<CaptionWordDto>>();
        // Carrying recent words forward (Whisper prompt + Sarvam keyterms) gives continuity across chunk boundaries.
        string runningPrompt = "";
        var runningKeyterms = new List<string>();
        int sarvamPasses = 0, sarvamFailures = 0;

        var tempDir = Path.Combine(Path.GetTempPath(), "captain-chunks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            foreach (var chunk in await SplitWavAsync(audioPath, tempDir, ct))
            {
                double skipBefore = chunk.ContentStart - chunk.Offset;
                // A failed Whisper call (bad key, quota, outage, timeout) is deliberately NOT caught here: it says nothing about the audio, so it must not
                // end up as "no trustworthy transcript" or as a silent hole in the captions. The job fails, uncharged, as a server issue.
                var response = await _whisper.TranscribeAsync(chunk.Path, whisperLanguage, runningPrompt, ct);
                var candidate = TranscriptionAlgorithms.BuildWhisperCandidate(response, skipBefore);

                if (!candidate.Trustworthy || candidate.Words.Count == 0)
                {
                    _logger.LogWarning("Chunk {Chunk} has no usable timing skeleton: avg_logprob={Avg} words={Count}", chunk.Path, candidate.AverageLogprob, candidate.Words.Count);
                    continue;
                }

                string sarvamText = "";
                sarvamPasses++;
                try { sarvamText = (await _sarvam.TranscribeAsync(chunk.Path, sarvamLanguageCode, runningKeyterms, ct)).Trim(); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex) { sarvamFailures++; _logger.LogError(ex, "Sarvam text pass failed for chunk {Chunk}", chunk.Path); }

                // If Sarvam fails for this chunk, fall back to Whisper's own words rather than losing the chunk.
                var stitched = sarvamText.Length > 0 ? TranscriptionAlgorithms.StitchChunk(candidate.Words, sarvamText) : new List<CaptionWordDto>();
                if (stitched.Count == 0) stitched = candidate.Words;

                var scoring = stitched.Where(w => w.Start >= skipBefore).ToList();
                var tokenList = TranscriptionAlgorithms.Tokens(string.Join(" ", scoring.Select(w => w.Original)));
                var repetition = TranscriptionAlgorithms.RepetitionMetricsFor(tokenList);
                bool trustworthy = stitched.Count > 0 && !TranscriptionAlgorithms.LooksRepetitive(repetition, tokenList.Count);
                if (!trustworthy)
                {
                    _logger.LogWarning("Chunk {Chunk} rejected after stitching: unique_ratio={U} bigram_ratio={B} ngram_repeat={N} words={W}",
                        chunk.Path, repetition.UniqueRatio, repetition.BigramRatio, repetition.NgramRepeat, stitched.Count);
                    continue;
                }

                foreach (var word in stitched)
                {
                    word.Start = TranscriptionAlgorithms.PyRound(word.Start + chunk.Offset, 3);
                    word.End = TranscriptionAlgorithms.PyRound(word.End + chunk.Offset, 3);
                }
                // Drop words re-recognised from the overlap priming audio (the previous chunk already produced them).
                stitched = stitched.Where(w => w.Start >= chunk.ContentStart - 0.05).ToList();
                selectedChunks.Add(stitched);
                if (stitched.Count > 0)
                {
                    var tail = stitched.Skip(Math.Max(0, stitched.Count - 40)).Select(w => w.Original).ToList();
                    var joined = string.Join(" ", tail);
                    runningPrompt = joined.Length > 800 ? joined[^800..] : joined;
                    // Sarvam rejects keyterms containing , or ; (its own delimiters).
                    var seen = new HashSet<string>();
                    runningKeyterms = tail.Select(w => Delimiters.Replace(w, "").Trim()).Where(w => w.Length > 0 && seen.Add(w)).ToList();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
        }

        var words = new List<CaptionWordDto>();
        foreach (var selected in selectedChunks)
            foreach (var word in selected) { word.Index = words.Count; words.Add(word); }
        TranscriptionAlgorithms.NormalizeWordTimings(words);

        var overallTokens = TranscriptionAlgorithms.Tokens(string.Join(" ", words.Select(w => w.Original)));
        var overall = TranscriptionAlgorithms.RepetitionMetricsFor(overallTokens);
        if (words.Count == 0 || (words.Count >= 12 && (overall.UniqueRatio < .24 || overall.BigramRatio > .30 || overall.NgramRepeat >= 3)))
            throw new NoTrustworthyTranscriptException();

        // The stored mode has to say what really produced the text, or a dead Sarvam key looks like a normal run.
        if (sarvamFailures > 0) _logger.LogError("Sarvam failed on {Failed}/{Passes} chunks; those captions use Whisper's text", sarvamFailures, sarvamPasses);
        var mode = sarvamFailures == 0 ? "whisper-timing+sarvam-text" : sarvamFailures < sarvamPasses ? "whisper-timing+partial-sarvam-text" : "whisper-only";
        return (words, new TranscriptionDiagnostics(mode, selectedChunks.Count,
            Math.Round(overall.UniqueRatio, 3), Math.Round(overall.BigramRatio, 3), overall.NgramRepeat, sarvamFailures));
    }
}
