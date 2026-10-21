using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class SiteAudit
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public int TeamId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [ForeignKey("TeamId")]
        public virtual Team Team { get; set; } = null!;

        public DateTime AuditDate { get; set; } = DateTime.UtcNow;

        public int OverallScore { get; set; }

        public int IssuesFound { get; set; }

        public string? ReportUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
