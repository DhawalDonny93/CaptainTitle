using System;

namespace AppBackendBasic.Core.Entities
{
    public class VisibilityGoal
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public string GoalType { get; set; } = string.Empty; // VisibilityScore, CitationShare, MentionRate, SiteScore
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Active"; // Active, Achieved, Failed, Cancelled
        public decimal Progress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
    }
}
