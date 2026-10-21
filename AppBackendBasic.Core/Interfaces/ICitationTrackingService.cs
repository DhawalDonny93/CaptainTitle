using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ICitationTrackingService
    {
        Task<List<Citation>> GetCitationsAsync(int projectId, string? platform = null, DateTime? startDate = null, DateTime? endDate = null);
        
        Task<Citation?> GetCitationByIdAsync(int citationId);
        
        Task<Dictionary<string, object>> GetCitationStatsAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null);
        
        Task<List<BrandMention>> GetBrandMentionsAsync(int projectId, string? platform = null, bool? isMentioned = null, DateTime? startDate = null, DateTime? endDate = null);
        
        Task<Dictionary<string, object>> GetBrandMentionStatsAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null);
        
        Task TrackCitationsForPromptAsync(int projectId, string promptText, string platform, string responseText);
        
        Task TrackBrandMentionAsync(int projectId, string promptText, string platform, string responseText, string brandName, string? brandDomain = null);
        
        Task AggregateDailyCitationsAsync(int projectId, DateTime date);
        
        Task<List<object>> GetTopCitedDomainsAsync(int projectId, int limit = 10, DateTime? startDate = null, DateTime? endDate = null);
        
        Task<SourcesAnalyticsDto> GetSourcesAnalyticsAsync(int projectId, string? platform = null, string? topic = null, DateTime? startDate = null, DateTime? endDate = null);
        
        Task<MentionsAnalyticsDto> GetMentionsAnalyticsAsync(int projectId, string? platform = null, string? topic = null, DateTime? startDate = null, DateTime? endDate = null);
        
        Task<CitationDetailsDto> GetCitationDetailsByDomainAsync(int projectId, string domain, DateTime? startDate = null, DateTime? endDate = null);
    }
}
