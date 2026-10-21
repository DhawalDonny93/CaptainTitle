using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IBrandProfileService
    {
        Task<BrandProfile?> GetByProjectIdAsync(int projectId);
        Task<BrandProfile> CreateOrUpdateAsync(int projectId, string? overview, string? description, string? topics);
        Task<string?> UploadLogoAsync(int projectId, Stream fileStream, string fileName);
        Task DeleteAsync(int projectId);
    }
}
