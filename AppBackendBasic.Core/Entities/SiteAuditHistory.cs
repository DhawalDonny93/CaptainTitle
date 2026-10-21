using System;

namespace AppBackendBasic.Core.Entities
{
    public class SiteAuditHistory
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public DateTime CheckDate { get; set; }
        public int? OverallScore { get; set; }
        public int? LlmsTxtScore { get; set; }
        public int? SchemaScore { get; set; }
        public int? DomClarityScore { get; set; }
        public int? ContentScore { get; set; }
        public int? TechnicalScore { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
        public int WarningIssues { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
    }
}
