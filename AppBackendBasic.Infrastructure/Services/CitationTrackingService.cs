using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class CitationTrackingService : ICitationTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILlmProviderService _llmProviderService;
        private readonly ILogger<CitationTrackingService> _logger;

        public CitationTrackingService(
            ApplicationDbContext context,
            ILlmProviderService llmProviderService,
            ILogger<CitationTrackingService> logger)
        {
            _context = context;
            _llmProviderService = llmProviderService;
            _logger = logger;
        }

        public async Task<List<Citation>> GetCitationsAsync(int projectId, string? platform = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Citations
                .Include(c => c.Metrics.OrderByDescending(m => m.CheckDate).Take(10))
                .Where(c => c.ProjectId == projectId);

            if (!string.IsNullOrEmpty(platform))
            {
                query = query.Where(c => c.Platform == platform);
            }

            if (startDate.HasValue)
            {
                query = query.Where(c => c.DiscoveredAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.DiscoveredAt <= endDate.Value);
            }

            return await query
                .OrderByDescending(c => c.DiscoveredAt)
                .ToListAsync();
        }

        public async Task<Citation?> GetCitationByIdAsync(int citationId)
        {
            return await _context.Citations
                .Include(c => c.Metrics.OrderByDescending(m => m.CheckDate))
                .FirstOrDefaultAsync(c => c.Id == citationId);
        }

        public async Task<Dictionary<string, object>> GetCitationStatsAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Citations.Where(c => c.ProjectId == projectId);

            if (startDate.HasValue)
            {
                query = query.Where(c => c.DiscoveredAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.DiscoveredAt <= endDate.Value);
            }

            var citations = await query.ToListAsync();

            var stats = new Dictionary<string, object>
            {
                ["totalCitations"] = citations.Count,
                ["uniqueDomains"] = citations.Select(c => c.SourceDomain).Distinct().Count(),
                ["byPlatform"] = citations.GroupBy(c => c.Platform)
                    .Select(g => new { platform = g.Key, count = g.Count() })
                    .OrderByDescending(x => x.count)
                    .ToList(),
                ["byCitationType"] = citations.GroupBy(c => c.CitationType)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .ToList(),
                ["topDomains"] = citations.GroupBy(c => c.SourceDomain)
                    .Select(g => new { domain = g.Key, count = g.Count() })
                    .OrderByDescending(x => x.count)
                    .Take(10)
                    .ToList(),
                ["recentCitations"] = citations.OrderByDescending(c => c.DiscoveredAt)
                    .Take(5)
                    .Select(c => new
                    {
                        id = c.Id,
                        sourceUrl = c.SourceUrl,
                        sourceDomain = c.SourceDomain,
                        platform = c.Platform,
                        discoveredAt = c.DiscoveredAt
                    })
                    .ToList()
            };

            return stats;
        }

        public async Task<List<BrandMention>> GetBrandMentionsAsync(int projectId, string? platform = null, bool? isMentioned = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.BrandMentions.Where(m => m.ProjectId == projectId);

            if (!string.IsNullOrEmpty(platform))
            {
                query = query.Where(m => m.Platform == platform);
            }

            if (isMentioned.HasValue)
            {
                query = query.Where(m => m.IsMentioned == isMentioned.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(m => m.CheckDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(m => m.CheckDate <= endDate.Value);
            }

            return await query
                .OrderByDescending(m => m.CheckDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, object>> GetBrandMentionStatsAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.BrandMentions.Where(m => m.ProjectId == projectId);

            if (startDate.HasValue)
            {
                query = query.Where(m => m.CheckDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(m => m.CheckDate <= endDate.Value);
            }

            var mentions = await query.ToListAsync();
            var mentionedCount = mentions.Count(m => m.IsMentioned);
            var totalChecks = mentions.Count;

            var stats = new Dictionary<string, object>
            {
                ["totalChecks"] = totalChecks,
                ["mentionedCount"] = mentionedCount,
                ["mentionRate"] = totalChecks > 0 ? (double)mentionedCount / totalChecks * 100 : 0,
                ["byPlatform"] = mentions.GroupBy(m => m.Platform)
                    .Select(g => new
                    {
                        platform = g.Key,
                        total = g.Count(),
                        mentioned = g.Count(m => m.IsMentioned),
                        mentionRate = g.Count() > 0 ? (double)g.Count(m => m.IsMentioned) / g.Count() * 100 : 0
                    })
                    .ToList(),
                ["bySentiment"] = mentions.Where(m => m.IsMentioned)
                    .GroupBy(m => m.Sentiment)
                    .Select(g => new { sentiment = g.Key, count = g.Count() })
                    .ToList(),
                ["byMentionType"] = mentions.Where(m => m.IsMentioned)
                    .GroupBy(m => m.MentionType)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .ToList(),
                ["averagePosition"] = mentions.Where(m => m.IsMentioned && m.Position.HasValue)
                    .Average(m => (double?)m.Position) ?? 0,
                ["recentMentions"] = mentions.Where(m => m.IsMentioned)
                    .OrderByDescending(m => m.CheckDate)
                    .Take(5)
                    .Select(m => new
                    {
                        id = m.Id,
                        platform = m.Platform,
                        promptText = m.PromptText,
                        position = m.Position,
                        sentiment = m.Sentiment,
                        checkDate = m.CheckDate
                    })
                    .ToList()
            };

            return stats;
        }

        public async Task TrackCitationsForPromptAsync(int projectId, string promptText, string platform, string responseText)
        {
            try
            {
                // Use LLM service to analyze citations
                var citationAnalysis = await _llmProviderService.AnalyzeCitationsAsync(new CitationAnalysisRequest
                {
                    ProjectId = projectId,
                    ResponseText = responseText
                });

                if (!citationAnalysis.Success || citationAnalysis.Citations == null)
                {
                    _logger.LogWarning("Citation analysis failed for project {ProjectId}", projectId);
                    return;
                }

                foreach (var detectedCitation in citationAnalysis.Citations)
                {
                    // Check if citation already exists
                    var existingCitation = await _context.Citations
                        .FirstOrDefaultAsync(c =>
                            c.ProjectId == projectId &&
                            c.SourceUrl == detectedCitation.Url &&
                            c.Platform == platform);

                    if (existingCitation == null)
                    {
                        // Create new citation
                        var citation = new Citation
                        {
                            ProjectId = projectId,
                            SourceUrl = detectedCitation.Url,
                            SourceDomain = detectedCitation.Domain ?? ExtractDomainFromUrl(detectedCitation.Url),
                            SourceTitle = detectedCitation.PagePath ?? detectedCitation.Url,
                            Platform = platform,
                            PromptText = promptText,
                            Position = detectedCitation.Position,
                            CitationType = detectedCitation.CitationType ?? "DirectLink",
                            Context = detectedCitation.Context,
                            DiscoveredAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Citations.Add(citation);
                        await _context.SaveChangesAsync();

                        // Create initial metric
                        var metric = new CitationMetric
                        {
                            CitationId = citation.Id,
                            CheckDate = DateTime.UtcNow,
                            Position = detectedCitation.Position,
                            IsVisible = true,
                            ResponseContext = detectedCitation.Context,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.CitationMetrics.Add(metric);
                    }
                    else
                    {
                        // Update existing citation and add new metric
                        var metric = new CitationMetric
                        {
                            CitationId = existingCitation.Id,
                            CheckDate = DateTime.UtcNow,
                            Position = detectedCitation.Position,
                            IsVisible = true,
                            ResponseContext = detectedCitation.Context,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.CitationMetrics.Add(metric);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Tracked {Count} citations for project {ProjectId}", citationAnalysis.Citations.Count, projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking citations for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task TrackBrandMentionAsync(int projectId, string promptText, string platform, string responseText, string brandName, string? brandDomain = null)
        {
            try
            {
                var responseTextLower = responseText.ToLower();
                var brandNameLower = brandName.ToLower();

                var isMentioned = responseTextLower.Contains(brandNameLower);
                int? position = null;
                string? mentionContext = null;
                string mentionType = "Direct";

                if (isMentioned)
                {
                    // Find mention position
                    var sentences = responseText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < sentences.Length; i++)
                    {
                        if (sentences[i].ToLower().Contains(brandNameLower))
                        {
                            position = i + 1;
                            mentionContext = sentences[i].Trim();
                            break;
                        }
                    }

                    // Determine mention type
                    if (!string.IsNullOrEmpty(brandDomain) && responseTextLower.Contains(brandDomain.ToLower()))
                    {
                        mentionType = "Direct";
                    }
                    else if (responseTextLower.Contains("vs") || responseTextLower.Contains("compared to") || responseTextLower.Contains("alternative"))
                    {
                        mentionType = "Comparison";
                    }
                    else
                    {
                        mentionType = "Indirect";
                    }
                }

                // Analyze sentiment if mentioned
                string sentiment = "Neutral";
                double? sentimentScore = null;

                if (isMentioned && !string.IsNullOrEmpty(mentionContext))
                {
                    var sentimentAnalysis = await _llmProviderService.AnalyzeSentimentAsync(new SentimentAnalysisRequest
                    {
                        Text = mentionContext
                    });

                    if (sentimentAnalysis.Success)
                    {
                        sentiment = sentimentAnalysis.OverallSentiment;
                        sentimentScore = sentimentAnalysis.SentimentScore;
                    }
                }

                // Create brand mention record
                var brandMention = new BrandMention
                {
                    ProjectId = projectId,
                    Platform = platform,
                    PromptText = promptText,
                    IsMentioned = isMentioned,
                    Position = position,
                    MentionType = mentionType,
                    MentionContext = mentionContext,
                    Sentiment = sentiment,
                    SentimentScore = sentimentScore,
                    ResponseText = responseText.Length > 2000 ? responseText.Substring(0, 2000) : responseText,
                    CheckDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BrandMentions.Add(brandMention);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tracked brand mention for project {ProjectId}, mentioned: {IsMentioned}", projectId, isMentioned);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking brand mention for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task AggregateDailyCitationsAsync(int projectId, DateTime date)
        {
            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);

                var citations = await _context.Citations
                    .Where(c => c.ProjectId == projectId &&
                               c.DiscoveredAt >= startDate &&
                               c.DiscoveredAt < endDate)
                    .ToListAsync();

                var groupedCitations = citations
                    .GroupBy(c => new { c.SourceDomain, c.Platform });

                foreach (var group in groupedCitations)
                {
                    var existingAggregation = await _context.CitationAggregations
                        .FirstOrDefaultAsync(a =>
                            a.ProjectId == projectId &&
                            a.SourceDomain == group.Key.SourceDomain &&
                            a.Platform == group.Key.Platform &&
                            a.AggregationDate == startDate);

                    var citationList = group.ToList();
                    var positions = citationList.Where(c => c.Position.HasValue).Select(c => c.Position!.Value).ToList();

                    if (existingAggregation == null)
                    {
                        var aggregation = new CitationAggregation
                        {
                            ProjectId = projectId,
                            SourceDomain = group.Key.SourceDomain,
                            Platform = group.Key.Platform,
                            AggregationDate = startDate,
                            TotalCitations = citationList.Count,
                            UniqueCitations = citationList.Select(c => c.SourceUrl).Distinct().Count(),
                            AveragePosition = positions.Any() ? positions.Average() : 0,
                            TopPosition = positions.Any() ? positions.Min() : 999,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.CitationAggregations.Add(aggregation);
                    }
                    else
                    {
                        existingAggregation.TotalCitations = citationList.Count;
                        existingAggregation.UniqueCitations = citationList.Select(c => c.SourceUrl).Distinct().Count();
                        existingAggregation.AveragePosition = positions.Any() ? positions.Average() : 0;
                        existingAggregation.TopPosition = positions.Any() ? positions.Min() : 999;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Aggregated citations for project {ProjectId} on {Date}", projectId, date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating citations for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<object>> GetTopCitedDomainsAsync(int projectId, int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.LogInformation("GetTopCitedDomainsAsync called for project {ProjectId}, limit {Limit}, startDate {StartDate}, endDate {EndDate}", 
                projectId, limit, startDate, endDate);

            var query = _context.CitationAggregations.Where(a => a.ProjectId == projectId);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.AggregationDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.AggregationDate <= endDate.Value.Date);
            }

            var aggregations = await query.ToListAsync();
            _logger.LogInformation("Found {Count} citation aggregations for project {ProjectId}", aggregations.Count, projectId);

            // If no aggregations exist, try to get data from Citations table directly
            if (aggregations.Count == 0)
            {
                _logger.LogWarning("No citation aggregations found, falling back to Citations table");
                var citationsQuery = _context.Citations.Where(c => c.ProjectId == projectId);

                if (startDate.HasValue)
                {
                    citationsQuery = citationsQuery.Where(c => c.DiscoveredAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    citationsQuery = citationsQuery.Where(c => c.DiscoveredAt <= endDate.Value);
                }

                var citations = await citationsQuery.ToListAsync();
                _logger.LogInformation("Found {Count} citations for project {ProjectId}", citations.Count, projectId);

                var topDomainsFromCitations = citations
                    .Where(c => !string.IsNullOrEmpty(c.SourceDomain))
                    .GroupBy(c => c.SourceDomain)
                    .Select(g => new
                    {
                        domain = g.Key,
                        citationCount = g.Count(),
                        averagePosition = g.Average(c => c.Position),
                        isCompetitor = g.Any(c => c.IsCompetitor),
                        competitorType = g.FirstOrDefault(c => c.IsCompetitor)?.CompetitorType
                    })
                    .OrderByDescending(x => x.citationCount)
                    .Take(limit)
                    .Cast<object>()
                    .ToList();

                _logger.LogInformation("Returning {Count} top domains from Citations table", topDomainsFromCitations.Count);
                return topDomainsFromCitations;
            }

            // Get domain list from aggregations
            var domainGroups = aggregations
                .GroupBy(a => a.SourceDomain)
                .Select(g => new
                {
                    domain = g.Key,
                    citationCount = g.Sum(a => a.TotalCitations),
                    averagePosition = g.Average(a => a.AveragePosition)
                })
                .OrderByDescending(x => x.citationCount)
                .Take(limit)
                .ToList();

            // Get isCompetitor info from Citations table for these domains
            var domains = domainGroups.Select(d => d.domain).ToList();
            var citationsForDomains = await _context.Citations
                .Where(c => c.ProjectId == projectId && domains.Contains(c.SourceDomain))
                .ToListAsync();

            var topDomains = domainGroups
                .Select(d =>
                {
                    var domainCitations = citationsForDomains.Where(c => c.SourceDomain == d.domain).ToList();
                    return new
                    {
                        domain = d.domain,
                        citationCount = d.citationCount,
                        averagePosition = d.averagePosition,
                        isCompetitor = domainCitations.Any(c => c.IsCompetitor),
                        competitorType = domainCitations.FirstOrDefault(c => c.IsCompetitor)?.CompetitorType
                    };
                })
                .Cast<object>()
                .ToList();

            _logger.LogInformation("Returning {Count} top domains from CitationAggregations", topDomains.Count);
            return topDomains;
        }

        private string ExtractDomainFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host.Replace("www.", "");
            }
            catch
            {
                return url;
            }
        }

        public async Task<SourcesAnalyticsDto> GetSourcesAnalyticsAsync(int projectId, string? platform = null, string? topic = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-7);
                var end = endDate ?? DateTime.UtcNow;

                // Get project info
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    throw new InvalidOperationException($"Project {projectId} not found");
                }

                // Get all citations for the project
                var citationsQuery = _context.Citations
                    .Where(c => c.ProjectId == projectId && c.DiscoveredAt >= start && c.DiscoveredAt <= end);

                if (!string.IsNullOrEmpty(platform))
                {
                    citationsQuery = citationsQuery.Where(c => c.Platform == platform);
                }

                var citations = await citationsQuery.ToListAsync();

                // Get prompts for topic filtering
                var promptsQuery = _context.PromptTrackings.Where(p => p.ProjectId == projectId);
                if (!string.IsNullOrEmpty(topic))
                {
                    promptsQuery = promptsQuery.Where(p => p.Topic == topic);
                }
                var promptTexts = await promptsQuery.Select(p => p.PromptText).ToListAsync();

                if (!string.IsNullOrEmpty(topic))
                {
                    citations = citations.Where(c => promptTexts.Contains(c.PromptText ?? "")).ToList();
                }

                // Calculate insights
                var topDomains = citations
                    .GroupBy(c => c.SourceDomain)
                    .Select(g => new { Domain = g.Key, Count = g.Sum(c => c.CitationCount) })
                    .OrderByDescending(x => x.Count)
                    .Take(3)
                    .ToList();

                var insights = new SourcesInsightsDto
                {
                    TopSource = topDomains.FirstOrDefault()?.Domain ?? "",
                    TopSourceCitations = topDomains.FirstOrDefault()?.Count ?? 0,
                    SecondSource = topDomains.Skip(1).FirstOrDefault()?.Domain ?? "",
                    SecondSourcePercentage = citations.Any() ? (topDomains.Skip(1).FirstOrDefault()?.Count ?? 0) * 100.0 / citations.Count : 0,
                    ThirdSource = topDomains.Skip(2).FirstOrDefault()?.Domain ?? "",
                    ThirdSourcePercentage = citations.Any() ? (topDomains.Skip(2).FirstOrDefault()?.Count ?? 0) * 100.0 / citations.Count : 0
                };

                // Calculate time series data
                var dates = Enumerable.Range(0, (end - start).Days + 1)
                    .Select(i => start.AddDays(i).ToString("MMM dd"))
                    .ToList();

                var ownBrandCitations = citations.Where(c => !c.IsCompetitor).ToList();
                var competitorCitations = citations.Where(c => c.IsCompetitor).ToList();

                var youData = dates.Select((date, index) =>
                    ownBrandCitations.Count(c => c.DiscoveredAt.ToString("MMM dd") == date)).ToList();
                var competitorsData = dates.Select((date, index) =>
                    competitorCitations.Count(c => c.DiscoveredAt.ToString("MMM dd") == date)).ToList();

                var analytics = new SourcesChartsDto
                {
                    Sources = new SourcesTimeSeriesDto
                    {
                        Dates = dates,
                        You = youData,
                        Competitors = competitorsData,
                        Unattributed = dates.Select(_ => 0).ToList()
                    },
                    BrandSources = ownBrandCitations
                        .GroupBy(c => c.SourceDomain)
                        .Select(g => new BrandSourceDto
                        {
                            Domain = g.Key,
                            Usage = g.Sum(c => c.CitationCount),
                            LastSeen = g.Max(c => c.LastSeenAt ?? c.DiscoveredAt)
                        })
                        .OrderByDescending(b => b.Usage)
                        .Take(10)
                        .ToList()
                };

                // Calculate sources table data
                var totalCitations = citations.Count;
                var sources = citations
                    .GroupBy(c => c.SourceDomain)
                    .Select((g, index) => new SourceDto
                    {
                        Rank = index + 1,
                        Domain = g.Key,
                        ContentType = g.First().ContentType,
                        Usage = totalCitations > 0 ? (g.Count() * 100.0 / totalCitations) : 0,
                        AvgCitations = g.Average(c => c.CitationCount),
                        PopularArticleTypes = new List<string>(),
                        SourceTypes = new List<string>(),
                        Platforms = g.Select(c => c.Platform).Distinct().ToList()
                    })
                    .OrderByDescending(s => s.Usage)
                    .ToList();

                // Calculate content type distribution
                var contentTypeDistribution = citations
                    .GroupBy(c => c.ContentType)
                    .Select(g => new ContentTypeDistributionDto
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        Percentage = totalCitations > 0 ? (g.Count() * 100.0 / totalCitations) : 0
                    })
                    .OrderByDescending(c => c.Percentage)
                    .ToList();

                return new SourcesAnalyticsDto
                {
                    Insights = insights,
                    Opportunities = new List<OpportunityDto>(),
                    Analytics = analytics,
                    Sources = sources,
                    ContentTypeDistribution = contentTypeDistribution
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sources analytics for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<MentionsAnalyticsDto> GetMentionsAnalyticsAsync(int projectId, string? platform = null, string? topic = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-7);
                var end = endDate ?? DateTime.UtcNow;

                // Get project info
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                {
                    throw new InvalidOperationException($"Project {projectId} not found");
                }

                // Get all brand mentions for the project
                var mentionsQuery = _context.BrandMentions
                    .Where(m => m.ProjectId == projectId && m.CheckDate >= start && m.CheckDate <= end);

                if (!string.IsNullOrEmpty(platform))
                {
                    mentionsQuery = mentionsQuery.Where(m => m.Platform == platform);
                }

                var mentions = await mentionsQuery.ToListAsync();

                // Get prompts for topic filtering
                var promptsQuery = _context.PromptTrackings.Where(p => p.ProjectId == projectId);
                if (!string.IsNullOrEmpty(topic))
                {
                    promptsQuery = promptsQuery.Where(p => p.Topic == topic);
                }
                var promptTexts = await promptsQuery.Select(p => p.PromptText).ToListAsync();

                if (!string.IsNullOrEmpty(topic))
                {
                    mentions = mentions.Where(m => promptTexts.Contains(m.PromptText)).ToList();
                }

                // Calculate insights
                var ownBrandMentions = mentions.Where(m => !m.IsCompetitor).ToList();
                var competitorMentions = mentions.Where(m => m.IsCompetitor).ToList();

                var totalMentions = ownBrandMentions.Sum(m => m.MentionCount);
                var platformCounts = ownBrandMentions
                    .GroupBy(m => m.Platform)
                    .Select(g => new { Platform = g.Key, Count = g.Sum(m => m.MentionCount) })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var insights = new MentionsInsightsDto
                {
                    TotalMentions = totalMentions,
                    TotalCitations = 0, // TODO: Link with citations from CitationData table once citation aggregation is complete
                    // Currently hardcoded to 0 - should sum citations.Count for the period from CitationData table filtered by ProjectId and date range
                    TopPlatform = platformCounts.FirstOrDefault()?.Platform ?? "N/A",
                    KeyInsights = new List<string>()
                };

                // Calculate time series data
                var dates = Enumerable.Range(0, (end - start).Days + 1)
                    .Select(i => start.AddDays(i).ToString("MMM dd"))
                    .ToList();

                var brandData = new Dictionary<string, List<int>>
                {
                    ["You"] = dates.Select(date =>
                        ownBrandMentions.Where(m => m.CheckDate.ToString("MMM dd") == date).Sum(m => m.MentionCount)).ToList()
                };

                // Add competitor data
                var competitors = await _context.Competitors
                    .Where(c => c.ProjectId == projectId)
                    .ToListAsync();

                foreach (var competitor in competitors.Take(5))
                {
                    var compMentions = mentions.Where(m => m.CompetitorId == competitor.Id).ToList();
                    brandData[competitor.Name] = dates.Select(date =>
                        compMentions.Where(m => m.CheckDate.ToString("MMM dd") == date).Sum(m => m.MentionCount)).ToList();
                }

                // Calculate position distribution
                var positionDistribution = new List<PositionDistributionDto>();
                
                // Own brand
                var ownPositions = ownBrandMentions.Where(m => m.Position.HasValue).ToList();
                if (ownPositions.Any())
                {
                    positionDistribution.Add(new PositionDistributionDto
                    {
                        Brand = project.Name ?? "Your Brand",
                        Positions = new Dictionary<string, int>
                        {
                            ["1"] = CalculatePositionPercentage(ownPositions, 1),
                            ["2"] = CalculatePositionPercentage(ownPositions, 2),
                            ["3"] = CalculatePositionPercentage(ownPositions, 3),
                            ["4"] = CalculatePositionPercentage(ownPositions, 4),
                            ["5+"] = CalculatePositionPercentage(ownPositions, 5, true)
                        }
                    });
                }

                // Competitors
                foreach (var competitor in competitors.Take(5))
                {
                    var compPositions = mentions.Where(m => m.CompetitorId == competitor.Id && m.Position.HasValue).ToList();
                    if (compPositions.Any())
                    {
                        positionDistribution.Add(new PositionDistributionDto
                        {
                            Brand = competitor.Name,
                            Positions = new Dictionary<string, int>
                            {
                                ["1"] = CalculatePositionPercentage(compPositions, 1),
                                ["2"] = CalculatePositionPercentage(compPositions, 2),
                                ["3"] = CalculatePositionPercentage(compPositions, 3),
                                ["4"] = CalculatePositionPercentage(compPositions, 4),
                                ["5+"] = CalculatePositionPercentage(compPositions, 5, true)
                            }
                        });
                    }
                }

                var analytics = new MentionsChartsDto
                {
                    PerformanceOverTime = new MentionsTimeSeriesDto
                    {
                        Dates = dates,
                        BrandData = brandData
                    },
                    PositionDistribution = positionDistribution
                };

                // Get topics
                var topics = await _context.PromptTrackings
                    .Where(p => p.ProjectId == projectId)
                    .GroupBy(p => p.Topic ?? "Unclassified")
                    .Select(g => new TopicDto
                    {
                        Name = g.Key,
                        Count = g.Count(),
                        Status = "Active",
                        PerformanceScore = 0
                    })
                    .ToListAsync();

                // Get all mentions with prompt details
                var mentionsList = new List<MentionDto>();
                
                var promptGroups = await _context.BrandMentions
                    .Where(m => m.ProjectId == projectId && !m.IsCompetitor && m.CheckDate >= start && m.CheckDate <= end)
                    .GroupBy(m => m.PromptText)
                    .ToListAsync();

                foreach (var group in promptGroups)
                {
                    var promptText = group.Key;
                    
                    // Get citations for this prompt
                    var citationCount = await _context.Citations
                        .Where(c => c.ProjectId == projectId && c.PromptText == promptText && c.DiscoveredAt >= start && c.DiscoveredAt <= end)
                        .CountAsync();

                    // Get platforms where mentioned
                    var platforms = group.Where(m => m.IsMentioned).Select(m => m.Platform).Distinct().ToList();
                    var locations = platforms.Any() ? string.Join(", ", platforms) : "N/A";

                    // Get competitors mentioned in this prompt
                    var competitorMentionsForPrompt = await _context.BrandMentions
                        .Where(m => m.ProjectId == projectId && m.IsCompetitor && m.PromptText == promptText && m.CheckDate >= start && m.CheckDate <= end)
                        .Include(m => m.Competitor)
                        .ToListAsync();

                    var competitorsList = competitorMentionsForPrompt
                        .Where(m => m.Competitor != null)
                        .Select(m => new CompetitorMentionDto
                        {
                            Name = m.Competitor!.Name,
                            Type = m.CompetitorType ?? "Unknown"
                        })
                        .DistinctBy(c => c.Name)
                        .ToList();

                    mentionsList.Add(new MentionDto
                    {
                        Prompt = promptText,
                        Mentioned = group.Any(m => m.IsMentioned),
                        Locations = locations,
                        Mentions = group.Sum(m => m.MentionCount),
                        Citations = citationCount,
                        AvgPosition = group.Where(m => m.Position.HasValue).Any() 
                            ? group.Where(m => m.Position.HasValue).Average(m => m.Position!.Value) 
                            : null,
                        IsCompetitor = false,
                        Competitors = competitorsList
                    });
                }

                return new MentionsAnalyticsDto
                {
                    Insights = insights,
                    Opportunities = new List<OpportunityDto>(),
                    Analytics = analytics,
                    Topics = topics,
                    Mentions = mentionsList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mentions analytics for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<CitationDetailsDto> GetCitationDetailsByDomainAsync(int projectId, string domain, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.Citations
                    .Include(c => c.Metrics)
                    .Where(c => c.ProjectId == projectId && c.SourceDomain == domain);

                if (startDate.HasValue)
                {
                    query = query.Where(c => c.DiscoveredAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(c => c.DiscoveredAt <= endDate.Value);
                }

                var citations = await query.ToListAsync();
                var totalCitations = citations.Count;

                // Get all citations for the project to calculate shares
                var allProjectCitations = await _context.Citations
                    .Where(c => c.ProjectId == projectId)
                    .ToListAsync();

                var totalProjectCitations = allProjectCitations.Count;
                var citationShare = totalProjectCitations > 0 ? (totalCitations * 100.0) / totalProjectCitations : 0;

                // Get aggregation data for this domain
                var aggregations = await _context.CitationAggregations
                    .Where(a => a.ProjectId == projectId && a.SourceDomain == domain)
                    .OrderBy(a => a.AggregationDate)
                    .ToListAsync();

                // Calculate rank
                var domainCounts = await _context.Citations
                    .Where(c => c.ProjectId == projectId)
                    .GroupBy(c => c.SourceDomain)
                    .Select(g => new { Domain = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                var rank = domainCounts.FindIndex(d => d.Domain == domain) + 1;

                // Group citations by URL for pages
                var pageGroups = citations
                    .GroupBy(c => c.SourceUrl)
                    .Select((g, index) => new CitationPageDto
                    {
                        Rank = index + 1,
                        Url = g.Key,
                        Title = g.First().SourceTitle,
                        Share = totalCitations > 0 ? (g.Count() * 100.0) / totalCitations : 0,
                        ShareChange = 0, // Calculate from historical data if available
                        GlobalShare = totalProjectCitations > 0 ? (g.Count() * 100.0) / totalProjectCitations : 0,
                        GlobalShareChange = 0,
                        LastSeen = g.Max(c => c.LastSeenAt ?? c.DiscoveredAt)
                    })
                    .OrderByDescending(p => p.Share)
                    .ToList();

                // Get prompts that cited this domain
                var promptTexts = citations
                    .Where(c => !string.IsNullOrEmpty(c.PromptText))
                    .GroupBy(c => c.PromptText)
                    .Select(g => new CitationPromptDto
                    {
                        Id = g.First().Id,
                        PromptText = g.Key!,
                        CitationCount = g.Count(),
                        AveragePosition = g.Where(c => c.Position.HasValue).Any() 
                            ? g.Where(c => c.Position.HasValue).Average(c => c.Position!.Value) 
                            : 0,
                        LastSeen = g.Max(c => c.LastSeenAt ?? c.DiscoveredAt)
                    })
                    .OrderByDescending(p => p.CitationCount)
                    .ToList();

                // Get topics from PromptTracking
                var promptTrackings = await _context.PromptTrackings
                    .Where(pt => pt.ProjectId == projectId && !string.IsNullOrEmpty(pt.Topic))
                    .ToListAsync();

                var topics = citations
                    .Where(c => !string.IsNullOrEmpty(c.PromptText))
                    .Join(promptTrackings,
                        c => c.PromptText,
                        pt => pt.PromptText,
                        (c, pt) => new { Citation = c, Topic = pt.Topic })
                    .Where(x => !string.IsNullOrEmpty(x.Topic))
                    .GroupBy(x => x.Topic)
                    .Select(g => new CitationTopicDto
                    {
                        Topic = g.Key!,
                        CitationCount = g.Count(),
                        Share = totalCitations > 0 ? (g.Count() * 100.0) / totalCitations : 0
                    })
                    .OrderByDescending(t => t.CitationCount)
                    .ToList();

                // Group by platform
                var platforms = citations
                    .GroupBy(c => c.Platform)
                    .Select(g => new CitationPlatformDto
                    {
                        Platform = g.Key,
                        CitationCount = g.Count(),
                        AveragePosition = g.Where(c => c.Position.HasValue).Any() 
                            ? g.Where(c => c.Position.HasValue).Average(c => c.Position!.Value) 
                            : 0,
                        Share = totalCitations > 0 ? (g.Count() * 100.0) / totalCitations : 0
                    })
                    .OrderByDescending(p => p.CitationCount)
                    .ToList();

                // Regions - placeholder for now (would need location data)
                var regions = new List<CitationRegionDto>
                {
                    new CitationRegionDto { Region = "Global", CitationCount = totalCitations, Share = 100 }
                };

                // Share history from aggregations
                var shareHistory = aggregations
                    .Select(a => new CitationShareDataPoint
                    {
                        Date = a.AggregationDate,
                        Share = citationShare,
                        GlobalShare = citationShare
                    })
                    .ToList();

                return new CitationDetailsDto
                {
                    Domain = domain,
                    Rank = rank,
                    CitationShare = citationShare,
                    CitationShareChange = 0, // Calculate from historical data
                    GlobalShare = citationShare,
                    GlobalShareChange = 0,
                    TotalCitations = totalCitations,
                    Pages = pageGroups,
                    Prompts = promptTexts,
                    Topics = topics,
                    Platforms = platforms,
                    Regions = regions,
                    ShareHistory = shareHistory
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting citation details for domain {Domain} in project {ProjectId}", domain, projectId);
                throw;
            }
        }

        private int CalculatePositionPercentage(List<BrandMention> mentions, int position, bool isGreaterThan = false)
        {
            if (!mentions.Any()) return 0;

            var count = isGreaterThan
                ? mentions.Count(m => m.Position >= position)
                : mentions.Count(m => m.Position == position);

            return (int)Math.Round((count * 100.0) / mentions.Count);
        }
    }
}
