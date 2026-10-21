using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class UserTeamMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        public int TeamId { get; set; }

        [ForeignKey("TeamId")]
        public virtual Team? Team { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "Member"; // Admin, Manager, Member

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public int? InvitedBy { get; set; } // FK to User who invited this user

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
