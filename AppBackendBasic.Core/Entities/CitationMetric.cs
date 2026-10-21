using System;

namespace AppBackendBasic.Core.Entities
{
    public class CitationMetric
    {
        public int Id { get; set; }
        public int CitationId { get; set; }
        public int TeamId { get; set; }
        public DateTime CheckDate { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; } = true;
        public string? ResponseContext { get; set; }
        public bool IsCompetitor { get; set; } = false; // false = Own brand, true = Competitor
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Citation Citation { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
    }
}
