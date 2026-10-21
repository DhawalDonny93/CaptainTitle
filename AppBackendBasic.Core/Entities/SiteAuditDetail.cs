using System;
using System.Text.Json;

namespace AppBackendBasic.Core.Entities
{
    public class SiteAuditDetail
    {
        public int Id { get; set; }
        public int SiteAuditRunId { get; set; }
        public int TeamId { get; set; }
        public string CheckName { get; set; } = string.Empty;
        public string CheckType { get; set; } = string.Empty; // LlmsTxt, Schema, DomClarity, ContentStructure, TechnicalSEO, CrawledPage
        public string Status { get; set; } = "Info"; // Pass, Fail, Warning, Info
        public int Score { get; set; }
        public string? Message { get; set; }
        public string? DetailsJson { get; set; } // Store JSONB as string
        public string? Recommendation { get; set; }
        public bool AutoFixAvailable { get; set; }
        public int Priority { get; set; } // 1-5, higher is more important
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Crawled page properties (when CheckType = "CrawledPage")
        public string? PageUrl { get; set; }
        public int? Depth { get; set; }
        public int? StatusCode { get; set; }
        public string? Title { get; set; }
        public string? MetaDescription { get; set; }
        public int? WordCount { get; set; }
        public string? HeadingsJson { get; set; }
        public string? OutboundLinksJson { get; set; }
        public string? ImagesJson { get; set; }
        public bool? HasSchema { get; set; }
        public bool? HasLlmsTxt { get; set; }
        public string? ContentHash { get; set; }
        public string? MainContent { get; set; }

        // Navigation properties
        public virtual SiteAuditRun SiteAuditRun { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;

        // Helper property for working with Details as object
        public JsonDocument? Details
        {
            get => string.IsNullOrEmpty(DetailsJson) ? null : JsonDocument.Parse(DetailsJson);
            set => DetailsJson = value?.RootElement.ToString();
        }
    }
}
