using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class Competitor
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        public int TeamId { get; set; }

        [ForeignKey("TeamId")]
        public virtual Team? Team { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Domain { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Type of competitor: Direct (manually added) or SERP (found during prompt tracking)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string CompetitorType { get; set; } = "Direct"; // Direct, SERP

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
