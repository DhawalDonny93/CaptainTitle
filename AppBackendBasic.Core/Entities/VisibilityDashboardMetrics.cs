using System;

namespace AppBackendBasic.Core.Entities
{
    public class VisibilityDashboardMetrics
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public DateTime MetricDate { get; set; }
        public string Period { get; set; } = "daily"; // daily, weekly, monthly
        
        // Overall Visibility
        public int OverallVisibilityScore { get; set; }
        public int? VisibilityRank { get; set; }
        public decimal? VisibilityChange { get; set; }
        
        // Prompt Performance
        public int TotalPrompts { get; set; }
        public int ActivePrompts { get; set; }
        public decimal AveragePromptScore { get; set; }
        public int PromptsWithMentions { get; set; }
        public decimal MentionRate { get; set; }
        
        // Citation Metrics
        public int TotalCitations { get; set; }
        public decimal CitationShare { get; set; }
        public int? CitationRank { get; set; }
        public int UniqueDomainsCiting { get; set; }
        public int OwnedDomainCitations { get; set; }
        
        // Site Health
        public int SiteAuditScore { get; set; }
        public int CriticalIssues { get; set; }
        public int WarningIssues { get; set; }
        public int PassedChecks { get; set; }
        
        // Opportunities
        public int TotalOpportunities { get; set; }
        public int HighPriorityOpportunities { get; set; }
        public int OpportunitiesActioned { get; set; }
        
        // Platform Breakdown
        public string? PlatformMetrics { get; set; } // JSON
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
    }
}
