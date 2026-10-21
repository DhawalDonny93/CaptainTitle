using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class BrandProfile
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        public int TeamId { get; set; }

        [ForeignKey("TeamId")]
        public virtual Team? Team { get; set; }

        [MaxLength(120)]
        public string? Overview { get; set; }

        public string? Description { get; set; }

        public string? Topics { get; set; } // JSON array of topic strings

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
