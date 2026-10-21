using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.Entities
{
    public class Citation
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
        public string SourceDomain { get; set; } = string.Empty;
        public string SourceTitle { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // ChatGPT, Claude, Perplexity, Gemini
        public string? PromptText { get; set; }
        public int? Position { get; set; }
        public string CitationType { get; set; } = "DirectLink"; // DirectLink, Inline, Reference

        /// <summary>
        /// Content type classification: Editorial, Review, Institutional, Competitor, Social, Other
        /// </summary>
        public string ContentType { get; set; } = "Other";

        /// <summary>
        /// Number of times this source has been cited across all prompts
        /// </summary>
        public int CitationCount { get; set; } = 1;

        /// <summary>
        /// Last time this citation appeared in a response
        /// </summary>
        public DateTime? LastSeenAt { get; set; }

        public string? Context { get; set; }
        public bool IsCompetitor { get; set; } = false; // false = Own brand, true = Competitor

        /// <summary>
        /// Reference to the Competitor if this citation is for a competitor brand
        /// </summary>
        public int? CompetitorId { get; set; }

        /// <summary>
        /// Type of competitor: Direct (manually added) or SERP (found during tracking)
        /// Only populated when IsCompetitor = true
        /// </summary>
        public string? CompetitorType { get; set; } // Direct, SERP

        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual Competitor? Competitor { get; set; }
        public virtual ICollection<CitationMetric> Metrics { get; set; } = new List<CitationMetric>();
    }
}
