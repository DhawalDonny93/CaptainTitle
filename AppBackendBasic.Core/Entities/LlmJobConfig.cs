using System;

namespace AppBackendBasic.Core.Entities
{
    /// <summary>
    /// Maps each background job / LLM process to a specific provider and model.
    /// Admins can change these dynamically from the Admin UI without redeploying.
    /// </summary>
    public class LlmJobConfig
    {
        public int Id { get; set; }

        /// <summary>
        /// Unique machine-readable key identifying the job/process.
        /// e.g. "AiTracking", "PromptResearch", "SentimentAnalysis"
        /// </summary>
        public string JobKey { get; set; } = string.Empty;

        /// <summary>Human-readable name shown in Admin UI.</summary>
        public string JobName { get; set; } = string.Empty;

        /// <summary>Short description of what this job does.</summary>
        public string? Description { get; set; }

        /// <summary>
        /// Provider name matching LlmProviderFactory keys: "OpenAI", "Anthropic", "Google"
        /// </summary>
        public string Provider { get; set; } = "OpenAI";

        /// <summary>
        /// Model identifier passed to the provider.
        /// e.g. "gpt-4o", "claude-3-5-sonnet-20241022", "gemini-1.5-pro"
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>Max tokens for this job's requests.</summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>Temperature for this job's requests (0.0 – 2.0).</summary>
        public decimal Temperature { get; set; } = 0.7m;

        /// <summary>Whether this job config is active. If false, the job falls back to appsettings defaults.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Grouping category shown in Admin UI. e.g. "Tracking", "Research", "Analysis", "Content"</summary>
        public string Category { get; set; } = "General";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
