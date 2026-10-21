using System;
using System.Collections.Generic;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.DTOs
{
    public class DashboardOverview
    {
        public int OverallVisibilityScore { get; set; }
        public decimal VisibilityChange { get; set; }
        public int TotalPrompts { get; set; }
        public int ActivePrompts { get; set; }
        public decimal MentionRate { get; set; }
        public int TotalCitations { get; set; }
        public decimal CitationShare { get; set; }
        public int SiteAuditScore { get; set; }
        public int TotalOpportunities { get; set; }
        public int HighPriorityOpportunities { get; set; }
        public decimal AveragePosition { get; set; }
        public string TrendDirection { get; set; } = "stable"; // up, down, stable
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public List<PlatformMetric> PlatformBreakdown { get; set; } = new();
        public List<VisibilityAlert> RecentAlerts { get; set; } = new();
        public List<VisibilityGoal> ActiveGoals { get; set; } = new();
    }

    public class PlatformMetric
    {
        public string Platform { get; set; } = string.Empty;
        public int VisibilityScore { get; set; }
        public int Citations { get; set; }
        public int MentionCount { get; set; }
        public decimal MentionRate { get; set; }
        public decimal AveragePosition { get; set; }
        public decimal Change { get; set; }
    }

    public class TrendData
    {
        public List<TrendPoint> VisibilityTrend { get; set; } = new();
        public List<TrendPoint> CitationTrend { get; set; } = new();
        public List<TrendPoint> MentionRateTrend { get; set; } = new();
        public List<TrendPoint> SiteScoreTrend { get; set; } = new();
    }

    public class TrendPoint
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public class CompetitiveAnalysisDto
    {
        public string ProjectDomain { get; set; } = string.Empty;
        public int ProjectVisibilityScore { get; set; }
        public decimal ProjectCitationShare { get; set; }
        public int ProjectRank { get; set; }
        public List<CompetitorComparisonDto> Competitors { get; set; } = new();
        public decimal MarketShare { get; set; }
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> Opportunities { get; set; } = new();
    }

    public class CompetitorComparisonDto
    {
        public string Domain { get; set; } = string.Empty;
        public int VisibilityScore { get; set; }
        public decimal CitationShare { get; set; }
        public int Rank { get; set; }
        public int ScoreDifference { get; set; }
        public decimal ShareDifference { get; set; }
        public string Status { get; set; } = string.Empty; // Leading, Competitive, Lagging
    }

    public class CreateGoalDto
    {
        public int ProjectId { get; set; }
        public string GoalType { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public DateTime TargetDate { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateGoalDto
    {
        public decimal? TargetValue { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? Status { get; set; }
    }
}
