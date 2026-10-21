using System;

namespace AppBackendBasic.Core.Entities
{
    public class PromptResearchData
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string PromptText { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public string? Category { get; set; }
        public int? RelevanceScore { get; set; } // Changed from double to int to match service
        public string? EstimatedVolume { get; set; } // Changed from int to string to match service
        public string Difficulty { get; set; } = "Medium"; // Easy, Medium, Hard
        public string Status { get; set; } = "New"; // New, Reviewed, Approved, Rejected
        public string Source { get; set; } = "Manual"; // Manual, AI, Import
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
    }
}
