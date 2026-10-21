using System;

namespace AppBackendBasic.Core.Entities
{
    public class ContentGapOpportunity
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public int? OpportunityScanId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RecommendedContentType { get; set; }
        public string EstimatedImpact { get; set; } = "Medium"; // High, Medium, Low (VARCHAR in DB)
        public int CompetitorCoverage { get; set; } = 0; // INTEGER in DB
        public int SearchVolume { get; set; } = 0;
        public int Difficulty { get; set; } = 0;
        public string Status { get; set; } = "New"; // New, InProgress, Implemented, Dismissed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual OpportunityScan? OpportunityScan { get; set; }
    }
}
