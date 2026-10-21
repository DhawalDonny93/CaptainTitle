using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.API.DTOs
{
    public class CreateContentArticleDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Slug { get; set; }

        public string? ContentHtml { get; set; }

        public string? ContentMarkdown { get; set; }

        [MaxLength(255)]
        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? FeaturedImageUrl { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        public DateTime? ScheduledPublishAt { get; set; }

        public int WordCount { get; set; }

        [MaxLength(100)]
        public string? AiModelUsed { get; set; }

        public string? GenerationPrompt { get; set; }

        public int SeoScore { get; set; }
    }
}
