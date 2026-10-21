using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    /// <summary>
    /// Service for managing prompt templates with DB storage and caching
    /// </summary>
    public interface IPromptTemplateService
    {
        /// <summary>
        /// Get a template by name with caching. Falls back to hardcoded templates if not in DB.
        /// </summary>
        Task<string> GetTemplateAsync(string templateName);

        /// <summary>
        /// Get a template and replace variables
        /// </summary>
        Task<string> GetTemplateWithVariablesAsync(string templateName, Dictionary<string, string> variables);

        /// <summary>
        /// Get all templates for a category
        /// </summary>
        Task<List<PromptTemplate>> GetTemplatesByCategoryAsync(string category);

        /// <summary>
        /// Get all active templates
        /// </summary>
        Task<List<PromptTemplate>> GetAllActiveTemplatesAsync();

        /// <summary>
        /// Create or update a template
        /// </summary>
        Task<PromptTemplate> UpsertTemplateAsync(PromptTemplate template);

        /// <summary>
        /// Delete a template
        /// </summary>
        Task<bool> DeleteTemplateAsync(int id);

        /// <summary>
        /// Increment usage count for a template
        /// </summary>
        Task IncrementUsageAsync(string templateName);

        /// <summary>
        /// Invalidate cache for a specific template or all templates
        /// </summary>
        void InvalidateCache(string? templateName = null);

        /// <summary>
        /// Seed database with hardcoded templates if they don't exist
        /// </summary>
        Task SeedTemplatesAsync();
    }
}
