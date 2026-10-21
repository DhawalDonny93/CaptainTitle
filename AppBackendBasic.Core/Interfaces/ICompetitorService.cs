using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ICompetitorService
    {
        Task<List<Competitor>> GetByProjectIdAsync(int projectId);
        Task<Competitor> AddCompetitorAsync(int projectId, string domain);
        Task<Competitor> FindOrCreateSerpCompetitorAsync(int projectId, string domain, string? name = null);
        Task DeleteCompetitorAsync(int competitorId);
        Task<string?> FetchLogoFromWebsiteAsync(string domain);
    }
}
