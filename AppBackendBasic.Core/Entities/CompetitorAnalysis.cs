using System;

namespace AppBackendBasic.Core.Entities
{
    public class CompetitorAnalysis
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string CompetitorDomain { get; set; } = string.Empty;
        public string? CompetitorName { get; set; }
        public int VisibilityScore { get; set; }
        public decimal CitationShare { get; set; }
        public int ContentCount { get; set; }
        public string? TopTopics { get; set; } // JSON array
        public string? Strengths { get; set; } // JSON array
        public string? Weaknesses { get; set; } // JSON array
        public DateTime? LastAnalyzedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
    }
}
