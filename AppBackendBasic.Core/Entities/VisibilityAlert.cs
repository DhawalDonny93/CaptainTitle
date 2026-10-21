using System;

namespace AppBackendBasic.Core.Entities
{
    public class VisibilityAlert
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public string AlertType { get; set; } = string.Empty; // VisibilityDrop, NewCompetitor, NegativeMention, NotMentioned
        public string Severity { get; set; } = string.Empty; // Critical, Warning, Info
        public string Message { get; set; } = string.Empty;
        public string? PromptText { get; set; }
        public string? Platform { get; set; }
        public string? DetailsJson { get; set; } // Store CurrentValue, PreviousValue, etc. as JSON
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public virtual Project Project { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
    }
}
