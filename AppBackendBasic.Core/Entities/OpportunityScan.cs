using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.Entities
{
    public class OpportunityScan
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public string ScanType { get; set; } = string.Empty; // Website, Community, Competitor, ContentGap
        public string? TargetUrl { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
        public int TotalOpportunities { get; set; }
        public int HighPriority { get; set; }
        public int MediumPriority { get; set; }
        public int LowPriority { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual ICollection<ContentGapOpportunity> ContentGapOpportunities { get; set; } = new List<ContentGapOpportunity>();
    }
}
