using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class Keyword
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public int TeamId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [MaxLength(200)]
        public string Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Team Team { get; set; } = null!;
        public virtual ICollection<PromptTracking> PromptTrackings { get; set; } = new List<PromptTracking>();
    }
}
