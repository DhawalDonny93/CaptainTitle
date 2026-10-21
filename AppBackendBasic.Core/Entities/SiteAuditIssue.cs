using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class SiteAuditIssue
    {
        [Key]
        public int Id { get; set; }

        public int SiteAuditRunId { get; set; }

        public int TeamId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Severity { get; set; } = "Medium"; // Critical, High, Medium, Low

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "Technical"; // Content, AEO, Technical

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty; // Schema, Meta, Links, etc.

        public int AffectedPages { get; set; } = 0;

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        public string? Recommendation { get; set; }

        public bool AutoFixAvailable { get; set; } = false;

        public int Priority { get; set; } = 3; // 1-5, higher is more important

        public int EstimatedImpactScore { get; set; } = 0; // 0-100

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SiteAuditRunId")]
        public virtual SiteAuditRun SiteAuditRun { get; set; } = null!;

        [ForeignKey("TeamId")]
        public virtual Team Team { get; set; } = null!;
    }
}
