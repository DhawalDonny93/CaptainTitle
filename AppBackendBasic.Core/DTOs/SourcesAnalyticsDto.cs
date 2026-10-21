using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.DTOs
{
    public class SourcesAnalyticsDto
    {
        public SourcesInsightsDto Insights { get; set; } = new();
        public List<OpportunityDto> Opportunities { get; set; } = new();
        public SourcesChartsDto Analytics { get; set; } = new();
        public List<SourceDto> Sources { get; set; } = new();
        public List<ContentTypeDistributionDto> ContentTypeDistribution { get; set; } = new();
    }

    public class SourcesInsightsDto
    {
        public string TopSource { get; set; } = string.Empty;
        public int TopSourceCitations { get; set; }
        public string SecondSource { get; set; } = string.Empty;
        public double SecondSourcePercentage { get; set; }
        public string ThirdSource { get; set; } = string.Empty;
        public double ThirdSourcePercentage { get; set; }
    }

    public class OpportunityDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    public class SourcesChartsDto
    {
        public SourcesTimeSeriesDto Sources { get; set; } = new();
        public List<BrandSourceDto> BrandSources { get; set; } = new();
    }

    public class SourcesTimeSeriesDto
    {
        public List<string> Dates { get; set; } = new();
        public List<int> You { get; set; } = new();
        public List<int> Competitors { get; set; } = new();
        public List<int> Unattributed { get; set; } = new();
    }

    public class BrandSourceDto
    {
        public string Domain { get; set; } = string.Empty;
        public int Usage { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class SourceDto
    {
        public int Rank { get; set; }
        public string Domain { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public double Usage { get; set; }
        public double AvgCitations { get; set; }
        public List<string> PopularArticleTypes { get; set; } = new();
        public List<string> SourceTypes { get; set; } = new();
        public List<string> Platforms { get; set; } = new();
    }

    public class ContentTypeDistributionDto
    {
        public string Type { get; set; } = string.Empty;
        public double Percentage { get; set; }
        public int Count { get; set; }
    }
}
