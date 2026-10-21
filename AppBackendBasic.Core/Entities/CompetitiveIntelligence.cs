using System;

namespace AppBackendBasic.Core.Entities
{
    public class CompetitiveIntelligence
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string CompetitorDomain { get; set; } = string.Empty;
        public DateTime MetricDate { get; set; }
        public int VisibilityScore { get; set; }
        public decimal CitationShare { get; set; }
        public int? Rank { get; set; }
        public int PromptsTracked { get; set; }
        public decimal? AverageMentionPosition { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public virtual Project Project { get; set; } = null!;
    }
}
