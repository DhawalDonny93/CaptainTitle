using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class OpportunityTask
    {
        [Key]
        public int Id { get; set; }

        public int OpportunityId { get; set; }

        [Required]
        [MaxLength(50)]
        public string OpportunitySource { get; set; } = string.Empty; // Visibility, ContentGap, SEO

        public int TaskId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; } = null!;
    }
}
