namespace AppBackendBasic.Core.DTOs
{
    public class LlmRequestDto
    {
        public int? UserId { get; set; }
        public int? ProjectId { get; set; }
        public string Provider { get; set; } = "OpenAI"; // OpenAI, Anthropic, Google
        public string Model { get; set; } = "gpt-4"; // gpt-4, claude-3-opus, gemini-pro
        public string RequestType { get; set; } = string.Empty; // ContentGeneration, PromptTest, etc.
        public string PromptText { get; set; } = string.Empty;
        public string? SystemPrompt { get; set; }
        public decimal Temperature { get; set; } = 0.7m;
        public int MaxTokens { get; set; } = 2000;
        public Dictionary<string, object>? AdditionalParams { get; set; }
    }

    public class LlmResponseDto
    {
        public bool Success { get; set; }
        public string? ResponseText { get; set; }
        public object? ResponseJson { get; set; }
        public int? RequestTokens { get; set; }
        public int? ResponseTokens { get; set; }
        public int? TotalTokens { get; set; }
        public decimal? Cost { get; set; }
        public int? LatencyMs { get; set; }
        public string? ErrorMessage { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
    }

    public class ContentGenerationRequest
    {
        public int ProjectId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public List<string> TargetPrompts { get; set; } = new();
        public List<string> CitationsToInclude { get; set; } = new();
        public string? BrandVoice { get; set; }
        public string? TargetAudience { get; set; }
        public string? AdditionalInstructions { get; set; }
        public int MinWordCount { get; set; } = 1500;
        public int MaxWordCount { get; set; } = 2000;
        public string Provider { get; set; } = "OpenAI";
    }

    public class ContentGenerationResponse
    {
        public bool Success { get; set; }
        public string? Title { get; set; }
        public string? MetaDescription { get; set; }
        public string? Content { get; set; }
        public List<string>? SuggestedHeadings { get; set; }
        public Dictionary<string, double>? KeywordDensity { get; set; }
        public int WordCount { get; set; }
        public decimal? Cost { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PromptTestRequest
    {
        public int ProjectId { get; set; }
        public string PromptText { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string? BrandDomain { get; set; }
        public List<string>? ProductNames { get; set; }
        public List<string> Platforms { get; set; } = new(); // ChatGPT, Claude, Perplexity, Gemini
        
        /// <summary>
        /// Optional JobKey to use for LLM configuration lookup in LlmJobConfigs.
        /// Defaults to "PromptTest" if not specified.
        /// Examples: "PromptTest", "OpportunityDetection"
        /// </summary>
        public string? RequestType { get; set; }
    }

    public class PromptTestResponse
    {
        public bool Success { get; set; }
        public List<PlatformTestResult> Results { get; set; } = new();
        public double AverageVisibilityScore { get; set; }
        public decimal TotalCost { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PlatformTestResult
    {
        public string Platform { get; set; } = string.Empty;
        public bool BrandMentioned { get; set; }
        public int? MentionPosition { get; set; }
        public string? MentionContext { get; set; }
        public string MentionType { get; set; } = string.Empty; // Direct, Indirect, Comparison
        public double VisibilityScore { get; set; }
        public string Sentiment { get; set; } = string.Empty; // Positive, Neutral, Negative
        public double SentimentScore { get; set; }
        public List<string> CitationsFound { get; set; } = new();
        public string ResponseText { get; set; } = string.Empty;
    }

    public class CitationAnalysisRequest
    {
        public string ResponseText { get; set; } = string.Empty;
        public int ProjectId { get; set; }
    }

    public class CitationAnalysisResponse
    {
        public bool Success { get; set; }
        public List<DetectedCitation> Citations { get; set; } = new();
        public int TotalCitations { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DetectedCitation
    {
        public string Url { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string PagePath { get; set; } = string.Empty;
        public string CitationType { get; set; } = string.Empty; // DirectLink, Reference, Inline
        public string Context { get; set; } = string.Empty;
        public int Position { get; set; }
    }

    public class SentimentAnalysisRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class SentimentAnalysisResponse
    {
        public bool Success { get; set; }
        public string OverallSentiment { get; set; } = string.Empty; // Positive, Neutral, Negative
        public double SentimentScore { get; set; } // -1.0 to 1.0
        public double Confidence { get; set; } // 0.0 to 1.0
        public Dictionary<string, double>? Emotions { get; set; } // joy, anger, sadness, fear, surprise
        public List<string>? KeyPhrases { get; set; }
        public string? Reasoning { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DraftReplyRequest
    {
        public int OpportunityId { get; set; }
        public int ProjectId { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // Reddit, Quora
        public string? Context { get; set; }
        public string? BrandVoice { get; set; }
        public string? ProductDescription { get; set; }
    }

    public class DraftReplyResponse
    {
        public bool Success { get; set; }
        public string? DraftReply { get; set; }
        public int WordCount { get; set; }
        public bool IsPromotional { get; set; }
        public double PromotionalPercentage { get; set; }
        public decimal? Cost { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
