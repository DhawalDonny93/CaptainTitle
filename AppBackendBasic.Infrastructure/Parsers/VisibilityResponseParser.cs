using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Parsers
{
    public class VisibilityResponseParser
    {
        private readonly ILogger<VisibilityResponseParser> _logger;

        public VisibilityResponseParser(ILogger<VisibilityResponseParser> logger)
        {
            _logger = logger;
        }

        public VisibilityAnalysis ParseResponse(string promptText, string responseText, string brandName, string? brandDomain)
        {
            var analysis = new VisibilityAnalysis
            {
                BrandMentioned = false,
                MentionPositions = new List<int>(),
                MentionContexts = new List<string>(),
                CompetitorsMentioned = new List<CompetitorMention>(),
                Citations = new List<string>(),
                KeyPhrases = new List<string>()
            };

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return analysis;
            }

            // Split into sentences
            var sentences = SplitIntoSentences(responseText);

            // Find brand mentions
            for (int i = 0; i < sentences.Count; i++)
            {
                if (ContainsBrandMention(sentences[i], brandName, brandDomain))
                {
                    analysis.BrandMentioned = true;
                    analysis.MentionPositions.Add(i + 1);
                    analysis.MentionContexts.Add(sentences[i].Trim());
                }
            }

            // Calculate visibility score
            analysis.VisibilityScore = CalculateVisibilityScore(analysis, sentences.Count);

            // Detect mention type
            analysis.MentionType = DetectMentionType(analysis.MentionContexts, brandName);

            // Extract URLs/citations
            analysis.Citations = ExtractUrls(responseText);

            // Extract key phrases (simple implementation)
            analysis.KeyPhrases = ExtractKeyPhrases(responseText, brandName);

            return analysis;
        }

        public VisibilityAnalysis ParseLlmAnalysisResponse(string llmJsonResponse)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var parsed = JsonSerializer.Deserialize<LlmBrandMentionAnalysis>(llmJsonResponse, options);

                if (parsed == null)
                {
                    _logger.LogWarning("Failed to parse LLM analysis response");
                    return new VisibilityAnalysis();
                }

                return new VisibilityAnalysis
                {
                    BrandMentioned = parsed.IsMentioned,
                    MentionPositions = parsed.Positions ?? new List<int>(),
                    MentionContexts = parsed.Contexts ?? new List<string>(),
                    MentionType = parsed.MentionType ?? "Unknown",
                    VisibilityScore = CalculateVisibilityScoreFromPositions(parsed.Positions),
                    CompetitorsMentioned = parsed.CompetitorsMentioned?.Select((name, idx) => new CompetitorMention
                    {
                        CompetitorName = name,
                        Position = parsed.CompetitorPositions != null && idx < parsed.CompetitorPositions.Count 
                            ? parsed.CompetitorPositions[idx] 
                            : 0,
                        Context = ""
                    }).ToList() ?? new List<CompetitorMention>(),
                    Citations = new List<string>(),
                    KeyPhrases = new List<string>()
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing LLM analysis JSON: {Response}", llmJsonResponse);
                return new VisibilityAnalysis();
            }
        }

        private List<string> SplitIntoSentences(string text)
        {
            // Split by common sentence endings
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            return sentences;
        }

        private bool ContainsBrandMention(string sentence, string brandName, string? brandDomain)
        {
            var sentenceLower = sentence.ToLower();
            var brandLower = brandName.ToLower();

            // Check for brand name
            if (sentenceLower.Contains(brandLower))
                return true;

            // Check for domain if provided
            if (!string.IsNullOrEmpty(brandDomain))
            {
                var domainLower = brandDomain.ToLower().Replace("www.", "");
                if (sentenceLower.Contains(domainLower))
                    return true;
            }

            return false;
        }

        private double CalculateVisibilityScore(VisibilityAnalysis analysis, int totalSentences)
        {
            if (!analysis.BrandMentioned || analysis.MentionPositions.Count == 0)
                return 0;

            double score = 50; // Base score for being mentioned

            // Position bonus (earlier is better)
            var firstPosition = analysis.MentionPositions.Min();
            if (firstPosition == 1) score += 30;
            else if (firstPosition <= 3) score += 20;
            else if (firstPosition <= 5) score += 10;
            else if (firstPosition <= 10) score += 5;

            // Frequency bonus
            var mentionCount = analysis.MentionPositions.Count;
            score += Math.Min(mentionCount * 5, 20);

            return Math.Min(score, 100);
        }

        private double CalculateVisibilityScoreFromPositions(List<int>? positions)
        {
            if (positions == null || positions.Count == 0)
                return 0;

            double score = 50;
            var firstPosition = positions.Min();

            if (firstPosition == 1) score += 30;
            else if (firstPosition <= 3) score += 20;
            else if (firstPosition <= 5) score += 10;
            else if (firstPosition <= 10) score += 5;

            score += Math.Min(positions.Count * 5, 20);

            return Math.Min(score, 100);
        }

        private string DetectMentionType(List<string> contexts, string brandName)
        {
            if (contexts.Count == 0)
                return "None";

            var combinedContext = string.Join(" ", contexts).ToLower();

            // Check for comparison keywords
            if (combinedContext.Contains("vs") || combinedContext.Contains("versus") || 
                combinedContext.Contains("compared to") || combinedContext.Contains("alternative"))
                return "Comparison";

            // Check for negative keywords
            if (combinedContext.Contains("however") || combinedContext.Contains("but") ||
                combinedContext.Contains("unfortunately") || combinedContext.Contains("drawback"))
                return "Negative";

            // Check for recommendation/alternative keywords
            if (combinedContext.Contains("instead") || combinedContext.Contains("rather than") ||
                combinedContext.Contains("alternative to"))
                return "Alternative";

            // Default to direct mention
            return "Direct";
        }

        private List<string> ExtractUrls(string text)
        {
            var urlPattern = @"https?://[^\s<>""]+|www\.[^\s<>""]+";
            var matches = Regex.Matches(text, urlPattern);

            return matches.Select(m => m.Value).Distinct().ToList();
        }

        private List<string> ExtractKeyPhrases(string text, string brandName)
        {
            // Simple implementation - extract phrases around brand mentions
            var phrases = new List<string>();
            var sentences = SplitIntoSentences(text);

            foreach (var sentence in sentences)
            {
                if (sentence.ToLower().Contains(brandName.ToLower()))
                {
                    // Extract 3-5 word phrases
                    var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length >= 3)
                    {
                        phrases.Add(string.Join(" ", words.Take(Math.Min(5, words.Length))));
                    }
                }
            }

            return phrases.Take(5).ToList();
        }

        // Helper class for LLM response parsing
        private class LlmBrandMentionAnalysis
        {
            public bool IsMentioned { get; set; }
            public List<int>? Positions { get; set; }
            public List<string>? Contexts { get; set; }
            public string? MentionType { get; set; }
            public string? Sentiment { get; set; }
            public double SentimentScore { get; set; }
            public List<string>? CompetitorsMentioned { get; set; }
            public List<int>? CompetitorPositions { get; set; }
        }
    }

    public class VisibilityAnalysis
    {
        public bool BrandMentioned { get; set; }
        public List<int> MentionPositions { get; set; } = new();
        public List<string> MentionContexts { get; set; } = new();
        public string MentionType { get; set; } = "None";
        public double VisibilityScore { get; set; }
        public List<CompetitorMention> CompetitorsMentioned { get; set; } = new();
        public List<string> Citations { get; set; } = new();
        public List<string> KeyPhrases { get; set; } = new();
    }

    public class CompetitorMention
    {
        public string CompetitorName { get; set; } = string.Empty;
        public int Position { get; set; }
        public string Context { get; set; } = string.Empty;
    }
}
