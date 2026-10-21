using System;

namespace AppBackendBasic.Core.Entities
{
    public class SeoOpportunity
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public int? SiteAuditDetailId { get; set; }
        public string PageUrl { get; set; } = string.Empty;
        public string OpportunityType { get; set; } = string.Empty; // ThinContent, MissingMeta, PoorHeadings, etc.
        public string Priority { get; set; } = "Medium"; // Critical, High, Medium, Low
        public string? CurrentState { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        public int EstimatedImpact { get; set; } // 0-100
        public string? DetailsJson { get; set; }
        public string Status { get; set; } = "Open"; // Open, InProgress, Fixed, Dismissed
        public DateTime? FixedAt { get; set; }
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual SiteAuditDetail? SiteAuditDetail { get; set; }
    }
}
