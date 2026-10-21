using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IOpportunityDiscoveryService
    {
        // Scan Execution
        Task<OpportunityScan> StartScanAsync(int projectId, string scanType, string? targetUrl);
        Task<OpportunityScan?> GetScanAsync(int scanId);
        Task<List<OpportunityScan>> GetProjectScansAsync(int projectId, int limit = 10);
        
        // Community Opportunities
        Task<List<CommunityOpportunity>> GetCommunityOpportunitiesAsync(int projectId, OpportunityFilters? filters = null);
        Task<List<CommunityOpportunity>> ScanRedditAsync(int projectId, List<string> keywords);
        Task<List<CommunityOpportunity>> ScanQuoraAsync(int projectId, List<string> keywords);
        Task<string> GenerateDraftReplyAsync(int opportunityId, int teamId);
        Task<bool> UpdateOpportunityStatusAsync(int opportunityId, string status, int teamId);
        Task<bool> ArchiveOpportunityAsync(int opportunityId, int teamId);
        
        // Content Gap Analysis
        Task<List<ContentGapOpportunity>> GetContentGapsAsync(int projectId);
        Task<List<ContentGapOpportunity>> AnalyzeContentGapsAsync(int projectId);
        Task<List<ContentGapOpportunity>> CompareWithCompetitorsAsync(int projectId, List<string> competitorUrls);
        
        // Competitor Analysis
        Task<CompetitorAnalysis> AnalyzeCompetitorAsync(int projectId, string competitorUrl);
        Task<List<CompetitorAnalysis>> GetCompetitorAnalysesAsync(int projectId);
        
        // Dashboard & Reporting
        Task<OpportunityDashboard> GetDashboardAsync(int projectId);
        Task<List<OpportunityTrend>> GetTrendsAsync(int projectId, DateRangeDto dateRange);
    }
}
