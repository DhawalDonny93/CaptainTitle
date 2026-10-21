using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities
{
    public class PromptTracking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        
        [Required]
        public string PromptText { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Topic { get; set; }
        
        public string Status { get; set; } = "Active"; // Active, Paused, Archived, Pending, Scheduled, Completed
        public bool IsCompetitor { get; set; } = false; // false = Own brand, true = Competitor
        public bool IsActive { get; set; } = true;
        public DateTime? LastCheckedAt { get; set; }
        public double? AverageVisibilityScore { get; set; }
        
        // Fields from AiTrackingPrompt for consolidation
        public string? CompetitorUrlsJson { get; set; } // JSON array of competitor URLs to track
        
        [MaxLength(20)]
        public string? TrackingFrequency { get; set; } // Daily, Weekly, Monthly
        
        public int? KeywordId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual Keyword? Keyword { get; set; }
        public virtual ICollection<PromptPlatformTracking> PlatformTrackings { get; set; } = new List<PromptPlatformTracking>();
        public virtual ICollection<AiTrackingResult> AiTrackingResults { get; set; } = new List<AiTrackingResult>();
    }
}
