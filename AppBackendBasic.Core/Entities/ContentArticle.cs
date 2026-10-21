using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class ContentArticle
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public int TeamId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [ForeignKey("TeamId")]
        public virtual Team Team { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [MaxLength(255)]
        public string? Slug { get; set; }

        public string? ContentHtml { get; set; }

        public string? ContentMarkdown { get; set; }

        [MaxLength(255)]
        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? FeaturedImageUrl { get; set; }

        // Draft, Scheduled, Published, Archived
        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        public DateTime? ScheduledPublishAt { get; set; }

        public DateTime? PublishedAt { get; set; }

        public int WordCount { get; set; }

        [MaxLength(100)]
        public string? AiModelUsed { get; set; }

        public string? GenerationPrompt { get; set; }

        public int SeoScore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
