using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class AiTrackingResult
    {
        [Key]
        public int Id { get; set; }

        public int PromptId { get; set; }

        [ForeignKey("PromptId")]
        public virtual PromptTracking Prompt { get; set; }

        // chatgpt, perplexity, claude, gemini, deepseek
        [Required]
        [MaxLength(50)]
        public string AiEngine { get; set; }

        [MaxLength(10)]
        public string? CountryCode { get; set; }

        public string? ResponseText { get; set; }

        public bool BrandMentioned { get; set; }

        public int? MentionPosition { get; set; }

        // 0-100 score indicating visibility
        public int VisibilityScore { get; set; }

        // Positive, Neutral, Negative
        [MaxLength(20)]
        public string? Sentiment { get; set; }

        public double? SentimentScore { get; set; }

        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
