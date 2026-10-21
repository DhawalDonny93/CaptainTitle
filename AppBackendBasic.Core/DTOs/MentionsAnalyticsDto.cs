using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.DTOs
{
    public class MentionsAnalyticsDto
    {
        public MentionsInsightsDto Insights { get; set; } = new();
        public List<OpportunityDto> Opportunities { get; set; } = new();
        public MentionsChartsDto Analytics { get; set; } = new();
        public List<TopicDto> Topics { get; set; } = new();
        public List<MentionDto> Mentions { get; set; } = new();
    }

    public class MentionsInsightsDto
    {
        public int TotalMentions { get; set; }
        public int TotalCitations { get; set; }
        public string TopPlatform { get; set; } = string.Empty;
        public List<string> KeyInsights { get; set; } = new();
    }

    public class MentionsChartsDto
    {
        public MentionsTimeSeriesDto PerformanceOverTime { get; set; } = new();
        public List<PositionDistributionDto> PositionDistribution { get; set; } = new();
    }

    public class MentionsTimeSeriesDto
    {
        public List<string> Dates { get; set; } = new();
        public Dictionary<string, List<int>> BrandData { get; set; } = new();
    }

    public class PositionDistributionDto
    {
        public string Brand { get; set; } = string.Empty;
        public Dictionary<string, int> Positions { get; set; } = new();
    }

    public class TopicDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Status { get; set; } = string.Empty;
        public double PerformanceScore { get; set; }
    }

    public class MentionDto
    {
        public string Prompt { get; set; } = string.Empty;
        public bool Mentioned { get; set; }
        public string Locations { get; set; } = string.Empty;
        public int Mentions { get; set; }
        public int Citations { get; set; }
        public double? AvgPosition { get; set; }
        public bool IsCompetitor { get; set; } = false;
        public List<CompetitorMentionDto> Competitors { get; set; } = new();
    }

    public class CompetitorMentionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
