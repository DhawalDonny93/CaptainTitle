using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        public int TeamId { get; set; }

        [ForeignKey("TeamId")]
        public virtual Team? Team { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Domain { get; set; }
        
        [MaxLength(255)]
        public string? WebsiteUrl { get; set; } // Alias for Domain for backward compatibility

        public string? BrandVoice { get; set; }

        public string? TargetAudience { get; set; }
        
        public string? Industry { get; set; }
        
        public string? CompetitorDomainsJson { get; set; } // JSON array of competitor domains

        // JSON string for CMS credentials
        public string? CmsCredentials { get; set; }

        public bool IsActive { get; set; } = true;
        
        // Feature flags for new functionality
        public bool? EnableAutoCrawl { get; set; }
        public int? CrawlMaxPages { get; set; }
        public int? CrawlMaxDepth { get; set; }
        public bool? EnableOpportunityDetection { get; set; }
        public bool? EnableVisibilityAlerts { get; set; }
        public string? VisibilityCheckFrequency { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<ContentArticle> ContentArticles { get; set; } = new List<ContentArticle>();
    }
}
