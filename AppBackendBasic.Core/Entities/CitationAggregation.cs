using System;

namespace AppBackendBasic.Core.Entities
{
    public class CitationAggregation
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string SourceDomain { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public DateTime AggregationDate { get; set; }
        public int TotalCitations { get; set; }
        public int UniqueCitations { get; set; }
        public double AveragePosition { get; set; }
        public int TopPosition { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
    }
}
