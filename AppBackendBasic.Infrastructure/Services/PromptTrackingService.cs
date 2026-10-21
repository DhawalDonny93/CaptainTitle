using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.Templates;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Infrastructure.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class PromptTrackingService : IPromptTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILlmProviderService _llmProviderService;
        private readonly ICompetitorService _competitorService;
        private readonly ILogger<PromptTrackingService> _logger;
        private readonly VisibilityResponseParser _visibilityParser;
        private readonly CitationParser _citationParser;
        private readonly SentimentParser _sentimentParser;

        public PromptTrackingService(
            ApplicationDbContext context,
            ILlmProviderService llmProviderService,
            ICompetitorService competitorService,
            ILogger<PromptTrackingService> logger,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _llmProviderService = llmProviderService;
            _competitorService = competitorService;
            _logger = logger;
            _visibilityParser = new VisibilityResponseParser(loggerFactory.CreateLogger<VisibilityResponseParser>());
            _citationParser = new CitationParser(loggerFactory.CreateLogger<CitationParser>());
            _sentimentParser = new SentimentParser(loggerFactory.CreateLogger<SentimentParser>());
        }

        public async Task<PromptTracking> CreatePromptAsync(int projectId, int teamId, int userId, string promptText, string? topic, List<string> platforms, bool? isCompetitor = null)
        {
            var prompt = new PromptTracking
            {
                ProjectId = projectId,
                TeamId = teamId,
                UserId = userId,
                PromptText = promptText,
                Topic = topic,
                Status = "Active",
                IsCompetitor = isCompetitor ?? false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PromptTrackings.Add(prompt);
            await _context.SaveChangesAsync();

            // Create platform tracking entries
            foreach (var platform in platforms)
            {
                var platformTracking = new PromptPlatformTracking
                {
                    PromptId = prompt.Id,
                    TeamId = teamId,
                    Platform = platform,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PromptPlatformTrackings.Add(platformTracking);
            }

            await _context.SaveChangesAsync();

            // Perform initial visibility check
            await CheckPromptVisibilityAsync(teamId, prompt.Id);

            return prompt;
        }

        public async Task<PromptTracking> UpdatePromptAsync(int promptId, int teamId, string promptText, string? topic, string status)
        {
            var prompt = await _context.PromptTrackings.FirstOrDefaultAsync(p => p.Id == promptId && p.TeamId == teamId);
            if (prompt == null)
            {
                throw new InvalidOperationException($"Prompt with ID {promptId} not found or access denied");
            }

            prompt.PromptText = promptText;
            prompt.Topic = topic;
            prompt.Status = status;
            prompt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return prompt;
        }

        public async Task<bool> DeletePromptAsync(int promptId, int teamId)
        {
            var prompt = await _context.PromptTrackings.FirstOrDefaultAsync(p => p.Id == promptId && p.TeamId == teamId);
            if (prompt == null)
            {
                return false;
            }

            _context.PromptTrackings.Remove(prompt);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PromptTracking?> GetPromptByIdAsync(int promptId)
        {
            return await _context.PromptTrackings
                .Include(p => p.PlatformTrackings)
                    .ThenInclude(pt => pt.VisibilityMetrics.OrderByDescending(m => m.CheckDate).Take(10))
                .FirstOrDefaultAsync(p => p.Id == promptId);
        }

        public async Task<List<PromptTracking>> GetPromptsAsync(int projectId, string? status = null, string? topic = null, bool? isActive = null, bool? isCompetitor = null)
        {
            var query = _context.PromptTrackings
                .Include(p => p.PlatformTrackings)
                .Where(p => p.ProjectId == projectId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrEmpty(topic))
            {
                query = query.Where(p => p.Topic == topic);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            if (isCompetitor.HasValue)
            {
                query = query.Where(p => p.IsCompetitor == isCompetitor.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PromptTracking>> SearchPromptsAsync(int projectId, string searchTerm)
        {
            return await _context.PromptTrackings
                .Include(p => p.PlatformTrackings)
                .Where(p => p.ProjectId == projectId &&
                           (p.PromptText.Contains(searchTerm) ||
                            (p.Topic != null && p.Topic.Contains(searchTerm))))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> BulkUpdateStatusAsync(List<int> promptIds, string status)
        {
            var prompts = await _context.PromptTrackings
                .Where(p => promptIds.Contains(p.Id))
                .ToListAsync();

            foreach (var prompt in prompts)
            {
                prompt.Status = status;
                prompt.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BulkDeleteAsync(List<int> promptIds)
        {
            var prompts = await _context.PromptTrackings
                .Where(p => promptIds.Contains(p.Id))
                .ToListAsync();

            _context.PromptTrackings.RemoveRange(prompts);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CheckPromptVisibilityAsync(int teamId, int promptId)
        {
            try
            {
                var prompt = await _context.PromptTrackings
                    .Include(p => p.Project)
                    .Include(p => p.PlatformTrackings)
                    .FirstOrDefaultAsync(p => p.TeamId == teamId && p.Id == promptId);

                if (prompt == null)
                {
                    _logger.LogWarning("Prompt {PromptId} not found for visibility check", promptId);
                    return;
                }

                var enabledPlatforms = prompt.PlatformTrackings
                    .Where(pt => pt.TeamId == teamId && pt.IsEnabled)
                    .ToList();

                if (!enabledPlatforms.Any())
                {
                    _logger.LogInformation("No enabled platforms for prompt {PromptId}", promptId);
                    return;
                }

                // Create test request
                var testRequest = new PromptTestRequest
                {
                    ProjectId = prompt.ProjectId,
                    PromptText = prompt.PromptText,
                    BrandName = prompt.Project.Name ?? "Brand",
                    BrandDomain = prompt.Project.Domain,
                    Platforms = enabledPlatforms.Select(p => p.Platform).ToList(),
                    RequestType = "PromptTest" // Background job context - uses OpportunityDetection JobKey
                };

                // Test prompt across platforms
                var testResponse = await _llmProviderService.TestPromptAsync(testRequest);

                if (!testResponse.Success)
                {
                    _logger.LogError("Prompt test failed for prompt {PromptId}: {Error}", promptId, testResponse.ErrorMessage);
                    return;
                }

                var visibilityScores = new List<double>();

                // Process results for each platform
                foreach (var result in testResponse.Results)
                {
                    var platformTracking = enabledPlatforms.FirstOrDefault(p => p.Platform == result.Platform);
                    if (platformTracking == null) continue;

                    // Update platform tracking
                    platformTracking.LastCheckDate = DateTime.UtcNow;
                    platformTracking.CurrentVisibilityScore = result.VisibilityScore;
                    platformTracking.BrandMentioned = result.BrandMentioned;
                    platformTracking.MentionPosition = result.MentionPosition;
                    platformTracking.ResponseSummary = result.ResponseText?.Length > 500 
                        ? result.ResponseText.Substring(0, 500) + "..." 
                        : result.ResponseText;
                    platformTracking.UpdatedAt = DateTime.UtcNow;

                    // Create conversation content in structured format
                    var conversationContent = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        prompt = prompt.PromptText,
                        response = result.ResponseText,
                        platform = result.Platform,
                        timestamp = DateTime.UtcNow,
                        brandName = prompt.Project.Name,
                        metadata = new
                        {
                            visibilityScore = result.VisibilityScore,
                            brandMentioned = result.BrandMentioned,
                            position = result.MentionPosition,
                            sentiment = result.Sentiment,
                            sentimentScore = result.SentimentScore
                        }
                    });

                    // Create visibility metric entry
                    var metric = new PromptVisibilityMetrics
                    {
                        PromptPlatformId = platformTracking.Id,
                        TeamId = teamId,
                        CheckDate = DateTime.UtcNow,
                        VisibilityScore = result.VisibilityScore,
                        Position = result.MentionPosition,
                        BrandMentioned = result.BrandMentioned,
                        Sentiment = result.Sentiment,
                        SentimentScore = result.SentimentScore,
                        ResponseText = result.ResponseText,
                        ConversationContent = conversationContent,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PromptVisibilityMetrics.Add(metric);
                    visibilityScores.Add(result.VisibilityScore);

                    // Track own brand mention with count
                    if (!string.IsNullOrEmpty(result.ResponseText) && !string.IsNullOrEmpty(prompt.Project.Name))
                    {
                        await TrackOwnBrandMentionAsync(
                            prompt.ProjectId,
                            teamId,
                            result.Platform,
                            prompt.PromptText,
                            result.ResponseText,
                            prompt.Project.Name,
                            result.MentionPosition,
                            result.Sentiment,
                            result.SentimentScore
                        );
                    }

                    // Process citations and detect SERP competitors
                    if (!string.IsNullOrEmpty(result.ResponseText))
                    {
                        await ProcessCitationsAndCompetitorsAsync(
                            prompt.ProjectId,
                            teamId,
                            result.Platform,
                            prompt.PromptText,
                            result.ResponseText,
                            prompt.Project.Domain
                        );
                    }
                }

                // Update average visibility score
                if (visibilityScores.Any())
                {
                    prompt.AverageVisibilityScore = visibilityScores.Average();
                }

                prompt.LastCheckedAt = DateTime.UtcNow;
                prompt.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Visibility check completed for prompt {PromptId}. Average score: {Score}", 
                    promptId, prompt.AverageVisibilityScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking visibility for prompt {PromptId}", promptId);
                throw;
            }
        }

        public async Task<List<PromptResearchData>> GetPromptResearchDataAsync(string? topic = null, int limit = 100)
        {
            try
            {
                var query = _context.PromptResearchDatas.AsQueryable();

                if (!string.IsNullOrEmpty(topic))
                {
                    query = query.Where(p => p.Topic == topic);
                }

                return await query
                    .OrderByDescending(p => p.RelevanceScore ?? 0)
                    .ThenByDescending(p => p.DiscoveredAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prompt research data for topic {Topic}", topic);
                return new List<PromptResearchData>();
            }
        }

        public async Task<PromptResearchData> AddResearchDataAsync(string promptText, string? topic, int? estimatedVolume, string difficulty)
        {
            var researchData = new PromptResearchData
            {
                PromptText = promptText,
                Topic = topic,
                EstimatedVolume = estimatedVolume?.ToString(),
                Difficulty = difficulty,
                Source = "Manual",
                DiscoveredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.PromptResearchDatas.Add(researchData);
            await _context.SaveChangesAsync();

            return researchData;
        }

        /// <summary>
        /// Process citations from AI response and detect SERP competitors
        /// </summary>
        private async Task ProcessCitationsAndCompetitorsAsync(
            int projectId,
            int teamId,
            string platform,
            string promptText,
            string responseText,
            string? ownBrandDomain)
        {
            try
            {
                // Parse citations from response
                var citations = _citationParser.ParseCitations(responseText);

                if (!citations.Any())
                {
                    _logger.LogDebug("No citations found in response for project {ProjectId} on {Platform}", projectId, platform);
                    return;
                }

                // Normalize own brand domain for comparison
                var normalizedOwnDomain = NormalizeDomain(ownBrandDomain ?? "");

                foreach (var citation in citations)
                {
                    var normalizedCitationDomain = NormalizeDomain(citation.Domain);

                    // Skip if this is the own brand domain
                    if (!string.IsNullOrEmpty(normalizedOwnDomain) && 
                        normalizedCitationDomain.Equals(normalizedOwnDomain, StringComparison.OrdinalIgnoreCase))
                    {
                        // This is own brand citation - create citation record without competitor
                        await CreateCitationRecordAsync(
                            projectId,
                            teamId,
                            platform,
                            promptText,
                            citation,
                            isCompetitor: false,
                            competitorId: null,
                            competitorType: null
                        );
                        continue;
                    }

                    // This is a competitor citation - find or create SERP competitor
                    var serpCompetitor = await _competitorService.FindOrCreateSerpCompetitorAsync(
                        projectId,
                        normalizedCitationDomain,
                        ExtractBrandNameFromDomain(normalizedCitationDomain)
                    );

                    // Create citation record linked to SERP competitor
                    await CreateCitationRecordAsync(
                        projectId,
                        teamId,
                        platform,
                        promptText,
                        citation,
                        isCompetitor: true,
                        competitorId: serpCompetitor.Id,
                        competitorType: "SERP"
                    );

                    // Also create brand mention for this competitor
                    await CreateBrandMentionRecordAsync(
                        projectId,
                        platform,
                        promptText,
                        responseText,
                        serpCompetitor.Id,
                        serpCompetitor.Name,
                        citation.Position
                    );

                    _logger.LogInformation(
                        "Detected SERP competitor {CompetitorName} ({Domain}) for project {ProjectId} on {Platform}",
                        serpCompetitor.Name,
                        serpCompetitor.Domain,
                        projectId,
                        platform
                    );
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing citations and competitors for project {ProjectId}", projectId);
            }
        }

        /// <summary>
        /// Create citation record in database
        /// </summary>
        private async Task CreateCitationRecordAsync(
            int projectId,
            int teamId,
            string platform,
            string promptText,
            DetectedCitation citation,
            bool isCompetitor,
            int? competitorId,
            string? competitorType)
        {
            // Check if citation already exists (by domain and platform to track frequency)
            var existing = await _context.Citations
                .FirstOrDefaultAsync(c =>
                c.TeamId == teamId &&
                    c.ProjectId == projectId &&
                    c.Platform == platform &&
                    c.SourceDomain == citation.Domain);

            if (existing != null)
            {
                // Update existing citation - increment count and update metadata
                existing.CitationCount++;
                existing.LastSeenAt = DateTime.UtcNow;
                existing.CompetitorId = competitorId;
                existing.CompetitorType = competitorType;
                existing.DiscoveredAt = DateTime.UtcNow;
                
                // Update position if this is a better (lower) position
                if (citation.Position > 0 && (!existing.Position.HasValue || citation.Position < existing.Position.Value))
                {
                    existing.Position = citation.Position;
                }
                return;
            }

            // Classify content type based on domain
            var contentType = ClassifyContentType(citation.Domain);

            // Create new citation
            var newCitation = new Citation
            {
                ProjectId = projectId,
                TeamId = teamId,
                Platform = platform,
                PromptText = promptText,
                SourceUrl = citation.Url,
                SourceDomain = citation.Domain,
                SourceTitle = citation.PagePath ?? citation.Domain,
                CitationType = citation.CitationType ?? "Reference",
                ContentType = contentType,
                CitationCount = 1,
                LastSeenAt = DateTime.UtcNow,
                Position = citation.Position,
                Context = citation.Context,
                IsCompetitor = isCompetitor,
                CompetitorId = competitorId,
                CompetitorType = competitorType,
                DiscoveredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Citations.Add(newCitation);
        }

        /// <summary>
        /// Classify content type based on domain patterns
        /// </summary>
        private string ClassifyContentType(string domain)
        {
            var domainLower = domain.ToLower();

            // Editorial/News sites
            if (domainLower.Contains("news") || domainLower.Contains("times") || 
                domainLower.Contains("post") || domainLower.Contains("journal") ||
                domainLower.Contains("techcrunch") || domainLower.Contains("wired") ||
                domainLower.Contains("forbes") || domainLower.Contains("bloomberg"))
            {
                return "Editorial";
            }

            // Review sites
            if (domainLower.Contains("review") || domainLower.Contains("rating") ||
                domainLower.Contains("capterra") || domainLower.Contains("g2") ||
                domainLower.Contains("trustpilot") || domainLower.Contains("yelp"))
            {
                return "Review";
            }

            // Social media
            if (domainLower.Contains("twitter") || domainLower.Contains("facebook") ||
                domainLower.Contains("linkedin") || domainLower.Contains("reddit") ||
                domainLower.Contains("youtube") || domainLower.Contains("instagram"))
            {
                return "Social";
            }

            // Institutional/Educational
            if (domainLower.EndsWith(".edu") || domainLower.EndsWith(".gov") ||
                domainLower.Contains("university") || domainLower.Contains("institute"))
            {
                return "Institutional";
            }

            // Documentation/Technical
            if (domainLower.Contains("docs") || domainLower.Contains("documentation") ||
                domainLower.Contains("github") || domainLower.Contains("stackoverflow"))
            {
                return "Technical";
            }

            // Default
            return "Other";
        }

        /// <summary>
        /// Create brand mention record for SERP competitor
        /// </summary>
        private async Task CreateBrandMentionRecordAsync(
            int projectId,
            string platform,
            string promptText,
            string responseText,
            int competitorId,
            string competitorName,
            int? position)
        {
            // Count how many times the competitor brand is mentioned
            var mentionCount = CountBrandMentions(responseText, competitorName);
            
            if (mentionCount == 0)
            {
                _logger.LogDebug("Competitor {CompetitorName} not found in response text", competitorName);
                return;
            }

            // Check if mention already exists for this check
            var checkDate = DateTime.UtcNow.Date;
            var existing = await _context.BrandMentions
                .FirstOrDefaultAsync(bm =>
                    bm.ProjectId == projectId &&
                    bm.Platform == platform &&
                    bm.PromptText == promptText &&
                    bm.CompetitorId == competitorId &&
                    bm.CheckDate.Date == checkDate);

            if (existing != null)
            {
                // Update existing mention
                existing.IsMentioned = true;
                existing.MentionCount = mentionCount;
                existing.Position = position;
                existing.CompetitorType = "SERP";
                return;
            }

            // Analyze sentiment for this competitor mention
            var sentimentAnalysis = _sentimentParser.AnalyzeTextBasic(responseText);

            // Create new brand mention
            var mention = new BrandMention
            {
                ProjectId = projectId,
                Platform = platform,
                PromptText = promptText,
                IsMentioned = true,
                MentionCount = mentionCount,
                Position = position,
                MentionType = "Direct",
                MentionContext = ExtractMentionContext(responseText, competitorName),
                Sentiment = sentimentAnalysis.OverallSentiment ?? "Neutral",
                SentimentScore = sentimentAnalysis.SentimentScore,
                ResponseText = responseText.Length > 1000 ? responseText.Substring(0, 1000) : responseText,
                IsCompetitor = true,
                CompetitorId = competitorId,
                CompetitorType = "SERP",
                CheckDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.BrandMentions.Add(mention);
        }

        /// <summary>
        /// Count how many times a brand name appears in the response text
        /// </summary>
        private int CountBrandMentions(string responseText, string brandName)
        {
            if (string.IsNullOrEmpty(responseText) || string.IsNullOrEmpty(brandName))
                return 0;

            var count = 0;
            var index = 0;
            var responseTextLower = responseText.ToLower();
            var brandNameLower = brandName.ToLower();

            while ((index = responseTextLower.IndexOf(brandNameLower, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += brandNameLower.Length;
            }

            return count;
        }

        /// <summary>
        /// Normalize domain for comparison
        /// </summary>
        private string NormalizeDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return string.Empty;

            domain = domain.ToLowerInvariant().Trim();
            domain = domain.Replace("www.", "");
            domain = domain.Replace("http://", "").Replace("https://", "");
            domain = domain.Split('/')[0]; // Remove path
            domain = domain.Split(':')[0]; // Remove port

            return domain;
        }

        /// <summary>
        /// Extract brand name from domain
        /// </summary>
        private string ExtractBrandNameFromDomain(string domain)
        {
            var parts = domain.Split('.');
            if (parts.Length > 1)
            {
                var name = parts[0];
                return char.ToUpperInvariant(name[0]) + name.Substring(1);
            }
            return domain;
        }

        /// <summary>
        /// Extract context around competitor mention
        /// </summary>
        private string? ExtractMentionContext(string responseText, string competitorName)
        {
            var index = responseText.IndexOf(competitorName, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
                return null;

            var start = Math.Max(0, index - 100);
            var length = Math.Min(200, responseText.Length - start);
            return responseText.Substring(start, length).Trim();
        }

        /// <summary>
        /// Track own brand mention with count
        /// </summary>
        private async Task TrackOwnBrandMentionAsync(
            int projectId,
            int teamId,
            string platform,
            string promptText,
            string responseText,
            string brandName,
            int? position,
            string? sentiment,
            double? sentimentScore)
        {
            // Count how many times the own brand is mentioned
            var mentionCount = CountBrandMentions(responseText, brandName);

            // Check if mention already exists for this check
            var checkDate = DateTime.UtcNow.Date;
            var existing = await _context.BrandMentions
                .FirstOrDefaultAsync(bm =>
                    bm.TeamId == teamId &&
                    bm.ProjectId == projectId &&
                    bm.Platform == platform &&
                    bm.PromptText == promptText &&
                    bm.IsCompetitor == false &&
                    bm.CheckDate.Date == checkDate);

            if (existing != null)
            {
                // Update existing mention
                existing.IsMentioned = mentionCount > 0;
                existing.MentionCount = mentionCount;
                existing.Position = position;
                existing.Sentiment = sentiment ?? "Neutral";
                existing.SentimentScore = sentimentScore;
                return;
            }

            // Create new brand mention for own brand
            var mention = new BrandMention
            {
                ProjectId = projectId,
                TeamId = teamId,
                Platform = platform,
                PromptText = promptText,
                IsMentioned = mentionCount > 0,
                MentionCount = mentionCount,
                Position = position,
                MentionType = "Direct",
                MentionContext = mentionCount > 0 ? ExtractMentionContext(responseText, brandName) : null,
                Sentiment = sentiment ?? "Neutral",
                SentimentScore = sentimentScore,
                ResponseText = responseText.Length > 1000 ? responseText.Substring(0, 1000) : responseText,
                IsCompetitor = false,
                CompetitorId = null,
                CompetitorType = null,
                CheckDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.BrandMentions.Add(mention);
        }

        /// <summary>
        /// Process all active prompts for scheduled visibility checks
        /// This is the main entry point for the background job
        /// </summary>
        public async Task ProcessAllActivePromptsAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting scheduled visibility checks at {Time}", startTime);

            try
            {
                // Get all active prompts that need checking
                var activePrompts = await _context.PromptTrackings
                    .Where(p => p.IsActive && p.Status == "Active")
                    .ToListAsync();

                // Filter prompts that haven't been checked in the last 24 hours
                var promptsToCheck = activePrompts.Where(p =>
                {
                    if (!p.LastCheckedAt.HasValue)
                        return true;

                    var hoursSinceLastCheck = (DateTime.UtcNow - p.LastCheckedAt.Value).TotalHours;
                    return hoursSinceLastCheck >= 23; // Check daily
                }).ToList();

                _logger.LogInformation("Processing {Count} prompts for visibility check (out of {Total} active)",
                    promptsToCheck.Count, activePrompts.Count);

                var successCount = 0;
                var failureCount = 0;

                foreach (var prompt in promptsToCheck)
                {
                    try
                    {
                        _logger.LogDebug("Checking visibility for prompt {PromptId}: {PromptText}",
                            prompt.Id, prompt.PromptText);

                        await CheckPromptVisibilityAsync(prompt.TeamId, prompt.Id);
                        successCount++;

                        // Rate limiting - delay between prompts to avoid overwhelming LLM APIs
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex, "Error checking visibility for prompt {PromptId}", prompt.Id);
                    }
                }

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalSeconds;

                _logger.LogInformation(
                    "Scheduled visibility checks completed. Success: {Success}, Failed: {Failed}, Duration: {Duration}s",
                    successCount, failureCount, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessAllActivePromptsAsync");
                throw;
            }
        }

    }
}
