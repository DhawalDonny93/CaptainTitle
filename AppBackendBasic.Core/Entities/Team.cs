using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(255)]
        public string? StripeCustomerId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<UserTeamMapping> UserMappings { get; set; } = new List<UserTeamMapping>();
    }
}
