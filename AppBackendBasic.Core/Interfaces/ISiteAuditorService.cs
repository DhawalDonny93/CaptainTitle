using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface ISiteAuditorService
    {
        // Audit Execution
        Task<SiteAuditRun> StartAuditAsync(int projectId, string url, int teamId);
        Task<SiteAuditRun?> GetAuditRunAsync(int runId);
        Task<List<SiteAuditRun>> GetProjectAuditRunsAsync(int projectId, int limit = 10);
        Task<SiteAuditRun?> GetLatestAuditAsync(int projectId);

        // Multi-Page Crawling
        Task<SiteAuditRun> StartCrawlAsync(int projectId, string baseUrl, CrawlOptions options, int teamId);
        Task<List<SiteAuditDetail>> GetCrawledPagesAsync(int crawlRunId);
        Task<CrawlSummary> GetCrawlSummaryAsync(int crawlRunId);
        
        // Audit Checks
        Task<AuditCheckResult> CheckLlmsTxtAsync(string url);
        Task<AuditCheckResult> CheckSchemaMarkupAsync(string url);
        Task<AuditCheckResult> CheckDomClarityAsync(string url);
        Task<AuditCheckResult> CheckContentStructureAsync(string url);
        Task<AuditCheckResult> CheckTechnicalSEOAsync(string url);
        
        // Analysis & Reporting
        Task<AuditSummary> GetAuditSummaryAsync(int runId);
        Task<List<AuditTrend>> GetAuditTrendsAsync(int projectId, DateRangeDto dateRange);
        Task<byte[]> ExportAuditReportAsync(int runId, string format);
        
        // Issue Management
        Task<bool> RecheckIssueAsync(int detailId);
        Task<bool> MarkIssueFixedAsync(int detailId);
    }

    public class CrawlOptions
    {
        public int MaxPages { get; set; } = 100;
        public int MaxDepth { get; set; } = 3;
        public bool FollowExternalLinks { get; set; } = false;
        public bool RespectRobotsTxt { get; set; } = true;
        public int DelayMs { get; set; } = 1000;
        public List<string>? ExcludePatterns { get; set; }
    }

    public class CrawlSummary
    {
        public int TotalPages { get; set; }
        public int SuccessfulPages { get; set; }
        public int FailedPages { get; set; }
        public int BrokenLinks { get; set; }
        public int PagesWithSchema { get; set; }
        public int PagesWithLlmsTxt { get; set; }
        public int DuplicatePages { get; set; }
        public Dictionary<int, int> StatusCodeDistribution { get; set; } = new();
        public List<string> MostCommonIssues { get; set; } = new();
    }
}
