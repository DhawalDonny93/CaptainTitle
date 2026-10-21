using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ILlmJobConfigService
    {
        /// <summary>
        /// Returns the resolved (provider, model, maxTokens, temperature) for a given job key.
        /// Hits in-memory cache first; falls back to DB; falls back to supplied defaults.
        /// </summary>
        Task<LlmJobResolution> GetJobConfigAsync(string jobKey, string defaultProvider, string defaultModel, int defaultMaxTokens = 2000, decimal defaultTemperature = 0.7m);

        /// <summary>Get all job configs (for Admin UI listing).</summary>
        Task<List<LlmJobConfig>> GetAllAsync();

        /// <summary>Get a single config by job key.</summary>
        Task<LlmJobConfig?> GetByKeyAsync(string jobKey);

        /// <summary>Create or update a job config and invalidate cache.</summary>
        Task<LlmJobConfig> UpsertAsync(LlmJobConfig config);

        /// <summary>Delete a job config and invalidate cache.</summary>
        Task<bool> DeleteAsync(string jobKey);

        /// <summary>Invalidate the entire cache (called after bulk updates).</summary>
        void InvalidateCache();
    }

    /// <summary>Resolved LLM settings for a specific job.</summary>
    public record LlmJobResolution(
        string Provider,
        string Model,
        int MaxTokens,
        decimal Temperature
    );
}
