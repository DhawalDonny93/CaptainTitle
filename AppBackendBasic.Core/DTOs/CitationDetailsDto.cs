using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.DTOs
{
    public class CitationDetailsDto
    {
        public string Domain { get; set; } = string.Empty;
        public int Rank { get; set; }
        public double CitationShare { get; set; }
        public double CitationShareChange { get; set; }
        public double GlobalShare { get; set; }
        public double GlobalShareChange { get; set; }
        public int TotalCitations { get; set; }
        public List<CitationPageDto> Pages { get; set; } = new();
        public List<CitationPromptDto> Prompts { get; set; } = new();
        public List<CitationTopicDto> Topics { get; set; } = new();
        public List<CitationPlatformDto> Platforms { get; set; } = new();
        public List<CitationRegionDto> Regions { get; set; } = new();
        public List<CitationShareDataPoint> ShareHistory { get; set; } = new();
    }

    public class CitationPageDto
    {
        public int Rank { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Share { get; set; }
        public double ShareChange { get; set; }
        public double GlobalShare { get; set; }
        public double GlobalShareChange { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    public class CitationPromptDto
    {
        public int Id { get; set; }
        public string PromptText { get; set; } = string.Empty;
        public int CitationCount { get; set; }
        public double AveragePosition { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    public class CitationTopicDto
    {
        public string Topic { get; set; } = string.Empty;
        public int CitationCount { get; set; }
        public double Share { get; set; }
    }

    public class CitationPlatformDto
    {
        public string Platform { get; set; } = string.Empty;
        public int CitationCount { get; set; }
        public double AveragePosition { get; set; }
        public double Share { get; set; }
    }

    public class CitationRegionDto
    {
        public string Region { get; set; } = string.Empty;
        public int CitationCount { get; set; }
        public double Share { get; set; }
    }

    public class CitationShareDataPoint
    {
        public DateTime Date { get; set; }
        public double Share { get; set; }
        public double GlobalShare { get; set; }
    }
}
