using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class CommunityOpportunity
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        public int TeamId { get; set; }

        [ForeignKey("TeamId")]
        public virtual Team Team { get; set; }

        public int? KeywordId { get; set; }

        [ForeignKey("KeywordId")]
        public virtual Keyword? Keyword { get; set; }

        // Reddit, Quora
        [Required]
        [MaxLength(50)]
        public string Platform { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; }

        [Required]
        public string Url { get; set; }

        public string? Summary { get; set; }

        // How the topic is discussed: Positive, Negative, Neutral, Question
        [MaxLength(50)]
        public string? Sentiment { get; set; }

        // New, Drafted, Posted, Ignored
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "New";

        public string? DraftReply { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Phase 5 additions
        public int Priority { get; set; } // 1-5
        public int EngagementScore { get; set; } // 0-100
        public int Upvotes { get; set; }
        public int Comments { get; set; }
        public int Views { get; set; }
        public DateTime? PostedAt { get; set; }
        public DateTime? LastCheckedAt { get; set; }
        public bool IsArchived { get; set; }
    }
}
