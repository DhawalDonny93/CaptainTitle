using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace AppBackendBasic.Services.Captions;

public interface IWhisperClient
{
    /// <summary>Whisper verbose_json with word timestamps. Used for TIMING only (text is replaced by Sarvam's).</summary>
    Task<JsonElement> TranscribeAsync(string audioPath, string? language, string prompt, CancellationToken ct = default);
}

public interface ISarvamClient
{
    /// <summary>Returns the recognised transcript for one chunk (Sarvam's REST API has no word timestamps).</summary>
    Task<string> TranscribeAsync(string audioPath, string languageCode, IReadOnlyList<string>? keyterms, CancellationToken ct = default);
}

public class WhisperClient : IWhisperClient
{
    private readonly HttpClient _http;
    private readonly CaptionOptions _options;
    private readonly ILlmProviderConfigService _providerConfig;

    public WhisperClient(HttpClient http, IOptions<CaptionOptions> options, ILlmProviderConfigService providerConfig)
    {
        _http = http;
        _options = options.Value;
        _providerConfig = providerConfig;
        _http.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<JsonElement> TranscribeAsync(string audioPath, string? language, string prompt, CancellationToken ct = default)
    {
        var apiKey = !string.IsNullOrWhiteSpace(_options.OpenAiApiKey)
            ? _options.OpenAiApiKey
            : await _providerConfig.GetConfigValueAsync("OpenAI", "ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("OpenAI API key is not configured (Admin > LLM Providers or Captions:OpenAiApiKey)");

        using var form = new MultipartFormDataContent();
        await using var fs = File.OpenRead(audioPath);
        var file = new StreamContent(fs);
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(file, "file", Path.GetFileName(audioPath));
        form.Add(new StringContent(_options.WhisperModel), "model");
        form.Add(new StringContent("verbose_json"), "response_format");
        form.Add(new StringContent("0"), "temperature");
        form.Add(new StringContent("word"), "timestamp_granularities[]");
        if (!string.IsNullOrEmpty(language)) form.Add(new StringContent(language), "language");
        if (!string.IsNullOrEmpty(prompt)) form.Add(new StringContent(prompt), "prompt");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.OpenAiBaseUrl.TrimEnd('/')}/audio/transcriptions") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Whisper request failed ({(int)response.StatusCode}): {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }
}

public class SarvamClient : ISarvamClient
{
    private readonly HttpClient _http;
    private readonly CaptionOptions _options;

    public SarvamClient(HttpClient http, IOptions<CaptionOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.Timeout = TimeSpan.FromMinutes(3);
    }

    public async Task<string> TranscribeAsync(string audioPath, string languageCode, IReadOnlyList<string>? keyterms, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SarvamApiKey)) throw new InvalidOperationException("Sarvam API key is not configured (Captions:SarvamApiKey)");

        using var form = new MultipartFormDataContent();
        await using var fs = File.OpenRead(audioPath);
        var file = new StreamContent(fs);
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(file, "file", Path.GetFileName(audioPath));
        form.Add(new StringContent(_options.SarvamModel), "model");
        form.Add(new StringContent("codemix"), "mode");
        form.Add(new StringContent(languageCode), "language_code");
        if (keyterms is { Count: > 0 })
            form.Add(new StringContent(JsonSerializer.Serialize(keyterms.Take(50).Select(k => k.Length > 64 ? k[..64] : k))), "keyterms");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.SarvamBaseUrl.TrimEnd('/')}/speech-to-text") { Content = form };
        request.Headers.Add("api-subscription-key", _options.SarvamApiKey);
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Sarvam request failed ({(int)response.StatusCode}): {body}");
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("transcript", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() ?? "" : "";
    }
}
