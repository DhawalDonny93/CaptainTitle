using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Services.LlmProviders;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Captions;

public interface IHinglishService
{
    Task<List<CaptionWordDto>> ConvertToHinglishAsync(List<CaptionWordDto> words, string context, CancellationToken ct = default);
    Task<List<CaptionWordDto>> ReassignEmphasisAsync(List<CaptionWordDto> words, string context, CancellationToken ct = default);
}

/// <summary>
/// Hindi -> Roman Hinglish conversion + 3-tier emphasis tagging (port of caption_ai.convert_to_hinglish /
/// reassign_emphasis). Provider/model/max-tokens/temperature come from the admin-managed LlmJobConfigs
/// (job keys CaptionHinglishConversion / CaptionEmphasisAssignment) instead of the old CAPTION_LLM_MODEL env var.
/// </summary>
public class HinglishService : IHinglishService
{
    private static readonly Regex JsonArray = new(@"\[[\s\S]*\]", RegexOptions.Compiled);
    private static readonly HashSet<string> Tiers = new() { "support", "feature", "key" };

    private const string EmphasisRules =
        "\"key\" is the single punchiest word in a short phrase - a number, a strong verb/adjective, a brand or product name, " +
        "the emotional peak of what's being said. Use it sparingly, roughly 1 word in every 10-15. " +
        "\"feature\" is a secondary notable word worth a visual lift but not the main punch - another name, quantity, or " +
        "descriptive word. Use it for roughly 15-20 percent of words. Every other word (articles, connectors, filler) is \"support\".";

    private const string ConvertSystem =
        "You convert Hindi or mixed Hindi-English speech to exact Roman Hinglish captions. " +
        "Correct only obvious phonetic ASR spelling errors when the intended Hindi or English word is unambiguous from context. " +
        "Never paraphrase, omit, merge, reorder, or add words, and keep one output token per input index. Return only a JSON array. Each item must be " +
        "{\"index\": integer, \"text\": romanized_word, \"emphasis\": \"support\"|\"feature\"|\"key\", \"uncertain\": boolean}. Keep existing English words unchanged. " +
        "Set uncertain=true when the ASR source word or surrounding phrase is nonsensical and cannot be corrected confidently. " +
        "Emphasis is a 3-tier read of how a professional caption editor would highlight each word: " + EmphasisRules;

    private const string EmphasisSystem =
        "You are a professional caption editor deciding which words in a short-form video caption " +
        "deserve visual emphasis. Return only a JSON array, one item per input index: " +
        "{\"index\": integer, \"emphasis\": \"support\"|\"feature\"|\"key\"}. " + EmphasisRules;

    private readonly LlmProviderFactory _providers;
    private readonly ILlmJobConfigService _jobConfig;
    private readonly ILogger<HinglishService> _logger;

    public HinglishService(LlmProviderFactory providers, ILlmJobConfigService jobConfig, ILogger<HinglishService> logger)
    {
        _providers = providers;
        _jobConfig = jobConfig;
        _logger = logger;
    }

    private async Task<Dictionary<int, JsonElement>> CallAsync(string jobKey, string system, string userPrompt)
    {
        var cfg = await _jobConfig.GetJobConfigAsync(jobKey, "OpenAI", "gpt-4o", 16000, 0m);
        var provider = _providers.GetProvider(cfg.Provider);
        var response = await provider.SendRequestAsync(new LlmRequestDto
        {
            Provider = cfg.Provider, Model = cfg.Model, RequestType = jobKey, SystemPrompt = system,
            PromptText = userPrompt, Temperature = cfg.Temperature, MaxTokens = cfg.MaxTokens,
        });
        if (!response.Success) throw new InvalidOperationException($"LLM request failed: {response.ErrorMessage}");

        var raw = (response.ResponseText ?? "").Trim();
        var match = JsonArray.Match(raw);
        if (!match.Success) throw new InvalidDataException("LLM did not return valid JSON");
        using var doc = JsonDocument.Parse(match.Value);
        var map = new Dictionary<int, JsonElement>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("index", out var idx)) continue;
            int key = idx.ValueKind == JsonValueKind.Number ? idx.GetInt32() : int.Parse(idx.GetString()!);
            map[key] = item.Clone();
        }
        return map;
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    public async Task<List<CaptionWordDto>> ConvertToHinglishAsync(List<CaptionWordDto> words, string context, CancellationToken ct = default)
    {
        var numbered = string.Join("\n", words.Select(w => $"{w.Index}: {w.Original}"));
        var contextLine = string.IsNullOrEmpty(context) ? "" : $"Video context only for resolving product terms: {context}\n";
        var converted = await CallAsync(CaptionSettingKeys.HinglishModelJobKey, ConvertSystem, $"{contextLine}Convert these indexed words:\n{numbered}");

        var missing = words.Count(w => !converted.ContainsKey(w.Index));
        // The model occasionally drops indices from a long response; falling back to the source text for just those beats failing.
        if (missing > 0) _logger.LogWarning("Hinglish conversion dropped {Missing}/{Total} word indices", missing, words.Count);

        foreach (var word in words)
        {
            converted.TryGetValue(word.Index, out var item);
            var text = item.ValueKind == JsonValueKind.Object ? Str(item, "text") : null;
            word.Text = (string.IsNullOrEmpty(text) ? word.Original : text).Trim();
            var emphasis = item.ValueKind == JsonValueKind.Object ? Str(item, "emphasis") : null;
            if (emphasis == null || !Tiers.Contains(emphasis)) emphasis = "support";
            word.Emphasis = emphasis;
            word.Important = emphasis != "support"; // kept for templates built around a simple on/off flag
            word.Uncertain = item.ValueKind == JsonValueKind.Object && item.TryGetProperty("uncertain", out var u) && u.ValueKind == JsonValueKind.True;
        }
        return words;
    }

    public async Task<List<CaptionWordDto>> ReassignEmphasisAsync(List<CaptionWordDto> words, string context, CancellationToken ct = default)
    {
        var numbered = string.Join("\n", words.Select(w => $"{w.Index}: {w.Text}"));
        var contextLine = string.IsNullOrEmpty(context) ? "" : $"Video context only for resolving product terms: {context}\n";
        var converted = await CallAsync(CaptionSettingKeys.EmphasisJobKey, EmphasisSystem, $"{contextLine}Assign emphasis to these indexed words:\n{numbered}");
        foreach (var word in words)
        {
            converted.TryGetValue(word.Index, out var item);
            var emphasis = item.ValueKind == JsonValueKind.Object ? Str(item, "emphasis") : null;
            if (emphasis == null || !Tiers.Contains(emphasis)) emphasis = string.IsNullOrEmpty(word.Emphasis) ? "support" : word.Emphasis;
            word.Emphasis = emphasis;
            word.Important = emphasis != "support";
        }
        return words;
    }
}
