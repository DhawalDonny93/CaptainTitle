using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.DTOs
{
    public class StartOpportunityScanRequest
    {
        public int ProjectId { get; set; }
        public string ScanType { get; set; } = string.Empty; // Website, Community, Competitor, ContentGap
        public string? TargetUrl { get; set; }
    }

    public class OpportunityFilters
    {
        public string? Platform { get; set; }
        public string? Status { get; set; }
        public int? MinPriority { get; set; }
        public int? MinEngagementScore { get; set; }
        public bool? IsArchived { get; set; }
    }

    public class UpdateOpportunityStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class OpportunityDashboard
    {
        public int TotalOpportunities { get; set; }
        public int HighPriority { get; set; }
        public int MediumPriority { get; set; }
        public int LowPriority { get; set; }
        public int CommunityOpportunities { get; set; }
        public int ContentGaps { get; set; }
        public int CompetitorInsights { get; set; }
        public List<OpportunitySummary> RecentOpportunities { get; set; } = new();
        public List<PlatformStats> PlatformBreakdown { get; set; } = new();
    }

    public class OpportunitySummary
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PlatformStats
    {
        public string Platform { get; set; } = string.Empty;
        public int Count { get; set; }
        public int AvgEngagementScore { get; set; }
    }

    public class OpportunityTrend
    {
        public DateTime Date { get; set; }
        public int TotalOpportunities { get; set; }
        public int CommunityOpportunities { get; set; }
        public int ContentGaps { get; set; }
        public int AvgEngagementScore { get; set; }
    }

    /// <summary>
    /// Unified opportunity response DTO for the frontend OpportunitiesFound page.
    /// Maps all opportunity types (Visibility, ContentGap, SEO) to a common shape.
    /// Frontend expects: id, title, description, priority (string label), opportunityType, platform, status, estimatedImpact, discoveredAt, createdAt
    /// </summary>
    public class OpportunityResponseDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string OpportunityType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // "Visibility", "ContentGap", "SEO"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium"; // "Critical", "High", "Medium", "Low"
        public string Status { get; set; } = string.Empty;
        public int EstimatedImpact { get; set; } // 0-100 numeric for sorting/display
        public string? SourceUrl { get; set; }
        public string? OptimizationStrategy { get; set; }
        public DateTime DiscoveredAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
