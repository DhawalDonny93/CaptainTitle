using System;

namespace AppBackendBasic.Core.Entities
{
    public class JobExecutionLog
    {
        public int Id { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Running, Completed, Failed
        public int? ProjectId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? DurationSeconds { get; set; }
        public int ItemsProcessed { get; set; } = 0;
        public int ItemsSucceeded { get; set; } = 0;
        public int ItemsFailed { get; set; } = 0;
        public string? ErrorMessage { get; set; }
        public string? DetailsJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Project? Project { get; set; }
    }
}
