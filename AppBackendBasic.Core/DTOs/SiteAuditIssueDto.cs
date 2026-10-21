using System;

namespace AppBackendBasic.Core.DTOs
{
    public class SiteAuditIssueDto
    {
        public int Id { get; set; }
        public int SiteAuditRunId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string Category { get; set; } = "Technical";
        public string Type { get; set; } = string.Empty;
        public int AffectedPages { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Recommendation { get; set; }
        public bool AutoFixAvailable { get; set; }
        public int Priority { get; set; }
        public int EstimatedImpactScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateSiteAuditIssueDto
    {
        public int SiteAuditRunId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string Category { get; set; } = "Technical";
        public string Type { get; set; } = string.Empty;
        public int AffectedPages { get; set; }
        public string? Recommendation { get; set; }
        public bool AutoFixAvailable { get; set; }
        public int Priority { get; set; } = 3;
        public int EstimatedImpactScore { get; set; }
    }

    public class UpdateSiteAuditIssueDto
    {
        public bool? IsCompleted { get; set; }
        public string? Recommendation { get; set; }
    }

    public class AddOpportunityToTaskDto
    {
        public int OpportunityId { get; set; }
        public string OpportunitySource { get; set; } = string.Empty; // Visibility, ContentGap, SEO
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public DateTime? DueDate { get; set; }
    }
}
