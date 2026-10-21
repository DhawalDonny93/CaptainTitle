using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IFeatureFlagService
    {
        Task<List<FeatureFlag>> GetAllFlagsAsync();
        Task<FeatureFlag?> GetFlagByKeyAsync(string flagKey);
        Task<FeatureFlag> CreateFlagAsync(CreateFeatureFlagDto dto);
        Task<FeatureFlag?> UpdateFlagAsync(int id, UpdateFeatureFlagDto dto);
        Task<bool> DeleteFlagAsync(int id);
        Task<bool> IsFlagEnabledAsync(string flagKey, int? userId = null, string? role = null);
    }
}
