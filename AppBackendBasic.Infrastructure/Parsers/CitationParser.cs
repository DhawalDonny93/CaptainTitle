using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Parsers
{
    public class CitationParser
    {
        private readonly ILogger<CitationParser> _logger;

        public CitationParser(ILogger<CitationParser> logger)
        {
            _logger = logger;
        }

        public List<DetectedCitation> ParseCitations(string responseText)
        {
            var citations = new List<DetectedCitation>();

            if (string.IsNullOrWhiteSpace(responseText))
                return citations;

            // Extract URLs using regex
            var urls = ExtractUrls(responseText);
            var sentences = SplitIntoSentences(responseText);

            foreach (var url in urls)
            {
                var citation = new DetectedCitation
                {
                    Url = url,
                    Domain = ExtractDomain(url),
                    PagePath = ExtractPath(url),
                    CitationType = DetermineCitationType(responseText, url),
                    Position = FindUrlPosition(sentences, url),
                    Context = ExtractContext(sentences, url)
                };

                citations.Add(citation);
            }

            return citations;
        }

        public List<DetectedCitation> ParseLlmCitationResponse(string llmJsonResponse)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var parsed = JsonSerializer.Deserialize<LlmCitationResponse>(llmJsonResponse, options);

                if (parsed?.Citations == null)
                {
                    _logger.LogWarning("Failed to parse LLM citation response");
                    return new List<DetectedCitation>();
                }

                return parsed.Citations.Select(c => new DetectedCitation
                {
                    Url = c.Url ?? "",
                    Domain = c.Domain ?? ExtractDomain(c.Url ?? ""),
                    PagePath = c.Title ?? "",
                    CitationType = c.CitationType ?? "Reference",
                    Position = c.Position,
                    Context = c.Context ?? ""
                }).ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing LLM citation JSON: {Response}", llmJsonResponse);
                return new List<DetectedCitation>();
            }
        }

        private List<string> ExtractUrls(string text)
        {
            // Match http/https URLs
            var urlPattern = @"https?://[^\s<>""'\)]+";
            var matches = Regex.Matches(text, urlPattern);

            var urls = new List<string>();
            foreach (Match match in matches)
            {
                var url = match.Value.TrimEnd('.', ',', '!', '?', ';', ':');
                if (IsValidUrl(url))
                {
                    urls.Add(url);
                }
            }

            return urls.Distinct().ToList();
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private string ExtractDomain(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host.Replace("www.", "");
            }
            catch
            {
                return url;
            }
        }

        private string ExtractPath(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.PathAndQuery;
            }
            catch
            {
                return "";
            }
        }

        private string DetermineCitationType(string text, string url)
        {
            var urlIndex = text.IndexOf(url, StringComparison.OrdinalIgnoreCase);
            if (urlIndex == -1)
                return "Reference";

            // Check surrounding text for citation indicators
            var start = Math.Max(0, urlIndex - 50);
            var length = Math.Min(100, text.Length - start);
            var context = text.Substring(start, length).ToLower();

            if (context.Contains("according to") || context.Contains("source:") || context.Contains("via"))
                return "DirectLink";

            if (context.Contains("see") || context.Contains("read more") || context.Contains("learn more"))
                return "Reference";

            if (context.Contains("[") || context.Contains("]"))
                return "Inline";

            return "Mention";
        }

        private int FindUrlPosition(List<string> sentences, string url)
        {
            for (int i = 0; i < sentences.Count; i++)
            {
                if (sentences[i].Contains(url, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1;
                }
            }
            return 0;
        }

        private string ExtractContext(List<string> sentences, string url)
        {
            foreach (var sentence in sentences)
            {
                if (sentence.Contains(url, StringComparison.OrdinalIgnoreCase))
                {
                    return sentence.Trim();
                }
            }
            return "";
        }

        private List<string> SplitIntoSentences(string text)
        {
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            return sentences;
        }

        // Helper class for LLM response parsing
        private class LlmCitationResponse
        {
            public List<LlmCitation>? Citations { get; set; }
        }

        private class LlmCitation
        {
            public string? Url { get; set; }
            public string? Domain { get; set; }
            public string? Title { get; set; }
            public int Position { get; set; }
            public string? CitationType { get; set; }
            public string? Context { get; set; }
        }
    }
}
