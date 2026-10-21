using System;

namespace AppBackendBasic.Core.Entities
{
    public class VisibilityTrend
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public DateTime TrendDate { get; set; }
        public string? Platform { get; set; }
        public string? Topic { get; set; }
        public int TotalPromptsChecked { get; set; }
        public int PromptsWithMention { get; set; }
        public decimal AverageVisibilityScore { get; set; }
        public decimal? AveragePosition { get; set; }
        public int PositiveMentions { get; set; }
        public int NeutralMentions { get; set; }
        public int NegativeMentions { get; set; }
        public int CompetitorMentions { get; set; }
        public decimal? ShareOfVoice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
    }
}
