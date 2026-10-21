using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class UserTask
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        // High, Medium, Low
        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        // Pending, InProgress, Completed, Cancelled
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public bool IsCompleted { get; set; } = false;

        public DateTime? DueDate { get; set; }

        // SystemGenerated, Manual
        [MaxLength(50)]
        public string Source { get; set; } = "Manual";

        /// <summary>
        /// URL of the page where this task was created from
        /// Helps users navigate back to the context where the task originated
        /// </summary>
        [MaxLength(500)]
        public string? SourceUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
