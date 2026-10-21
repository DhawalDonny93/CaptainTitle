using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IPromptResearchService
    {
        Task<List<PromptResearchData>> GeneratePromptResearchAsync(
            int projectId,
            string topic,
            string industry,
            string? audience = null,
            string? businessType = null,
            int count = 20);

        Task<List<PromptResearchData>> GetPromptResearchAsync(int projectId, string? topic = null);

        Task<PromptResearchData> AnalyzePromptRelevanceAsync(int projectId, string promptText);

        Task<Dictionary<string, List<PromptResearchData>>> CategorizePromptsAsync(int projectId);

        Task<bool> UpdatePromptStatusAsync(int researchId, string status);

        Task<bool> DeletePromptResearchAsync(int researchId);
    }
}
