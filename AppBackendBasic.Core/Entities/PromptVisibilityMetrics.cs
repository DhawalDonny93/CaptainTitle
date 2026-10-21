using System;

namespace AppBackendBasic.Core.Entities
{
    public class PromptVisibilityMetrics
    {
        public int Id { get; set; }
        public int PromptPlatformId { get; set; }
        public int TeamId { get; set; }
        public DateTime CheckDate { get; set; }
        public double VisibilityScore { get; set; }
        public int? Position { get; set; }
        public bool BrandMentioned { get; set; }
        public string Sentiment { get; set; } = "Neutral"; // Positive, Neutral, Negative
        public double? SentimentScore { get; set; }
        public string? ResponseText { get; set; }

        /// <summary>
        /// Full conversation content in structured format (JSON)
        /// Contains: prompt, response, platform, timestamp, metadata
        /// </summary>
        public string? ConversationContent { get; set; }

        public bool IsCompetitor { get; set; } = false; // false = Own brand, true = Competitor
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual PromptPlatformTracking PromptPlatform { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
    }
}
