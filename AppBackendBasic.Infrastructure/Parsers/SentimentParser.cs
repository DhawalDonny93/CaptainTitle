using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Parsers
{
    public class SentimentParser
    {
        private readonly ILogger<SentimentParser> _logger;

        public SentimentParser(ILogger<SentimentParser> logger)
        {
            _logger = logger;
        }

        public SentimentAnalysisResponse ParseLlmSentimentResponse(string llmJsonResponse)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var parsed = JsonSerializer.Deserialize<SentimentAnalysisResponse>(llmJsonResponse, options);

                if (parsed == null)
                {
                    _logger.LogWarning("Failed to parse LLM sentiment response");
                    return CreateDefaultResponse();
                }

                return parsed;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing LLM sentiment JSON: {Response}", llmJsonResponse);
                return CreateDefaultResponse();
            }
        }

        public SentimentAnalysisResponse AnalyzeTextBasic(string text)
        {
            // Fallback basic sentiment analysis without LLM
            var response = new SentimentAnalysisResponse
            {
                Success = true,
                Confidence = 0.6 // Lower confidence for basic analysis
            };

            var textLower = text.ToLower();

            // Count positive and negative words
            var positiveCount = CountPositiveWords(textLower);
            var negativeCount = CountNegativeWords(textLower);

            // Determine sentiment
            if (positiveCount > negativeCount)
            {
                response.OverallSentiment = "Positive";
                response.SentimentScore = 0.5 + (Math.Min(positiveCount - negativeCount, 5) * 0.1);
            }
            else if (negativeCount > positiveCount)
            {
                response.OverallSentiment = "Negative";
                response.SentimentScore = -0.5 - (Math.Min(negativeCount - positiveCount, 5) * 0.1);
            }
            else
            {
                response.OverallSentiment = "Neutral";
                response.SentimentScore = 0.0;
            }

            // Basic emotion detection
            response.Emotions = new Dictionary<string, double>
            {
                ["joy"] = CountWords(textLower, _joyWords) * 0.2,
                ["anger"] = CountWords(textLower, _angerWords) * 0.2,
                ["sadness"] = CountWords(textLower, _sadnessWords) * 0.2,
                ["fear"] = CountWords(textLower, _fearWords) * 0.2,
                ["surprise"] = CountWords(textLower, _surpriseWords) * 0.2
            };

            // Extract key phrases
            response.KeyPhrases = ExtractKeyPhrases(text);

            response.Reasoning = $"Basic analysis: {positiveCount} positive words, {negativeCount} negative words";

            return response;
        }

        private int CountPositiveWords(string text)
        {
            return CountWords(text, _positiveWords);
        }

        private int CountNegativeWords(string text)
        {
            return CountWords(text, _negativeWords);
        }

        private int CountWords(string text, string[] wordList)
        {
            int count = 0;
            foreach (var word in wordList)
            {
                count += Regex.Matches(text, $@"\b{word}\b").Count;
            }
            return count;
        }

        private List<string> ExtractKeyPhrases(string text)
        {
            // Simple implementation - extract sentences with strong sentiment words
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var keyPhrases = new List<string>();

            foreach (var sentence in sentences)
            {
                var sentenceLower = sentence.ToLower();
                if (_positiveWords.Any(w => sentenceLower.Contains(w)) ||
                    _negativeWords.Any(w => sentenceLower.Contains(w)))
                {
                    // Take first 10 words of the sentence
                    var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    keyPhrases.Add(string.Join(" ", words.Take(Math.Min(10, words.Length))));
                }
            }

            return keyPhrases.Take(5).ToList();
        }

        private SentimentAnalysisResponse CreateDefaultResponse()
        {
            return new SentimentAnalysisResponse
            {
                Success = false,
                OverallSentiment = "Neutral",
                SentimentScore = 0.0,
                Confidence = 0.0,
                Emotions = new Dictionary<string, double>(),
                KeyPhrases = new List<string>(),
                ErrorMessage = "Failed to parse sentiment analysis"
            };
        }

        #region Word Lists

        private static readonly string[] _positiveWords = new[]
        {
            "excellent", "great", "good", "amazing", "wonderful", "fantastic", "outstanding",
            "superb", "brilliant", "perfect", "best", "love", "recommend", "impressive",
            "quality", "reliable", "effective", "helpful", "valuable", "beneficial",
            "positive", "success", "advantage", "benefit", "improvement", "better"
        };

        private static readonly string[] _negativeWords = new[]
        {
            "bad", "poor", "terrible", "awful", "horrible", "worst", "disappointing",
            "useless", "waste", "problem", "issue", "difficult", "hard", "fail",
            "negative", "unfortunately", "however", "but", "lacking", "missing",
            "expensive", "overpriced", "slow", "complicated", "confusing", "frustrating"
        };

        private static readonly string[] _joyWords = new[]
        {
            "happy", "joy", "excited", "delighted", "pleased", "glad", "cheerful",
            "love", "wonderful", "amazing", "fantastic", "excellent"
        };

        private static readonly string[] _angerWords = new[]
        {
            "angry", "furious", "mad", "annoyed", "frustrated", "irritated",
            "outraged", "upset", "terrible", "awful", "horrible"
        };

        private static readonly string[] _sadnessWords = new[]
        {
            "sad", "unhappy", "disappointed", "depressed", "miserable", "unfortunate",
            "regret", "sorry", "poor", "bad"
        };

        private static readonly string[] _fearWords = new[]
        {
            "afraid", "scared", "worried", "anxious", "nervous", "concerned",
            "frightened", "terrified", "panic", "risk"
        };

        private static readonly string[] _surpriseWords = new[]
        {
            "surprised", "shocked", "amazed", "astonished", "unexpected",
            "incredible", "unbelievable", "wow", "remarkable"
        };

        #endregion
    }
}
