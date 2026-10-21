using System;

namespace AppBackendBasic.Core.Entities
{
    public class OpportunityImpact
    {
        public int Id { get; set; }
        public string OpportunityType { get; set; } = string.Empty; // Community, ContentGap, Competitor
        public int OpportunityId { get; set; }
        public string ImpactType { get; set; } = string.Empty; // VisibilityIncrease, CitationGain, TrafficGain
        public decimal? EstimatedValue { get; set; }
        public decimal? ActualValue { get; set; }
        public DateTime? MeasuredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
