using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class OpportunityDetectionService : IOpportunityDetectionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILlmProviderService _llmService;
        private readonly IPromptTemplateService _promptTemplateService;
        private readonly ILogger<OpportunityDetectionService> _logger;

        public OpportunityDetectionService(
            ApplicationDbContext context,
            ILlmProviderService llmService,
            IPromptTemplateService promptTemplateService,
            ILogger<OpportunityDetectionService> logger)
        {
            _context = context;
            _llmService = llmService;
            _promptTemplateService = promptTemplateService;
            _logger = logger;
        }

        public async Task<List<ContentGapOpportunity>> DetectContentGapsAsync(
            int projectId, 
            List<string>? competitorDomains = null)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    throw new Exception($"Project {projectId} not found");
                }

                // Get competitor domains from project settings if not provided
                if (competitorDomains == null || !competitorDomains.Any())
                {
                    if (!string.IsNullOrEmpty(project.CompetitorDomainsJson))
                    {
                        competitorDomains = JsonSerializer.Deserialize<List<string>>(project.CompetitorDomainsJson);
                    }
                }

                if (competitorDomains == null || !competitorDomains.Any())
                {
                    _logger.LogWarning("No competitor domains provided for content gap analysis");
                    return new List<ContentGapOpportunity>();
                }

                // Get existing prompts for this project
                var existingPrompts = await _context.PromptTrackings
                    .Where(p => p.ProjectId == projectId)
                    .Select(p => p.PromptText)
                    .ToListAsync();

                // Prepare template variables
                var variables = new Dictionary<string, string>
                {
                    { "BRAND_NAME", project.Name },
                    { "DOMAIN", project.WebsiteUrl ?? "your website" },
                    { "COMPETITOR_DOMAINS", string.Join(", ", competitorDomains) },
                    { "EXISTING_PROMPTS", existingPrompts.Any() ? string.Join("\n", existingPrompts.Take(10)) : "None" },
                    { "INDUSTRY", project.Industry ?? "general" }
                };

                // Get template and replace variables
                var prompt = await _promptTemplateService.GetTemplateWithVariablesAsync("ContentGapAnalysis", variables);

                // Call LLM
                var llmRequest = new LlmRequestDto
                {
                    Provider = "OpenAI",
                    Model = "gpt-4-turbo-preview",
                    PromptText = prompt,
                    MaxTokens = 2000,
                    Temperature = 0.7M
                };

                var llmResponse = await _llmService.SendRequestAsync(llmRequest);

                if (!llmResponse.Success)
                {
                    _logger.LogError("LLM request failed: {Error}", llmResponse.ErrorMessage);
                    return new List<ContentGapOpportunity>();
                }

                // Parse JSON response
                var gaps = ParseContentGapResponse(llmResponse.ResponseText ?? "");

                // Load existing active content gap topics to avoid duplicates
                var existingTopics = await _context.ContentGapOpportunities
                    .Where(c => c.ProjectId == projectId && (c.Status == "New" || c.Status == "InProgress"))
                    .Select(c => c.Topic)
                    .ToListAsync();

                // Save to database
                var opportunities = new List<ContentGapOpportunity>();
                foreach (var gap in gaps)
                {
                    // Skip if an active opportunity already exists for this topic
                    if (existingTopics.Contains(gap.Topic, StringComparer.OrdinalIgnoreCase))
                        continue;

                    var opportunity = new ContentGapOpportunity
                    {
                        ProjectId = projectId,
                        Topic = gap.Topic,
                        Description = gap.SuggestedPrompt,
                        RecommendedContentType = "Blog Post",
                        CompetitorCoverage = int.TryParse(gap.CompetitorCoverage, out int coverage) ? coverage : 0,
                        EstimatedImpact = gap.Priority ?? "Medium",
                        SearchVolume = gap.EstimatedImpact,
                        Difficulty = 50,
                        Status = "New",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.ContentGapOpportunities.Add(opportunity);
                    opportunities.Add(opportunity);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Detected {Count} content gap opportunities for project {ProjectId}", 
                    opportunities.Count, projectId);

                return opportunities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting content gaps for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<ContentGapOpportunity>> GetContentGapOpportunitiesAsync(
            int projectId, 
            string? status = null)
        {
            var query = _context.ContentGapOpportunities
                .Where(c => c.ProjectId == projectId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            // Deduplicate: keep only the latest record per Topic to handle existing DB duplicates
            var all = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return all
                .GroupBy(c => c.Topic, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderByDescending(c => c.EstimatedImpact)
                .ThenByDescending(c => c.CreatedAt)
                .ToList();
        }

        public async Task<bool> UpdateContentGapStatusAsync(int opportunityId, string status)
        {
            var opportunity = await _context.ContentGapOpportunities.FindAsync(opportunityId);
            if (opportunity == null)
                return false;

            opportunity.Status = status;
            opportunity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SeoOpportunity>> DetectSeoOpportunitiesAsync(
            int projectId, 
            int? siteAuditRunId = null)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    throw new Exception($"Project {projectId} not found");
                }

                // Get latest audit run if not specified
                if (siteAuditRunId == null)
                {
                    var latestAudit = await _context.SiteAuditRuns
                        .Where(a => a.ProjectId == projectId && a.Status == "Completed")
                        .OrderByDescending(a => a.CompletedAt)
                        .FirstOrDefaultAsync();

                    if (latestAudit == null)
                    {
                        _logger.LogWarning("No completed audit runs found for project {ProjectId}", projectId);
                        return new List<SeoOpportunity>();
                    }

                    siteAuditRunId = latestAudit.Id;
                }

                // Get audit details
                var auditDetails = await _context.SiteAuditDetails
                    .Where(d => d.SiteAuditRunId == siteAuditRunId.Value)
                    .ToListAsync();

                // Load existing active SEO opportunity audit detail IDs to avoid duplicates
                var existingSeoDetailIds = await _context.SeoOpportunities
                    .Where(s => s.ProjectId == projectId && s.SiteAuditDetailId != null && (s.Status == "Open" || s.Status == "InProgress"))
                    .Select(s => s.SiteAuditDetailId!.Value)
                    .ToListAsync();

                var opportunities = new List<SeoOpportunity>();

                // Analyze each failed or warning check
                foreach (var detail in auditDetails.Where(d => d.Status == "Fail" || d.Status == "Warning"))
                {
                    // Skip if an active opportunity already exists for this audit detail
                    if (existingSeoDetailIds.Contains(detail.Id))
                        continue;

                    var opportunity = new SeoOpportunity
                    {
                        ProjectId = projectId,
                        TeamId = project.TeamId,
                        SiteAuditDetailId = detail.Id,
                        PageUrl = detail.PageUrl ?? project.WebsiteUrl ?? "",
                        OpportunityType = DetermineOpportunityType(detail.CheckType, detail.CheckName),
                        Priority = DeterminePriority(detail.Status, detail.Priority),
                        CurrentState = detail.Message,
                        RecommendedAction = detail.Recommendation ?? "Review and fix this issue",
                        EstimatedImpact = CalculateImpact(detail.Status, detail.Priority),
                        DetailsJson = detail.DetailsJson,
                        Status = "Open",
                        DiscoveredAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.SeoOpportunities.Add(opportunity);
                    opportunities.Add(opportunity);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Detected {Count} SEO opportunities for project {ProjectId}", 
                    opportunities.Count, projectId);

                return opportunities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting SEO opportunities for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<SeoOpportunity>> GetSeoOpportunitiesAsync(
            int projectId, 
            string? priority = null)
        {
            var query = _context.SeoOpportunities
                .Where(s => s.ProjectId == projectId);

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(s => s.Priority == priority);
            }

            // Deduplicate: keep only the latest record per unique (OpportunityType, PageUrl)
            // This handles any duplicates already stored in the DB from previous job runs
            var all = await query
                .OrderByDescending(s => s.EstimatedImpact)
                .ThenByDescending(s => s.DiscoveredAt)
                .ToListAsync();

            return all
                .GroupBy(s => new { s.OpportunityType, s.PageUrl })
                .Select(g => g.First())
                .OrderByDescending(s => s.EstimatedImpact)
                .ThenByDescending(s => s.DiscoveredAt)
                .ToList();
        }

        public async Task<bool> UpdateSeoOpportunityStatusAsync(int opportunityId, string status)
        {
            var opportunity = await _context.SeoOpportunities.FindAsync(opportunityId);
            if (opportunity == null)
                return false;

            opportunity.Status = status;
            opportunity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkSeoOpportunityFixedAsync(int opportunityId)
        {
            var opportunity = await _context.SeoOpportunities.FindAsync(opportunityId);
            if (opportunity == null)
                return false;

            opportunity.Status = "Fixed";
            opportunity.FixedAt = DateTime.UtcNow;
            opportunity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<VisibilityOpportunity>> DetectVisibilityOpportunitiesAsync(int projectId)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    throw new Exception($"Project {projectId} not found");
                }

                // Get recent visibility metrics
                var recentMetrics = await _context.PromptVisibilityMetrics
                    .Include(m => m.PromptPlatform)
                        .ThenInclude(p => p.Prompt)
                    .Where(m => m.PromptPlatform.Prompt.ProjectId == projectId)
                    .Where(m => m.CheckDate >= DateTime.UtcNow.AddDays(-30))
                    .ToListAsync();

                var opportunities = new List<VisibilityOpportunity>();

                // Find prompts with low visibility
                var lowVisibilityPrompts = recentMetrics
                    .GroupBy(m => m.PromptPlatform.PromptId)
                    .Where(g => g.Average(m => m.VisibilityScore) < 30)
                    .Select(g => new
                    {
                        PromptId = g.Key,
                        AvgScore = g.Average(m => m.VisibilityScore),
                        Prompt = g.First().PromptPlatform.Prompt
                    })
                    .ToList();

                // Load existing active opportunity PromptTrackingIds per type — integer set, O(1) lookup
                var existingLowVisIds = (await _context.VisibilityOpportunities
                    .Where(v => v.ProjectId == projectId
                             && v.OpportunityType == "LowVisibility"
                             && v.PromptTrackingId != null
                             && (v.Status == "New" || v.Status == "InProgress"))
                    .Select(v => v.PromptTrackingId!.Value)
                    .ToListAsync()).ToHashSet();

                var existingCompetitorIds = (await _context.VisibilityOpportunities
                    .Where(v => v.ProjectId == projectId
                             && v.OpportunityType == "CompetitorDominance"
                             && v.PromptTrackingId != null
                             && (v.Status == "New" || v.Status == "InProgress"))
                    .Select(v => v.PromptTrackingId!.Value)
                    .ToListAsync()).ToHashSet();

                foreach (var lowVis in lowVisibilityPrompts)
                {
                    // Dedup by integer PromptTrackingId — no string comparison
                    if (existingLowVisIds.Contains(lowVis.PromptId))
                        continue;

                    var opportunity = new VisibilityOpportunity
                    {
                        ProjectId = projectId,
                        PromptTrackingId = lowVis.PromptId,
                        PromptText = lowVis.Prompt?.PromptText ?? string.Empty,
                        Platform = "All",
                        OpportunityType = "LowVisibility",
                        Reasoning = $"Average visibility score is {lowVis.AvgScore:F1}, below the 30% threshold",
                        RecommendedContent = "Improve content quality, add more relevant information, optimize for AI responses",
                        OptimizationStrategy = "Content optimization and brand authority building",
                        Priority = lowVis.AvgScore < 20 ? 1 : 2, // 1=High, 2=Medium
                        Status = "New",
                        DiscoveredAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.VisibilityOpportunities.Add(opportunity);
                    opportunities.Add(opportunity);
                }

                // Find prompts where competitors are mentioned more
                var competitorMentions = recentMetrics
                    .Where(m => m.BrandMentioned == false)
                    .GroupBy(m => m.PromptPlatform.PromptId)
                    .Where(g => g.Count() > 5)
                    .Select(g => new
                    {
                        PromptId = g.Key,
                        Prompt = g.First().PromptPlatform.Prompt
                    })
                    .ToList();

                foreach (var comp in competitorMentions)
                {
                    // Dedup by integer PromptTrackingId — no string comparison
                    if (existingCompetitorIds.Contains(comp.PromptId))
                        continue;

                    var opportunity = new VisibilityOpportunity
                    {
                        ProjectId = projectId,
                        PromptTrackingId = comp.PromptId,
                        PromptText = comp.Prompt?.PromptText ?? string.Empty,
                        Platform = "All",
                        OpportunityType = "CompetitorDominance",
                        Reasoning = "Brand not mentioned while competitors dominate responses",
                        RecommendedContent = "Analyze competitor content, create superior content, improve brand authority",
                        OptimizationStrategy = "Competitive content strategy and brand authority building",
                        Priority = 1, // 1=High
                        Status = "New",
                        DiscoveredAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.VisibilityOpportunities.Add(opportunity);
                    opportunities.Add(opportunity);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Detected {Count} visibility opportunities for project {ProjectId}", 
                    opportunities.Count, projectId);

                return opportunities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting visibility opportunities for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<VisibilityOpportunity>> GetVisibilityOpportunitiesAsync(
            int projectId, 
            string? type = null)
        {
            var query = _context.VisibilityOpportunities
                .Where(v => v.ProjectId == projectId);

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(v => v.OpportunityType == type);
            }

            // Deduplicate: prefer PromptTrackingId (int) as key; fall back to PromptText for legacy rows without it
            var all = await query
                .OrderBy(v => v.Priority)
                .ThenByDescending(v => v.DiscoveredAt)
                .ToListAsync();

            return all
                .GroupBy(v => new
                {
                    v.OpportunityType,
                    v.Platform,
                    DedupeKey = v.PromptTrackingId.HasValue
                        ? v.PromptTrackingId.Value.ToString()
                        : v.PromptText
                })
                .Select(g => g.First())
                .OrderBy(v => v.Priority)
                .ThenByDescending(v => v.DiscoveredAt)
                .ToList();
        }

        public async Task<bool> UpdateVisibilityOpportunityStatusAsync(int opportunityId, string status)
        {
            var opportunity = await _context.VisibilityOpportunities.FindAsync(opportunityId);
            if (opportunity == null)
                return false;

            opportunity.Status = status;
            opportunity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<OpportunitySummaryDto> GetOpportunitySummaryAsync(int projectId)
        {
            var contentGaps = await _context.ContentGapOpportunities
                .Where(c => c.ProjectId == projectId)
                .ToListAsync();

            var seoOpps = await _context.SeoOpportunities
                .Where(s => s.ProjectId == projectId)
                .ToListAsync();

            var visibilityOpps = await _context.VisibilityOpportunities
                .Where(v => v.ProjectId == projectId)
                .ToListAsync();

            var totalOpportunities = contentGaps.Count + seoOpps.Count + visibilityOpps.Count;
            var highPriority = contentGaps.Count(c => c.EstimatedImpact == "High") + 
                              seoOpps.Count(s => s.Priority == "High" || s.Priority == "Critical") +
                              visibilityOpps.Count(v => v.Priority == 1); // 1=High
            var inProgress = contentGaps.Count(c => c.Status == "InProgress") +
                            seoOpps.Count(s => s.Status == "InProgress") +
                            visibilityOpps.Count(v => v.Status == "InProgress");
            var fixedCount = contentGaps.Count(c => c.Status == "Implemented") +
                            seoOpps.Count(s => s.Status == "Fixed") +
                            visibilityOpps.Count(v => v.Status == "Actioned");

            return new OpportunitySummaryDto
            {
                TotalOpportunities = totalOpportunities,
                ContentGaps = contentGaps.Count,
                SeoIssues = seoOpps.Count,
                VisibilityGaps = visibilityOpps.Count,
                HighPriority = highPriority,
                InProgress = inProgress,
                Fixed = fixedCount
            };
        }

        private List<ContentGapDto> ParseContentGapResponse(string jsonResponse)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var response = JsonSerializer.Deserialize<ContentGapResponse>(jsonResponse, options);
                return response?.ContentGaps ?? new List<ContentGapDto>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing content gap JSON: {Response}", jsonResponse);
                return new List<ContentGapDto>();
            }
        }

        private string DetermineOpportunityType(string checkType, string checkName)
        {
            return checkType switch
            {
                "LlmsTxt" => "MissingLlmsTxt",
                "Schema" => "MissingSchema",
                "DomClarity" => "PoorDomClarity",
                "ContentStructure" => "PoorContentStructure",
                "TechnicalSEO" => "TechnicalSEO",
                _ => checkName
            };
        }

        private string DeterminePriority(string status, int priority)
        {
            if (status == "Fail" && priority >= 4)
                return "Critical";
            if (status == "Fail")
                return "High";
            if (status == "Warning" && priority >= 3)
                return "Medium";
            return "Low";
        }

        private int CalculateImpact(string status, int priority)
        {
            var baseImpact = status == "Fail" ? 70 : 40;
            var priorityBonus = priority * 5;
            return Math.Min(100, baseImpact + priorityBonus);
        }

        private class ContentGapResponse
        {
            public List<ContentGapDto> ContentGaps { get; set; } = new();
        }

        private class ContentGapDto
        {
            public string Topic { get; set; } = string.Empty;
            public string SuggestedPrompt { get; set; } = string.Empty;
            public string CompetitorCoverage { get; set; } = string.Empty;
            public int EstimatedImpact { get; set; }
            public string Priority { get; set; } = string.Empty;
            public string Reasoning { get; set; } = string.Empty;
        }
    }
}
