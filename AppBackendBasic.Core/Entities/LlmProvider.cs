using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.Entities
{
    public class LlmProvider
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // OpenAI, Anthropic, Google
        public string DisplayName { get; set; } = string.Empty;
        public string? ApiEndpoint { get; set; }
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 0; // Higher priority used first
        public int MaxTokens { get; set; } = 4000;
        public decimal? CostPer1kInputTokens { get; set; } // USD
        public decimal? CostPer1kOutputTokens { get; set; } // USD
        public int RateLimitPerMinute { get; set; } = 60;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<LlmProviderConfig> Configs { get; set; } = new List<LlmProviderConfig>();
        public virtual ICollection<LlmRequest> Requests { get; set; } = new List<LlmRequest>();
    }
}
