using System;

namespace AppBackendBasic.Core.Entities
{
    public class LlmRequest
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ProjectId { get; set; }
        public int ProviderId { get; set; }
        public string Model { get; set; } = string.Empty; // gpt-4, claude-3-opus, gemini-pro
        public string RequestType { get; set; } = string.Empty; // ContentGeneration, PromptTest, CitationAnalysis, DraftReply
        public string PromptText { get; set; } = string.Empty;
        public string? SystemPrompt { get; set; }
        public decimal Temperature { get; set; } = 0.7m;
        public int MaxTokens { get; set; } = 2000;
        public int? RequestTokens { get; set; }
        public int? ResponseTokens { get; set; }
        public int? TotalTokens { get; set; }
        public decimal? Cost { get; set; } // USD
        public string? ResponseText { get; set; }
        public string? ResponseJson { get; set; } // JSONB - Structured response
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed, RateLimited
        public string? ErrorMessage { get; set; }
        public int? LatencyMs { get; set; } // Response time in milliseconds
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Project? Project { get; set; }
        public virtual LlmProvider Provider { get; set; } = null!;
    }
}
