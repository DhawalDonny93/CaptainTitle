namespace AppBackendBasic.Core.DTOs
{
    public class ContentCreationDataDto
    {
        public List<TopicOptionDto> Topics { get; set; } = new();
        public List<PromptOptionDto> Prompts { get; set; } = new();
        public List<CitationOptionDto> Citations { get; set; } = new();
        public List<BrandKitOptionDto> BrandKits { get; set; } = new();
        public List<AudienceSegmentOptionDto> AudienceSegments { get; set; } = new();
    }

    public class TopicOptionDto
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SearchVolume { get; set; }
    }

    public class PromptOptionDto
    {
        public int Id { get; set; }
        public string PromptText { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int Priority { get; set; }
    }

    public class CitationOptionDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int CitationCount { get; set; }
        public string? Domain { get; set; }
    }

    public class BrandKitOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class AudienceSegmentOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
