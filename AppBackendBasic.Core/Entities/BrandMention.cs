using System;

namespace AppBackendBasic.Core.Entities
{
    public class BrandMention
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
        public bool IsMentioned { get; set; }

        /// <summary>
        /// Number of times the brand was mentioned in the response
        /// </summary>
        public int MentionCount { get; set; } = 0;

        public int? Position { get; set; }
        public string MentionType { get; set; } = "Direct"; // Direct, Indirect, Comparison
        public string? MentionContext { get; set; }
        public string Sentiment { get; set; } = "Neutral"; // Positive, Neutral, Negative
        public double? SentimentScore { get; set; }
        public string? ResponseText { get; set; }
        public bool IsCompetitor { get; set; } = false; // false = Own brand, true = Competitor

        /// <summary>
        /// Reference to the Competitor if this mention is for a competitor brand
        /// </summary>
        public int? CompetitorId { get; set; }

        /// <summary>
        /// Type of competitor: Direct (manually added) or SERP (found during tracking)
        /// Only populated when IsCompetitor = true
        /// </summary>
        public string? CompetitorType { get; set; } // Direct, SERP

        public DateTime CheckDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual Competitor? Competitor { get; set; }
    }
}
