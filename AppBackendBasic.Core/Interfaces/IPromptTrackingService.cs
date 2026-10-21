using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IPromptTrackingService
    {
        Task<PromptTracking> CreatePromptAsync(int projectId, int teamId, int userId, string promptText, string? topic, List<string> platforms, bool? isCompetitor = null);

        Task<PromptTracking> UpdatePromptAsync(int promptId, int teamId, string promptText, string? topic, string status);

        Task<bool> DeletePromptAsync(int promptId, int teamId);
        
        Task<PromptTracking?> GetPromptByIdAsync(int promptId);
        
        Task<List<PromptTracking>> GetPromptsAsync(int projectId, string? status = null, string? topic = null, bool? isActive = null, bool? isCompetitor = null);
        
        Task<List<PromptTracking>> SearchPromptsAsync(int projectId, string searchTerm);
        
        Task<bool> BulkUpdateStatusAsync(List<int> promptIds, string status);
        
        Task<bool> BulkDeleteAsync(List<int> promptIds);
        
        Task CheckPromptVisibilityAsync(int userId, int promptId);
        
        Task ProcessAllActivePromptsAsync();
        
        
        Task<List<PromptResearchData>> GetPromptResearchDataAsync(string? topic = null, int limit = 100);
        
        Task<PromptResearchData> AddResearchDataAsync(string promptText, string? topic, int? estimatedVolume, string difficulty);
    }
}
