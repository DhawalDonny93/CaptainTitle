using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.Entities
{
    public class PromptPlatformTracking
    {
        public int Id { get; set; }
        public int PromptId { get; set; }
        public int TeamId { get; set; }
        public string Platform { get; set; } = string.Empty; // ChatGPT, Claude, Perplexity, Gemini
        public bool IsCompetitor { get; set; } = false; // false = Own brand, true = Competitor
        public bool IsEnabled { get; set; } = true;
        public DateTime? LastCheckDate { get; set; }
        public double? CurrentVisibilityScore { get; set; }
        public bool? BrandMentioned { get; set; }
        public int? MentionPosition { get; set; }
        public string? ResponseSummary { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual PromptTracking Prompt { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual ICollection<PromptVisibilityMetrics> VisibilityMetrics { get; set; } = new List<PromptVisibilityMetrics>();
    }
}
