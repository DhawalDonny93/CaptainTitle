using System.Text;
using System.Text.Json;
using AppBackendBasic.Services.Captions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AppBackendBasic.Captions.Tests;

/// <summary>A server/provider failure must never be reported to the user as a problem with their video, and vice versa.</summary>
public class ErrorReportingTests : IDisposable
{
    private const string GoodWhisperResponse = """
        {"segments":[{"text":"aaj hum naya video dekhenge","start":0.5,"end":2.5,"avg_logprob":-0.2,"no_speech_prob":0.01}],
         "words":[{"word":"aaj","start":0.6,"end":0.9},{"word":"hum","start":1.0,"end":1.2},{"word":"naya","start":1.3,"end":1.6},{"word":"video","start":1.7,"end":2.0},{"word":"dekhenge","start":2.1,"end":2.4}]}
        """;

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "captain-errors-test-" + Guid.NewGuid().ToString("N"));
    private readonly string _wav;

    public ErrorReportingTests()
    {
        Directory.CreateDirectory(_dir);
        _wav = Path.Combine(_dir, "speech.wav");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    /// <summary>16 kHz mono silence; with no silence points the chunker cuts at 20s.</summary>
    private void WriteWav(int seconds)
    {
        var data = new byte[16000 * 2 * seconds];
        using var w = new BinaryWriter(File.Create(_wav));
        w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + data.Length); w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(16000); w.Write(32000); w.Write((short)2); w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data")); w.Write(data.Length); w.Write(data);
    }

    private static CaptionTranscriptionService Service(IWhisperClient whisper, ISarvamClient sarvam) =>
        new(whisper, sarvam, new SilenceOnlyFfmpeg(), NullLogger<CaptionTranscriptionService>.Instance);

    [Fact]
    public async Task A_whisper_failure_is_not_reported_as_an_untrustworthy_transcript()
    {
        WriteWav(45);
        var whisper = new FakeWhisper(_ => throw new HttpRequestException("Whisper request failed (401): invalid_api_key"));
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Service(whisper, new FakeSarvam(_ => "")).TranscribeWordsAsync(_wav, "hi"));
        Assert.Equal(CaptionErrors.ServerIssue, CaptionErrors.JobFailureMessage(ex));
        Assert.Equal(1, whisper.Calls); // no point paying for the remaining chunks
    }

    [Fact]
    public async Task A_whisper_failure_on_a_later_chunk_fails_the_run_instead_of_leaving_a_hole()
    {
        WriteWav(45);
        var whisper = new FakeWhisper(call => call == 2 ? throw new HttpRequestException("Whisper request failed (500)") : GoodWhisperResponse);
        await Assert.ThrowsAsync<HttpRequestException>(() => Service(whisper, new FakeSarvam(_ => "")).TranscribeWordsAsync(_wav, "hi"));
    }

    [Fact]
    public async Task A_whisper_timeout_is_a_server_failure_not_a_shutdown()
    {
        WriteWav(5);
        // HttpClient reports its own timeout as TaskCanceledException with the caller's token still live.
        var whisper = new FakeWhisper(_ => throw new TaskCanceledException("HttpClient.Timeout elapsed"));
        var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => Service(whisper, new FakeSarvam(_ => "")).TranscribeWordsAsync(_wav, "hi"));
        Assert.Equal(CaptionErrors.ServerIssue, CaptionErrors.JobFailureMessage(ex));
    }

    [Fact]
    public async Task Audio_with_no_speech_is_reported_as_an_untrustworthy_transcript()
    {
        WriteWav(5);
        var whisper = new FakeWhisper(_ => """{"segments":[],"words":[]}""");
        var ex = await Assert.ThrowsAsync<NoTrustworthyTranscriptException>(() => Service(whisper, new FakeSarvam(_ => "")).TranscribeWordsAsync(_wav, "hi"));
        Assert.Contains("Try clearer speech", CaptionErrors.JobFailureMessage(ex));
    }

    [Fact]
    public async Task Sarvam_failing_everywhere_falls_back_to_whisper_and_records_it()
    {
        WriteWav(5);
        var (words, diagnostics) = await Service(new FakeWhisper(_ => GoodWhisperResponse), new FakeSarvam(_ => throw new HttpRequestException("Sarvam request failed (403)")))
            .TranscribeWordsAsync(_wav, "hi");
        Assert.NotEmpty(words);
        Assert.Equal("whisper-only", diagnostics.Mode);
        Assert.Equal(1, diagnostics.SarvamFailedChunks);
    }

    [Fact]
    public async Task A_sarvam_timeout_also_falls_back_to_whisper()
    {
        WriteWav(5);
        var (words, diagnostics) = await Service(new FakeWhisper(_ => GoodWhisperResponse), new FakeSarvam(_ => throw new TaskCanceledException("HttpClient.Timeout elapsed")))
            .TranscribeWordsAsync(_wav, "hi");
        Assert.NotEmpty(words);
        Assert.Equal("whisper-only", diagnostics.Mode);
    }

    [Fact]
    public async Task Sarvam_failing_on_some_chunks_is_recorded_as_partial()
    {
        WriteWav(45);
        var (_, diagnostics) = await Service(new FakeWhisper(_ => GoodWhisperResponse),
                new FakeSarvam(call => call == 1 ? throw new HttpRequestException("Sarvam request failed (500)") : "aaj hum naya video dekhenge"))
            .TranscribeWordsAsync(_wav, "hi");
        Assert.Equal("whisper-timing+partial-sarvam-text", diagnostics.Mode);
        Assert.Equal(1, diagnostics.SarvamFailedChunks);
    }

    [Fact]
    public async Task A_healthy_run_keeps_the_normal_mode()
    {
        WriteWav(5);
        var (_, diagnostics) = await Service(new FakeWhisper(_ => GoodWhisperResponse), new FakeSarvam(_ => "aaj hum naya video dekhenge")).TranscribeWordsAsync(_wav, "hi");
        Assert.Equal("whisper-timing+sarvam-text", diagnostics.Mode);
        Assert.Equal(0, diagnostics.SarvamFailedChunks);
    }

    [Fact]
    public async Task Cancellation_during_sarvam_still_stops_the_run()
    {
        WriteWav(5);
        using var cts = new CancellationTokenSource();
        var sarvam = new FakeSarvam(_ => { cts.Cancel(); throw new OperationCanceledException(cts.Token); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(new FakeWhisper(_ => GoodWhisperResponse), sarvam).TranscribeWordsAsync(_wav, "hi", cts.Token));
    }

    [Fact]
    public void Only_failures_known_to_come_from_the_video_blame_the_video()
    {
        Assert.Contains("Try clearer speech", CaptionErrors.JobFailureMessage(new NoTrustworthyTranscriptException()));
        Assert.Equal("This video has no audio track", CaptionErrors.JobFailureMessage(new CaptionVideoUnusableException("This video has no audio track")));
        foreach (var serverFailure in new Exception[] { new HttpRequestException("x"), new InvalidOperationException("LLM request failed"), new InvalidDataException("LLM did not return valid JSON"), new JsonException("x"), new IOException("disk full") })
            Assert.Equal(CaptionErrors.ServerIssue, CaptionErrors.JobFailureMessage(serverFailure));
    }

    private sealed class FakeWhisper : IWhisperClient
    {
        private readonly Func<int, string> _respond;
        public int Calls { get; private set; }
        public FakeWhisper(Func<int, string> respond) { _respond = respond; }
        public Task<JsonElement> TranscribeAsync(string audioPath, string? language, string prompt, CancellationToken ct = default) =>
            Task.FromResult(JsonDocument.Parse(_respond(++Calls)).RootElement.Clone());
    }

    private sealed class FakeSarvam : ISarvamClient
    {
        private readonly Func<int, string> _respond;
        private int _calls;
        public FakeSarvam(Func<int, string> respond) { _respond = respond; }
        public Task<string> TranscribeAsync(string audioPath, string languageCode, IReadOnlyList<string>? keyterms, CancellationToken ct = default) =>
            Task.FromResult(_respond(++_calls));
    }

    private sealed class SilenceOnlyFfmpeg : IFfmpegService
    {
        public Task<List<double>> SilenceMidpointsAsync(string audioPath, CancellationToken ct = default) => Task.FromResult(new List<double>());
        public Task<FfmpegResult> RunAsync(IEnumerable<string> args, string? workingDirectory = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<VideoProbe> ProbeAsync(string path, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ExtractAudioAsync(string source, string wavOut, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> CreateBrowserPreviewAsync(string source, string output, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> CreateThumbnailAsync(string source, string output, double atSeconds, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FfmpegCapability> CheckAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
}
