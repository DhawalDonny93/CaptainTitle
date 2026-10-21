using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IOpportunityDetectionService
    {
        // Content Gap Detection
        Task<List<ContentGapOpportunity>> DetectContentGapsAsync(int projectId, List<string>? competitorDomains = null);
        Task<List<ContentGapOpportunity>> GetContentGapOpportunitiesAsync(int projectId, string? status = null);
        Task<bool> UpdateContentGapStatusAsync(int opportunityId, string status);

        // SEO Opportunity Detection
        Task<List<SeoOpportunity>> DetectSeoOpportunitiesAsync(int projectId, int? siteAuditRunId = null);
        Task<List<SeoOpportunity>> GetSeoOpportunitiesAsync(int projectId, string? priority = null);
        Task<bool> UpdateSeoOpportunityStatusAsync(int opportunityId, string status);
        Task<bool> MarkSeoOpportunityFixedAsync(int opportunityId);

        // Visibility Opportunity Detection
        Task<List<VisibilityOpportunity>> DetectVisibilityOpportunitiesAsync(int projectId);
        Task<List<VisibilityOpportunity>> GetVisibilityOpportunitiesAsync(int projectId, string? type = null);
        Task<bool> UpdateVisibilityOpportunityStatusAsync(int opportunityId, string status);

        // Opportunity Summary
        Task<OpportunitySummaryDto> GetOpportunitySummaryAsync(int projectId);
    }

    public class OpportunitySummaryDto
    {
        public int TotalOpportunities { get; set; }
        public int ContentGaps { get; set; }
        public int SeoIssues { get; set; }
        public int VisibilityGaps { get; set; }
        public int HighPriority { get; set; }
        public int InProgress { get; set; }
        public int Fixed { get; set; }
    }
}
