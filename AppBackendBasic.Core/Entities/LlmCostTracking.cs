using System;

namespace AppBackendBasic.Core.Entities
{
    public class LlmCostTracking
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ProjectId { get; set; }
        public int ProviderId { get; set; }
        public DateTime TrackingDate { get; set; }
        public int TotalRequests { get; set; } = 0;
        public int SuccessfulRequests { get; set; } = 0;
        public int FailedRequests { get; set; } = 0;
        public long TotalInputTokens { get; set; } = 0;
        public long TotalOutputTokens { get; set; } = 0;
        public decimal TotalCost { get; set; } = 0; // USD
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Project? Project { get; set; }
        public virtual LlmProvider Provider { get; set; } = null!;
    }
}
