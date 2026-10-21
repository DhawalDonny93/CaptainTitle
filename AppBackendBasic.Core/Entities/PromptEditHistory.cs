using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class PromptEditHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PromptTrackingId { get; set; }

        [ForeignKey("PromptTrackingId")]
        public virtual PromptTracking? PromptTracking { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChangeType { get; set; } = string.Empty; // Created, Updated, Deleted, StatusChanged

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
