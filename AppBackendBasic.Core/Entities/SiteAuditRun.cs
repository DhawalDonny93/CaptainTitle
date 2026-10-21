using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.Entities
{
    public class SiteAuditRun
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
        public int TotalChecks { get; set; }
        public int PassedChecks { get; set; }
        public int FailedChecks { get; set; }
        public int WarningChecks { get; set; }
        public int OverallScore { get; set; } // 0-100
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Crawling properties (for multi-page crawls)
        public bool IsCrawl { get; set; } = false; // True if this is a full site crawl
        public int TotalPages { get; set; } = 1; // Number of pages to crawl
        public int SuccessfulPages { get; set; } = 0;
        public int FailedPages { get; set; } = 0;
        public int BrokenLinks { get; set; } = 0;
        public int? MaxDepth { get; set; } // Max crawl depth
        public int? MaxPages { get; set; } // Max pages to crawl
        public int? DurationSeconds { get; set; }

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual ICollection<SiteAuditDetail> Details { get; set; } = new List<SiteAuditDetail>();
    }
}
