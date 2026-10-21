using System;

namespace AppBackendBasic.Core.Entities
{
    public class VisibilityOpportunity
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public int? PromptTrackingId { get; set; } // FK to PromptTrackings.Id — used for efficient integer dedup instead of text comparison
        public string PromptText { get; set; } = string.Empty; // Kept for display; dedup key is PromptTrackingId when available
        public string Platform { get; set; } = string.Empty; // ChatGPT, Claude, Gemini, Perplexity
        public string OpportunityType { get; set; } = string.Empty; // NotMentioned, MentionedLate, NegativeMention, CompetitorDominance
        public int? CurrentPosition { get; set; }
        public string? CompetitorsMentionedJson { get; set; }
        public string? Reasoning { get; set; }
        public string? RecommendedContent { get; set; }
        public string? OptimizationStrategy { get; set; }
        public int Priority { get; set; } = 3; // 1=High, 2=Medium, 3=Low (matches DB default of 3)
        public string Status { get; set; } = "New"; // New, InProgress, Actioned, Dismissed
        public DateTime? ActionedAt { get; set; }
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual PromptTracking? PromptTracking { get; set; }
    }
}
