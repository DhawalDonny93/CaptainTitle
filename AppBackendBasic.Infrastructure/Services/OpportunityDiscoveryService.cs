using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class OpportunityDiscoveryService : IOpportunityDiscoveryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILlmProviderService _llmService;
        private readonly ILogger<OpportunityDiscoveryService> _logger;

        public OpportunityDiscoveryService(
            ApplicationDbContext context,
            ILlmProviderService llmService,
            ILogger<OpportunityDiscoveryService> logger)
        {
            _context = context;
            _llmService = llmService;
            _logger = logger;
        }

        public async Task<OpportunityScan> StartScanAsync(int projectId, string scanType, string? targetUrl)
        {
            var scan = new OpportunityScan
            {
                ProjectId = projectId,
                ScanType = scanType,
                TargetUrl = targetUrl,
                Status = "Running",
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.OpportunityScans.Add(scan);
            await _context.SaveChangesAsync();

            try
            {
                switch (scanType.ToLower())
                {
                    case "community":
                        await ScanCommunityPlatformsAsync(scan);
                        break;
                    case "contentgap":
                        await ScanContentGapsAsync(scan);
                        break;
                    case "competitor":
                        if (!string.IsNullOrEmpty(targetUrl))
                        {
                            await ScanCompetitorAsync(scan, targetUrl);
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unknown scan type: {scanType}");
                }

                scan.Status = "Completed";
                scan.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running opportunity scan {ScanId}", scan.Id);
                scan.Status = "Failed";
                scan.ErrorMessage = ex.Message;
                scan.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return scan;
        }

        public async Task<OpportunityScan?> GetScanAsync(int scanId)
        {
            return await _context.OpportunityScans
                .Include(s => s.ContentGapOpportunities)
                .FirstOrDefaultAsync(s => s.Id == scanId);
        }

        public async Task<List<OpportunityScan>> GetProjectScansAsync(int projectId, int limit = 10)
        {
            return await _context.OpportunityScans
                .Where(s => s.ProjectId == projectId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<CommunityOpportunity>> GetCommunityOpportunitiesAsync(int projectId, OpportunityFilters? filters = null)
        {
            var query = _context.CommunityOpportunities
                .Where(o => o.ProjectId == projectId);

            if (filters != null)
            {
                if (!string.IsNullOrEmpty(filters.Platform))
                    query = query.Where(o => o.Platform == filters.Platform);

                if (!string.IsNullOrEmpty(filters.Status))
                    query = query.Where(o => o.Status == filters.Status);

                if (filters.MinPriority.HasValue)
                    query = query.Where(o => o.Priority >= filters.MinPriority.Value);

                if (filters.MinEngagementScore.HasValue)
                    query = query.Where(o => o.EngagementScore >= filters.MinEngagementScore.Value);

                if (filters.IsArchived.HasValue)
                    query = query.Where(o => o.IsArchived == filters.IsArchived.Value);
            }

            return await query
                .OrderByDescending(o => o.Priority)
                .ThenByDescending(o => o.EngagementScore)
                .ToListAsync();
        }

        public async Task<List<CommunityOpportunity>> ScanRedditAsync(int projectId, List<string> keywords)
        {
            // Mock implementation - in production, integrate with Reddit API
            var opportunities = new List<CommunityOpportunity>();
            
            foreach (var keyword in keywords.Take(3))
            {
                var opportunity = new CommunityOpportunity
                {
                    ProjectId = projectId,
                    Platform = "Reddit",
                    Title = $"Discussion about {keyword}",
                    Url = $"https://reddit.com/r/example/{keyword.ToLower()}",
                    Summary = $"Users discussing {keyword} with questions about best practices",
                    Sentiment = "Question",
                    Status = "New",
                    Priority = 3,
                    EngagementScore = 65,
                    Upvotes = 45,
                    Comments = 23,
                    Views = 1200,
                    PostedAt = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 48)),
                    LastCheckedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                opportunities.Add(opportunity);
            }

            if (opportunities.Any())
            {
                _context.CommunityOpportunities.AddRange(opportunities);
                await _context.SaveChangesAsync();
            }

            return opportunities;
        }

        public async Task<List<CommunityOpportunity>> ScanQuoraAsync(int projectId, List<string> keywords)
        {
            // Mock implementation - in production, integrate with Quora
            var opportunities = new List<CommunityOpportunity>();
            
            foreach (var keyword in keywords.Take(2))
            {
                var opportunity = new CommunityOpportunity
                {
                    ProjectId = projectId,
                    Platform = "Quora",
                    Title = $"What is the best approach for {keyword}?",
                    Url = $"https://quora.com/question/{keyword.ToLower()}",
                    Summary = $"Question about {keyword} with limited answers",
                    Sentiment = "Question",
                    Status = "New",
                    Priority = 4,
                    EngagementScore = 75,
                    Upvotes = 32,
                    Comments = 8,
                    Views = 850,
                    PostedAt = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 72)),
                    LastCheckedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                opportunities.Add(opportunity);
            }

            if (opportunities.Any())
            {
                _context.CommunityOpportunities.AddRange(opportunities);
                await _context.SaveChangesAsync();
            }

            return opportunities;
        }

        public async Task<string> GenerateDraftReplyAsync(int opportunityId, int teamId)
        {
            var opportunity = await _context.CommunityOpportunities.FindAsync(opportunityId);
            if (opportunity == null)
                throw new Exception("Opportunity not found");

            var prompt = $@"Generate a helpful, natural reply for this {opportunity.Platform} post:

Title: {opportunity.Title}
Summary: {opportunity.Summary}
Sentiment: {opportunity.Sentiment}

Requirements:
- Be helpful and informative
- Answer the question directly
- Keep it conversational and natural
- Avoid being overly promotional
- Include a subtle mention of relevant solutions if appropriate
- Match the tone of the platform";

            // TODO: Fix LLM service call - need to check ContentGenerationRequest DTO properties and implement draft generation
            // Action: Verify ContentGenerationRequest DTO structure, build request with conversation context, call _llmService.GenerateContentAsync()
            // Currently using placeholder text - should generate AI-powered draft reply based on community post and conversation history
            // var request = new ContentGenerationRequest { ... };
            // var response = await _llmService.GenerateContentAsync(request);
            
            var draftReply = "Draft reply placeholder - LLM integration pending";
            opportunity.DraftReply = draftReply;
            opportunity.Status = "Drafted";
            await _context.SaveChangesAsync();

            return draftReply;
        }

        public async Task<bool> UpdateOpportunityStatusAsync(int opportunityId, string status, int teamId)
        {
            var opportunity = await _context.CommunityOpportunities.FindAsync(opportunityId);
            if (opportunity == null)
                return false;

            opportunity.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveOpportunityAsync(int opportunityId, int teamId)
        {
            var opportunity = await _context.CommunityOpportunities.FindAsync(opportunityId);
            if (opportunity == null)
                return false;

            opportunity.IsArchived = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ContentGapOpportunity>> GetContentGapsAsync(int projectId)
        {
            return await _context.ContentGapOpportunities
                .Where(g => g.ProjectId == projectId)
                .OrderByDescending(g => g.EstimatedImpact)
                .ThenByDescending(g => g.SearchVolume)
                .ToListAsync();
        }

        public async Task<List<ContentGapOpportunity>> AnalyzeContentGapsAsync(int projectId)
        {
            // Mock implementation - in production, analyze actual content
            var gaps = new List<ContentGapOpportunity>
            {
                new ContentGapOpportunity
                {
                    ProjectId = projectId,
                    Topic = "AI SEO Best Practices 2026",
                    Description = "Comprehensive guide on AI-driven SEO strategies",
                    RecommendedContentType = "Guide",
                    EstimatedImpact = "High",
                    CompetitorCoverage = 3,
                    SearchVolume = 2400,
                    Difficulty = 45,
                    Status = "New",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ContentGapOpportunity
                {
                    ProjectId = projectId,
                    Topic = "LLMs.txt Implementation Tutorial",
                    Description = "Step-by-step tutorial for implementing LLMs.txt",
                    RecommendedContentType = "Tutorial",
                    EstimatedImpact = "High",
                    CompetitorCoverage = 1,
                    SearchVolume = 1800,
                    Difficulty = 30,
                    Status = "New",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ContentGapOpportunity
                {
                    ProjectId = projectId,
                    Topic = "AI Visibility vs Traditional SEO Comparison",
                    Description = "Comparison article between AI visibility and traditional SEO",
                    RecommendedContentType = "Comparison",
                    EstimatedImpact = "Medium",
                    CompetitorCoverage = 2,
                    SearchVolume = 1200,
                    Difficulty = 40,
                    Status = "New",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.ContentGapOpportunities.AddRange(gaps);
            await _context.SaveChangesAsync();

            return gaps;
        }

        public async Task<List<ContentGapOpportunity>> CompareWithCompetitorsAsync(int projectId, List<string> competitorUrls)
        {
            // Mock implementation
            var gaps = await AnalyzeContentGapsAsync(projectId);
            return gaps;
        }

        public async Task<CompetitorAnalysis> AnalyzeCompetitorAsync(int projectId, string competitorUrl)
        {
            var domain = new Uri(competitorUrl).Host;

            var existing = await _context.CompetitorAnalyses
                .FirstOrDefaultAsync(c => c.ProjectId == projectId && c.CompetitorDomain == domain);

            if (existing != null)
            {
                existing.LastAnalyzedAt = DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existing;
            }

            // Mock analysis - in production, perform actual analysis
            var analysis = new CompetitorAnalysis
            {
                ProjectId = projectId,
                CompetitorDomain = domain,
                CompetitorName = domain.Replace("www.", "").Split('.')[0],
                VisibilityScore = Random.Shared.Next(60, 95),
                CitationShare = (decimal)(Random.Shared.NextDouble() * 15 + 5),
                ContentCount = Random.Shared.Next(50, 200),
                TopTopics = JsonSerializer.Serialize(new[] { "AI SEO", "Content Optimization", "Schema Markup" }),
                Strengths = JsonSerializer.Serialize(new[] { "Strong technical SEO", "Regular content updates", "Good schema implementation" }),
                Weaknesses = JsonSerializer.Serialize(new[] { "Limited AI optimization", "No LLMs.txt file", "Weak internal linking" }),
                LastAnalyzedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CompetitorAnalyses.Add(analysis);
            await _context.SaveChangesAsync();

            return analysis;
        }

        public async Task<List<CompetitorAnalysis>> GetCompetitorAnalysesAsync(int projectId)
        {
            return await _context.CompetitorAnalyses
                .Where(c => c.ProjectId == projectId)
                .OrderByDescending(c => c.VisibilityScore)
                .ToListAsync();
        }

        public async Task<OpportunityDashboard> GetDashboardAsync(int projectId)
        {
            var communityOpps = await _context.CommunityOpportunities
                .Where(o => o.ProjectId == projectId && !o.IsArchived)
                .ToListAsync();

            var contentGaps = await _context.ContentGapOpportunities
                .Where(g => g.ProjectId == projectId && g.Status != "Completed")
                .ToListAsync();

            var competitors = await _context.CompetitorAnalyses
                .Where(c => c.ProjectId == projectId)
                .ToListAsync();

            var dashboard = new OpportunityDashboard
            {
                TotalOpportunities = communityOpps.Count + contentGaps.Count,
                HighPriority = communityOpps.Count(o => o.Priority >= 4) + contentGaps.Count(g => g.EstimatedImpact == "High"),
                MediumPriority = communityOpps.Count(o => o.Priority == 3) + contentGaps.Count(g => g.EstimatedImpact == "Medium"),
                LowPriority = communityOpps.Count(o => o.Priority <= 2) + contentGaps.Count(g => g.EstimatedImpact == "Low"),
                CommunityOpportunities = communityOpps.Count,
                ContentGaps = contentGaps.Count,
                CompetitorInsights = competitors.Count,
                RecentOpportunities = communityOpps
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new OpportunitySummary
                    {
                        Id = o.Id,
                        Type = "Community",
                        Title = o.Title,
                        Priority = o.Priority,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt
                    })
                    .ToList(),
                PlatformBreakdown = communityOpps
                    .GroupBy(o => o.Platform)
                    .Select(g => new PlatformStats
                    {
                        Platform = g.Key,
                        Count = g.Count(),
                        AvgEngagementScore = (int)g.Average(o => o.EngagementScore)
                    })
                    .ToList()
            };

            return dashboard;
        }

        public async Task<List<OpportunityTrend>> GetTrendsAsync(int projectId, DateRangeDto dateRange)
        {
            var communityOpps = await _context.CommunityOpportunities
                .Where(o => o.ProjectId == projectId &&
                           o.CreatedAt >= dateRange.StartDate &&
                           o.CreatedAt <= dateRange.EndDate)
                .ToListAsync();

            var contentGaps = await _context.ContentGapOpportunities
                .Where(g => g.ProjectId == projectId &&
                           g.CreatedAt >= dateRange.StartDate &&
                           g.CreatedAt <= dateRange.EndDate)
                .ToListAsync();

            var trends = communityOpps
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new OpportunityTrend
                {
                    Date = g.Key,
                    CommunityOpportunities = g.Count(),
                    ContentGaps = contentGaps.Count(c => c.CreatedAt.Date == g.Key),
                    TotalOpportunities = g.Count() + contentGaps.Count(c => c.CreatedAt.Date == g.Key),
                    AvgEngagementScore = (int)g.Average(o => o.EngagementScore)
                })
                .OrderBy(t => t.Date)
                .ToList();

            return trends;
        }

        private async Task ScanCommunityPlatformsAsync(OpportunityScan scan)
        {
            var project = await _context.Projects.FindAsync(scan.ProjectId);
            if (project == null) return;

            var keywords = new List<string> { "AI SEO", "content optimization", "search visibility" };

            var redditOpps = await ScanRedditAsync(scan.ProjectId, keywords);
            var quoraOpps = await ScanQuoraAsync(scan.ProjectId, keywords);

            scan.TotalOpportunities = redditOpps.Count + quoraOpps.Count;
            scan.HighPriority = redditOpps.Count(o => o.Priority >= 4) + quoraOpps.Count(o => o.Priority >= 4);
            scan.MediumPriority = redditOpps.Count(o => o.Priority == 3) + quoraOpps.Count(o => o.Priority == 3);
            scan.LowPriority = redditOpps.Count(o => o.Priority <= 2) + quoraOpps.Count(o => o.Priority <= 2);
        }

        private async Task ScanContentGapsAsync(OpportunityScan scan)
        {
            var gaps = await AnalyzeContentGapsAsync(scan.ProjectId);

            foreach (var gap in gaps)
            {
                gap.OpportunityScanId = scan.Id;
            }

            await _context.SaveChangesAsync();

            scan.TotalOpportunities = gaps.Count;
            scan.HighPriority = gaps.Count(g => g.EstimatedImpact == "High");
            scan.MediumPriority = gaps.Count(g => g.EstimatedImpact == "Medium");
            scan.LowPriority = gaps.Count(g => g.EstimatedImpact == "Low");
        }

        private async Task ScanCompetitorAsync(OpportunityScan scan, string competitorUrl)
        {
            await AnalyzeCompetitorAsync(scan.ProjectId, competitorUrl);
            scan.TotalOpportunities = 1;
        }
    }
}
