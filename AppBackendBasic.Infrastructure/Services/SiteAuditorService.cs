using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public partial class SiteAuditorService : ISiteAuditorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SiteAuditorService> _logger;

        public SiteAuditorService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<SiteAuditorService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<SiteAuditRun> StartAuditAsync(int projectId, string url, int teamId)
        {
            var auditRun = new SiteAuditRun
            {
                ProjectId = projectId,
                TeamId = teamId,
                Url = url,
                Status = "Running",
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.SiteAuditRuns.Add(auditRun);
            await _context.SaveChangesAsync();

            try
            {
                // Run all checks in parallel
                var checkTasks = new List<Task<AuditCheckResult>>
                {
                    CheckLlmsTxtAsync(url),
                    CheckSchemaMarkupAsync(url),
                    CheckDomClarityAsync(url),
                    CheckContentStructureAsync(url),
                    CheckTechnicalSEOAsync(url)
                };

                var results = await Task.WhenAll(checkTasks);

                // Store results
                var details = new List<SiteAuditDetail>();
                int totalScore = 0;
                int passedChecks = 0;
                int failedChecks = 0;
                int warningChecks = 0;

                foreach (var result in results)
                {
                    var detail = new SiteAuditDetail
                    {
                        SiteAuditRunId = auditRun.Id,
                        CheckName = result.CheckName,
                        CheckType = result.CheckType,
                        Status = result.Status,
                        Score = result.Score,
                        Message = result.Message,
                        DetailsJson = result.Details != null ? JsonSerializer.Serialize(result.Details) : null,
                        Recommendation = result.Recommendation,
                        AutoFixAvailable = result.AutoFixAvailable,
                        Priority = result.Priority,
                        CreatedAt = DateTime.UtcNow
                    };

                    details.Add(detail);
                    totalScore += result.Score;

                    if (result.Status == "Pass") passedChecks++;
                    else if (result.Status == "Fail") failedChecks++;
                    else if (result.Status == "Warning") warningChecks++;
                }

                _context.SiteAuditDetails.AddRange(details);

                // Update audit run
                auditRun.TotalChecks = results.Length;
                auditRun.PassedChecks = passedChecks;
                auditRun.FailedChecks = failedChecks;
                auditRun.WarningChecks = warningChecks;
                auditRun.OverallScore = totalScore / results.Length;
                auditRun.Status = "Completed";
                auditRun.CompletedAt = DateTime.UtcNow;

                // Create history record
                var history = new SiteAuditHistory
                {
                    ProjectId = projectId,
                    CheckDate = DateTime.UtcNow,
                    OverallScore = auditRun.OverallScore,
                    LlmsTxtScore = results.FirstOrDefault(r => r.CheckType == "LlmsTxt")?.Score,
                    SchemaScore = results.FirstOrDefault(r => r.CheckType == "Schema")?.Score,
                    DomClarityScore = results.FirstOrDefault(r => r.CheckType == "DomClarity")?.Score,
                    ContentScore = results.FirstOrDefault(r => r.CheckType == "ContentStructure")?.Score,
                    TechnicalScore = results.FirstOrDefault(r => r.CheckType == "TechnicalSEO")?.Score,
                    TotalIssues = failedChecks + warningChecks,
                    CriticalIssues = failedChecks,
                    WarningIssues = warningChecks,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SiteAuditHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Completed audit for {Url} with score {Score}", url, auditRun.OverallScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running audit for {Url}", url);
                auditRun.Status = "Failed";
                auditRun.ErrorMessage = ex.Message;
                auditRun.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return auditRun;
        }

        public async Task<AuditCheckResult> CheckLlmsTxtAsync(string url)
        {
            var result = new AuditCheckResult
            {
                CheckName = "LLMs.txt File Check",
                CheckType = "LlmsTxt",
                Status = "Fail",
                Score = 0,
                Priority = 5,
                Details = new Dictionary<string, object>()
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                // Try both standard locations
                var urls = new[] { $"{url.TrimEnd('/')}/llms.txt", $"{url.TrimEnd('/')}/.well-known/llms.txt" };
                string? content = null;
                string? foundUrl = null;

                foreach (var testUrl in urls)
                {
                    try
                    {
                        var response = await client.GetAsync(testUrl);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            content = await response.Content.ReadAsStringAsync();
                            foundUrl = testUrl;
                            break;
                        }
                    }
                    catch { }
                }

                if (content == null)
                {
                    result.Message = "LLMs.txt file not found";
                    result.Recommendation = "Create an llms.txt file at the root of your domain or in /.well-known/ directory. This file helps AI systems understand your site structure and content.";
                    result.Details["fileExists"] = false;
                    return result;
                }

                // File exists - start with 40 points
                result.Score = 40;
                result.Details["fileExists"] = true;
                result.Details["fileUrl"] = foundUrl;

                // Check for required sections
                var hasPromptSection = content.Contains("# Prompt", StringComparison.OrdinalIgnoreCase);
                var hasSitemapSection = content.Contains("# Sitemap", StringComparison.OrdinalIgnoreCase);
                var hasPreferredSection = content.Contains("# Preferred", StringComparison.OrdinalIgnoreCase);

                if (hasPromptSection) result.Score += 20;
                if (hasSitemapSection) result.Score += 20;
                if (hasPreferredSection) result.Score += 10;

                result.Details["hasPromptSection"] = hasPromptSection;
                result.Details["hasSitemapSection"] = hasSitemapSection;
                result.Details["hasPreferredSection"] = hasPreferredSection;

                // Check formatting
                var lines = content.Split('\n');
                var isWellFormatted = lines.Any(l => l.StartsWith("#")) && content.Length > 50;
                if (isWellFormatted) result.Score += 10;

                result.Details["isWellFormatted"] = isWellFormatted;
                result.Details["contentLength"] = content.Length;

                // Determine status
                if (result.Score >= 80) result.Status = "Pass";
                else if (result.Score >= 60) result.Status = "Warning";
                else result.Status = "Fail";

                result.Message = $"LLMs.txt file found with score {result.Score}/100";
                
                var recommendations = new List<string>();
                if (!hasPromptSection) recommendations.Add("Add a '# Prompt' section describing your site");
                if (!hasSitemapSection) recommendations.Add("Add a '# Sitemap' section with your sitemap URL");
                if (!hasPreferredSection) recommendations.Add("Consider adding a '# Preferred' section for AI preferences");
                
                result.Recommendation = recommendations.Any() 
                    ? string.Join(". ", recommendations) 
                    : "LLMs.txt file is well-configured";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking LLMs.txt for {Url}", url);
                result.Message = $"Error checking LLMs.txt: {ex.Message}";
                result.Status = "Warning";
            }

            return result;
        }

        public async Task<AuditCheckResult> CheckSchemaMarkupAsync(string url)
        {
            var result = new AuditCheckResult
            {
                CheckName = "Schema Markup Check",
                CheckType = "Schema",
                Status = "Fail",
                Score = 0,
                Priority = 4,
                Details = new Dictionary<string, object>()
            };

            try
            {
                var html = await FetchHtmlAsync(url);
                if (string.IsNullOrEmpty(html))
                {
                    result.Message = "Could not fetch page HTML";
                    return result;
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Find all JSON-LD scripts
                var schemaScripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
                
                if (schemaScripts == null || !schemaScripts.Any())
                {
                    result.Message = "No schema markup found";
                    result.Recommendation = "Add structured data using JSON-LD format to help search engines and AI understand your content better.";
                    result.Details["schemasFound"] = 0;
                    return result;
                }

                var schemas = new List<string>();
                foreach (var script in schemaScripts)
                {
                    try
                    {
                        var jsonContent = script.InnerText;
                        var jsonDoc = JsonDocument.Parse(jsonContent);
                        var type = jsonDoc.RootElement.GetProperty("@type").GetString();
                        if (type != null) schemas.Add(type);
                    }
                    catch { }
                }

                result.Details["schemasFound"] = schemas.Count;
                result.Details["schemaTypes"] = schemas;

                // Score based on schema types
                if (schemas.Contains("Organization")) result.Score += 20;
                if (schemas.Contains("WebSite")) result.Score += 15;
                if (schemas.Any(s => s.Contains("Article") || s.Contains("BlogPosting"))) result.Score += 15;
                if (schemas.Contains("BreadcrumbList")) result.Score += 15;
                if (schemas.Contains("FAQPage")) result.Score += 10;
                
                // Additional schemas
                var additionalSchemas = schemas.Count - 5;
                if (additionalSchemas > 0) result.Score += Math.Min(additionalSchemas * 5, 25);

                result.Score = Math.Min(result.Score, 100);

                if (result.Score >= 70) result.Status = "Pass";
                else if (result.Score >= 40) result.Status = "Warning";
                else result.Status = "Fail";

                result.Message = $"Found {schemas.Count} schema types with score {result.Score}/100";
                
                var recommendations = new List<string>();
                if (!schemas.Contains("Organization")) recommendations.Add("Add Organization schema");
                if (!schemas.Contains("WebSite")) recommendations.Add("Add WebSite schema");
                if (!schemas.Any(s => s.Contains("Article"))) recommendations.Add("Add Article schema for content pages");
                
                result.Recommendation = recommendations.Any() 
                    ? string.Join(". ", recommendations) 
                    : "Schema markup is well-implemented";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schema markup for {Url}", url);
                result.Message = $"Error checking schema: {ex.Message}";
                result.Status = "Warning";
            }

            return result;
        }

        public async Task<AuditCheckResult> CheckDomClarityAsync(string url)
        {
            var result = new AuditCheckResult
            {
                CheckName = "DOM Clarity & Semantic HTML Check",
                CheckType = "DomClarity",
                Status = "Fail",
                Score = 0,
                Priority = 3,
                Details = new Dictionary<string, object>()
            };

            try
            {
                var html = await FetchHtmlAsync(url);
                if (string.IsNullOrEmpty(html))
                {
                    result.Message = "Could not fetch page HTML";
                    return result;
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                int score = 0;

                // Check semantic HTML (25 points)
                var semanticTags = new[] { "header", "nav", "main", "article", "section", "aside", "footer" };
                var foundSemanticTags = semanticTags.Count(tag => doc.DocumentNode.SelectNodes($"//{tag}") != null);
                var semanticScore = (foundSemanticTags * 25) / semanticTags.Length;
                score += semanticScore;
                result.Details["semanticTagsFound"] = foundSemanticTags;
                result.Details["semanticTagsTotal"] = semanticTags.Length;

                // Check heading hierarchy (25 points)
                var h1Tags = doc.DocumentNode.SelectNodes("//h1");
                var h1Count = h1Tags?.Count ?? 0;
                var hasProperH1 = h1Count == 1;
                
                var headings = new List<int>();
                for (int i = 1; i <= 6; i++)
                {
                    var count = doc.DocumentNode.SelectNodes($"//h{i}")?.Count ?? 0;
                    if (count > 0) headings.Add(i);
                }
                
                var hasProperHierarchy = headings.Count > 0 && headings.SequenceEqual(headings.OrderBy(x => x));
                
                if (hasProperH1) score += 15;
                if (hasProperHierarchy) score += 10;
                
                result.Details["h1Count"] = h1Count;
                result.Details["hasProperHeadingHierarchy"] = hasProperHierarchy;

                // Check AI-friendly elements (25 points)
                var metaDescription = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
                var images = doc.DocumentNode.SelectNodes("//img");
                var imagesWithAlt = images?.Count(img => !string.IsNullOrEmpty(img.GetAttributeValue("alt", ""))) ?? 0;
                var totalImages = images?.Count ?? 0;
                
                if (metaDescription != null) score += 10;
                if (totalImages > 0 && imagesWithAlt == totalImages) score += 15;
                else if (totalImages > 0 && imagesWithAlt > totalImages / 2) score += 10;
                
                result.Details["hasMetaDescription"] = metaDescription != null;
                result.Details["imagesWithAlt"] = imagesWithAlt;
                result.Details["totalImages"] = totalImages;

                // Content structure (25 points)
                var paragraphs = doc.DocumentNode.SelectNodes("//p");
                var lists = doc.DocumentNode.SelectNodes("//ul | //ol");
                
                if (paragraphs != null && paragraphs.Count > 3) score += 15;
                if (lists != null && lists.Count > 0) score += 10;
                
                result.Details["paragraphCount"] = paragraphs?.Count ?? 0;
                result.Details["listCount"] = lists?.Count ?? 0;

                result.Score = Math.Min(score, 100);

                if (result.Score >= 70) result.Status = "Pass";
                else if (result.Score >= 50) result.Status = "Warning";
                else result.Status = "Fail";

                result.Message = $"DOM clarity score: {result.Score}/100";
                
                var recommendations = new List<string>();
                if (!hasProperH1) recommendations.Add("Use exactly one H1 tag per page");
                if (!hasProperHierarchy) recommendations.Add("Maintain proper heading hierarchy (H1→H2→H3)");
                if (metaDescription == null) recommendations.Add("Add a meta description");
                if (totalImages > 0 && imagesWithAlt < totalImages) recommendations.Add("Add alt text to all images");
                
                result.Recommendation = recommendations.Any() 
                    ? string.Join(". ", recommendations) 
                    : "DOM structure is clear and semantic";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking DOM clarity for {Url}", url);
                result.Message = $"Error checking DOM: {ex.Message}";
                result.Status = "Warning";
            }

            return result;
        }

        public async Task<AuditCheckResult> CheckContentStructureAsync(string url)
        {
            var result = new AuditCheckResult
            {
                CheckName = "Content Structure & Quality Check",
                CheckType = "ContentStructure",
                Status = "Fail",
                Score = 0,
                Priority = 3,
                Details = new Dictionary<string, object>()
            };

            try
            {
                var html = await FetchHtmlAsync(url);
                if (string.IsNullOrEmpty(html))
                {
                    result.Message = "Could not fetch page HTML";
                    return result;
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Extract main content (remove nav, footer, sidebar)
                var main = doc.DocumentNode.SelectSingleNode("//main") ?? doc.DocumentNode.SelectSingleNode("//article") ?? doc.DocumentNode;
                var textContent = main.InnerText;
                var words = Regex.Matches(textContent, @"\b\w+\b").Count;

                int score = 0;

                // Content length (20 points)
                if (words >= 300) score += 20;
                else if (words >= 150) score += 10;
                
                result.Details["wordCount"] = words;

                // Paragraph structure (20 points)
                var paragraphs = main.SelectNodes(".//p");
                var paragraphCount = paragraphs?.Count ?? 0;
                
                if (paragraphCount >= 5) score += 20;
                else if (paragraphCount >= 3) score += 15;
                else if (paragraphCount >= 1) score += 10;
                
                result.Details["paragraphCount"] = paragraphCount;

                // Content organization (30 points)
                var headings = main.SelectNodes(".//h1 | .//h2 | .//h3 | .//h4 | .//h5 | .//h6");
                var headingCount = headings?.Count ?? 0;
                
                if (headingCount >= 3) score += 30;
                else if (headingCount >= 2) score += 20;
                else if (headingCount >= 1) score += 10;
                
                result.Details["headingCount"] = headingCount;

                // AI-friendly format (30 points)
                var lists = main.SelectNodes(".//ul | .//ol");
                var tables = main.SelectNodes(".//table");
                var hasLists = lists != null && lists.Count > 0;
                var hasTables = tables != null && tables.Count > 0;
                
                if (hasLists) score += 15;
                if (hasTables) score += 15;
                
                result.Details["hasLists"] = hasLists;
                result.Details["hasTables"] = hasTables;

                result.Score = Math.Min(score, 100);

                if (result.Score >= 70) result.Status = "Pass";
                else if (result.Score >= 50) result.Status = "Warning";
                else result.Status = "Fail";

                result.Message = $"Content structure score: {result.Score}/100";
                
                var recommendations = new List<string>();
                if (words < 300) recommendations.Add("Increase content length to at least 300 words");
                if (headingCount < 3) recommendations.Add("Use more subheadings to organize content");
                if (!hasLists) recommendations.Add("Use lists to make content more scannable");
                
                result.Recommendation = recommendations.Any() 
                    ? string.Join(". ", recommendations) 
                    : "Content is well-structured and AI-friendly";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking content structure for {Url}", url);
                result.Message = $"Error checking content: {ex.Message}";
                result.Status = "Warning";
            }

            return result;
        }

        public async Task<AuditCheckResult> CheckTechnicalSEOAsync(string url)
        {
            var result = new AuditCheckResult
            {
                CheckName = "Technical SEO Check",
                CheckType = "TechnicalSEO",
                Status = "Fail",
                Score = 0,
                Priority = 4,
                Details = new Dictionary<string, object>()
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                int score = 0;

                // Check HTTPS (20 points)
                var isHttps = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                if (isHttps) score += 20;
                result.Details["isHttps"] = isHttps;

                // Fetch page
                var startTime = DateTime.UtcNow;
                var response = await client.GetAsync(url);
                var loadTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var html = await response.Content.ReadAsStringAsync();

                // Check response time (20 points)
                if (loadTime < 1000) score += 20;
                else if (loadTime < 2000) score += 15;
                else if (loadTime < 3000) score += 10;
                
                result.Details["loadTimeMs"] = loadTime;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Check mobile viewport (15 points)
                var viewport = doc.DocumentNode.SelectSingleNode("//meta[@name='viewport']");
                if (viewport != null) score += 15;
                result.Details["hasMobileViewport"] = viewport != null;

                // Check title tag (15 points)
                var title = doc.DocumentNode.SelectSingleNode("//title");
                var titleText = title?.InnerText ?? "";
                var titleLength = titleText.Length;
                
                if (titleLength >= 30 && titleLength <= 60) score += 15;
                else if (titleLength > 0 && titleLength < 70) score += 10;
                
                result.Details["titleLength"] = titleLength;
                result.Details["titleText"] = titleText;

                // Check meta description (15 points)
                var metaDesc = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
                var descText = metaDesc?.GetAttributeValue("content", "") ?? "";
                var descLength = descText.Length;
                
                if (descLength >= 120 && descLength <= 160) score += 15;
                else if (descLength > 0 && descLength < 200) score += 10;
                
                result.Details["metaDescriptionLength"] = descLength;

                // Check canonical URL (15 points)
                var canonical = doc.DocumentNode.SelectSingleNode("//link[@rel='canonical']");
                if (canonical != null) score += 15;
                result.Details["hasCanonical"] = canonical != null;

                result.Score = Math.Min(score, 100);

                if (result.Score >= 70) result.Status = "Pass";
                else if (result.Score >= 50) result.Status = "Warning";
                else result.Status = "Fail";

                result.Message = $"Technical SEO score: {result.Score}/100";
                
                var recommendations = new List<string>();
                if (!isHttps) recommendations.Add("Use HTTPS for security");
                if (loadTime > 2000) recommendations.Add("Improve page load speed");
                if (viewport == null) recommendations.Add("Add viewport meta tag for mobile");
                if (titleLength < 30 || titleLength > 60) recommendations.Add("Optimize title tag length (30-60 chars)");
                if (descLength < 120 || descLength > 160) recommendations.Add("Optimize meta description (120-160 chars)");
                
                result.Recommendation = recommendations.Any() 
                    ? string.Join(". ", recommendations) 
                    : "Technical SEO is well-optimized";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking technical SEO for {Url}", url);
                result.Message = $"Error checking technical SEO: {ex.Message}";
                result.Status = "Warning";
            }

            return result;
        }

        public async Task<SiteAuditRun?> GetAuditRunAsync(int runId)
        {
            return await _context.SiteAuditRuns
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == runId);
        }

        public async Task<List<SiteAuditRun>> GetProjectAuditRunsAsync(int projectId, int limit = 10)
        {
            return await _context.SiteAuditRuns
                .Where(r => r.ProjectId == projectId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<SiteAuditRun?> GetLatestAuditAsync(int projectId)
        {
            return await _context.SiteAuditRuns
                .Include(r => r.Details)
                .Where(r => r.ProjectId == projectId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<AuditSummary> GetAuditSummaryAsync(int runId)
        {
            var run = await GetAuditRunAsync(runId);
            if (run == null)
            {
                throw new Exception("Audit run not found");
            }

            var summary = new AuditSummary
            {
                RunId = run.Id,
                Url = run.Url,
                Status = run.Status,
                OverallScore = run.OverallScore,
                TotalChecks = run.TotalChecks,
                PassedChecks = run.PassedChecks,
                FailedChecks = run.FailedChecks,
                WarningChecks = run.WarningChecks,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt
            };

            // Group by category
            var categoryScores = run.Details
                .GroupBy(d => d.CheckType)
                .Select(g => new CategoryScore
                {
                    Category = g.Key,
                    Score = (int)g.Average(d => d.Score),
                    Status = g.Average(d => d.Score) >= 70 ? "Pass" : g.Average(d => d.Score) >= 50 ? "Warning" : "Fail"
                })
                .ToList();

            summary.CategoryScores = categoryScores;

            // Get critical issues and warnings
            summary.CriticalIssues = run.Details
                .Where(d => d.Status == "Fail")
                .Select(d => new AuditCheckResult
                {
                    CheckName = d.CheckName,
                    CheckType = d.CheckType,
                    Status = d.Status,
                    Score = d.Score,
                    Message = d.Message ?? "",
                    Recommendation = d.Recommendation,
                    Priority = d.Priority
                })
                .OrderByDescending(d => d.Priority)
                .ToList();

            summary.Warnings = run.Details
                .Where(d => d.Status == "Warning")
                .Select(d => new AuditCheckResult
                {
                    CheckName = d.CheckName,
                    CheckType = d.CheckType,
                    Status = d.Status,
                    Score = d.Score,
                    Message = d.Message ?? "",
                    Recommendation = d.Recommendation,
                    Priority = d.Priority
                })
                .OrderByDescending(d => d.Priority)
                .ToList();

            return summary;
        }

        public async Task<List<AuditTrend>> GetAuditTrendsAsync(int projectId, DateRangeDto dateRange)
        {
            var history = await _context.SiteAuditHistories
                .Where(h => h.ProjectId == projectId &&
                           h.CheckDate >= dateRange.StartDate &&
                           h.CheckDate <= dateRange.EndDate)
                .OrderBy(h => h.CheckDate)
                .ToListAsync();

            return history.Select(h => new AuditTrend
            {
                Date = h.CheckDate,
                OverallScore = h.OverallScore ?? 0,
                LlmsTxtScore = h.LlmsTxtScore ?? 0,
                SchemaScore = h.SchemaScore ?? 0,
                DomClarityScore = h.DomClarityScore ?? 0,
                ContentScore = h.ContentScore ?? 0,
                TechnicalScore = h.TechnicalScore ?? 0
            }).ToList();
        }

        public async Task<byte[]> ExportAuditReportAsync(int runId, string format)
        {
            var summary = await GetAuditSummaryAsync(runId);
            
            if (format.ToLower() == "json")
            {
                var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
                return Encoding.UTF8.GetBytes(json);
            }
            
            // Default to CSV
            var csv = new StringBuilder();
            csv.AppendLine("Category,Score,Status");
            foreach (var category in summary.CategoryScores)
            {
                csv.AppendLine($"{category.Category},{category.Score},{category.Status}");
            }
            
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<bool> RecheckIssueAsync(int detailId)
        {
            var detail = await _context.SiteAuditDetails
                .Include(d => d.SiteAuditRun)
                .FirstOrDefaultAsync(d => d.Id == detailId);

            if (detail == null) return false;

            // Re-run the specific check
            AuditCheckResult? result = detail.CheckType switch
            {
                "LlmsTxt" => await CheckLlmsTxtAsync(detail.SiteAuditRun.Url),
                "Schema" => await CheckSchemaMarkupAsync(detail.SiteAuditRun.Url),
                "DomClarity" => await CheckDomClarityAsync(detail.SiteAuditRun.Url),
                "ContentStructure" => await CheckContentStructureAsync(detail.SiteAuditRun.Url),
                "TechnicalSEO" => await CheckTechnicalSEOAsync(detail.SiteAuditRun.Url),
                _ => null
            };

            if (result != null)
            {
                detail.Status = result.Status;
                detail.Score = result.Score;
                detail.Message = result.Message;
                detail.DetailsJson = result.Details != null ? JsonSerializer.Serialize(result.Details) : null;
                detail.Recommendation = result.Recommendation;
                
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> MarkIssueFixedAsync(int detailId)
        {
            var detail = await _context.SiteAuditDetails.FindAsync(detailId);
            if (detail == null) return false;

            detail.Status = "Pass";
            detail.Score = 100;
            detail.Message = "Manually marked as fixed";
            
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> FetchHtmlAsync(string url)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                return await client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching HTML from {Url}", url);
                return string.Empty;
            }
        }

        // Multi-Page Crawling Methods
        public async Task<SiteAuditRun> StartCrawlAsync(int projectId, string baseUrl, CrawlOptions options, int teamId)
        {
            var startTime = DateTime.UtcNow;
            var crawlRun = new SiteAuditRun
            {
                ProjectId = projectId,
                TeamId = teamId,
                Url = baseUrl,
                IsCrawl = true,
                Status = "Running",
                MaxPages = options.MaxPages,
                MaxDepth = options.MaxDepth,
                StartedAt = startTime,
                CreatedAt = DateTime.UtcNow
            };

            _context.SiteAuditRuns.Add(crawlRun);
            await _context.SaveChangesAsync();

            try
            {
                var crawledUrls = new HashSet<string>();
                var urlsToProcess = new Queue<(string Url, int Depth)>();
                urlsToProcess.Enqueue((baseUrl, 0));

                var baseUri = new Uri(baseUrl);
                var successCount = 0;
                var failCount = 0;
                var brokenLinks = 0;

                while (urlsToProcess.Count > 0 && crawledUrls.Count < options.MaxPages)
                {
                    var (currentUrl, depth) = urlsToProcess.Dequeue();

                    if (crawledUrls.Contains(currentUrl) || depth > options.MaxDepth)
                        continue;

                    if (options.ExcludePatterns != null && options.ExcludePatterns.Any(pattern => currentUrl.Contains(pattern)))
                        continue;

                    crawledUrls.Add(currentUrl);

                    // Delay between requests
                    if (crawledUrls.Count > 1)
                        await Task.Delay(options.DelayMs);

                    try
                    {
                        var pageData = await CrawlPageAsync(currentUrl, depth);
                        
                        if (pageData.StatusCode == 200)
                        {
                            successCount++;
                            
                            // Store page data
                            var detail = new SiteAuditDetail
                            {
                                SiteAuditRunId = crawlRun.Id,
                                CheckType = "CrawledPage",
                                CheckName = "Page Crawl",
                                Status = "Pass",
                                Score = 100,
                                PageUrl = currentUrl,
                                Depth = depth,
                                StatusCode = pageData.StatusCode,
                                Title = pageData.Title,
                                MetaDescription = pageData.MetaDescription,
                                WordCount = pageData.WordCount,
                                HeadingsJson = JsonSerializer.Serialize(pageData.Headings),
                                OutboundLinksJson = JsonSerializer.Serialize(pageData.OutboundLinks),
                                ImagesJson = JsonSerializer.Serialize(pageData.Images),
                                HasSchema = pageData.HasSchema,
                                HasLlmsTxt = pageData.HasLlmsTxt,
                                ContentHash = pageData.ContentHash,
                                MainContent = pageData.MainContent,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.SiteAuditDetails.Add(detail);

                            // Add internal links to queue
                            if (depth < options.MaxDepth)
                            {
                                foreach (var link in pageData.OutboundLinks)
                                {
                                    if (Uri.TryCreate(link, UriKind.Absolute, out var linkUri))
                                    {
                                        if (options.FollowExternalLinks || linkUri.Host == baseUri.Host)
                                        {
                                            if (!crawledUrls.Contains(link))
                                            {
                                                urlsToProcess.Enqueue((link, depth + 1));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            failCount++;
                            if (pageData.StatusCode >= 400)
                                brokenLinks++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error crawling page {Url}", currentUrl);
                        failCount++;
                    }

                    // Save progress periodically
                    if (crawledUrls.Count % 10 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                // Update crawl run
                var endTime = DateTime.UtcNow;
                crawlRun.Status = "Completed";
                crawlRun.CompletedAt = endTime;
                crawlRun.TotalPages = crawledUrls.Count;
                crawlRun.SuccessfulPages = successCount;
                crawlRun.FailedPages = failCount;
                crawlRun.BrokenLinks = brokenLinks;
                crawlRun.DurationSeconds = (int)(endTime - startTime).TotalSeconds;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Crawl completed for project {ProjectId}. Crawled {Count} pages", 
                    projectId, crawledUrls.Count);

                return crawlRun;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during site crawl for project {ProjectId}", projectId);
                crawlRun.Status = "Failed";
                crawlRun.ErrorMessage = ex.Message;
                crawlRun.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw;
            }
        }

        public async Task<List<SiteAuditDetail>> GetCrawledPagesAsync(int crawlRunId)
        {
            return await _context.SiteAuditDetails
                .Where(d => d.SiteAuditRunId == crawlRunId && d.CheckType == "CrawledPage")
                .OrderBy(d => d.Depth)
                .ThenBy(d => d.PageUrl)
                .ToListAsync();
        }

        public async Task<CrawlSummary> GetCrawlSummaryAsync(int crawlRunId)
        {
            var crawlRun = await _context.SiteAuditRuns.FindAsync(crawlRunId);
            if (crawlRun == null || !crawlRun.IsCrawl)
            {
                throw new Exception("Crawl run not found");
            }

            var pages = await GetCrawledPagesAsync(crawlRunId);

            var statusDistribution = pages
                .Where(p => p.StatusCode.HasValue)
                .GroupBy(p => p.StatusCode!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var pagesWithSchema = pages.Count(p => p.HasSchema == true);
            var pagesWithLlmsTxt = pages.Count(p => p.HasLlmsTxt == true);

            // Find duplicates by content hash
            var duplicates = pages
                .Where(p => !string.IsNullOrEmpty(p.ContentHash))
                .GroupBy(p => p.ContentHash)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);

            return new CrawlSummary
            {
                TotalPages = crawlRun.TotalPages,
                SuccessfulPages = crawlRun.SuccessfulPages,
                FailedPages = crawlRun.FailedPages,
                BrokenLinks = crawlRun.BrokenLinks,
                PagesWithSchema = pagesWithSchema,
                PagesWithLlmsTxt = pagesWithLlmsTxt,
                DuplicatePages = duplicates,
                StatusCodeDistribution = statusDistribution,
                MostCommonIssues = new List<string>()
            };
        }

        private async Task<CrawledPageData> CrawlPageAsync(string url, int depth)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(url);
            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                return new CrawledPageData { StatusCode = statusCode };
            }

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract title
            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? "";

            // Extract meta description
            var metaDesc = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", "") ?? "";

            // Extract headings
            var headings = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //h4 | //h5 | //h6")
                ?.Select(h => new { Tag = h.Name, Text = h.InnerText.Trim() })
                .ToList();

            // Extract links
            var links = doc.DocumentNode.SelectNodes("//a[@href]")
                ?.Select(a => a.GetAttributeValue("href", ""))
                .Where(href => !string.IsNullOrEmpty(href))
                .Select(href => MakeAbsoluteUrl(url, href))
                .Where(href => !string.IsNullOrEmpty(href))
                .Distinct()
                .ToList() ?? new List<string>();

            // Extract images
            var images = doc.DocumentNode.SelectNodes("//img[@src]")
                ?.Select(img => new { 
                    Src = img.GetAttributeValue("src", ""),
                    Alt = img.GetAttributeValue("alt", "")
                })
                .ToList();

            // Check for schema markup
            var hasSchema = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']")?.Any() ?? false;

            // Check for llms.txt reference
            var hasLlmsTxt = html.Contains("llms.txt") || html.Contains("/llms.txt");

            // Extract main content
            var mainContent = doc.DocumentNode.SelectSingleNode("//main")?.InnerText 
                ?? doc.DocumentNode.SelectSingleNode("//article")?.InnerText
                ?? doc.DocumentNode.SelectSingleNode("//body")?.InnerText
                ?? "";
            mainContent = Regex.Replace(mainContent, @"\s+", " ").Trim();
            if (mainContent.Length > 5000)
                mainContent = mainContent.Substring(0, 5000);

            // Word count
            var wordCount = mainContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            // Content hash for duplicate detection
            var contentHash = ComputeHash(mainContent);

            return new CrawledPageData
            {
                StatusCode = statusCode,
                Title = title,
                MetaDescription = metaDesc,
                WordCount = wordCount,
                Headings = headings?.Cast<dynamic>().ToList() ?? new List<dynamic>(),
                OutboundLinks = links,
                Images = images?.Cast<dynamic>().ToList() ?? new List<dynamic>(),
                HasSchema = hasSchema,
                HasLlmsTxt = hasLlmsTxt,
                MainContent = mainContent,
                ContentHash = contentHash
            };
        }

        private string MakeAbsoluteUrl(string baseUrl, string relativeUrl)
        {
            try
            {
                if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out _))
                    return relativeUrl;

                var baseUri = new Uri(baseUrl);
                var absoluteUri = new Uri(baseUri, relativeUrl);
                return absoluteUri.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private string ComputeHash(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private class CrawledPageData
        {
            public int StatusCode { get; set; }
            public string Title { get; set; } = string.Empty;
            public string MetaDescription { get; set; } = string.Empty;
            public int WordCount { get; set; }
            public List<dynamic> Headings { get; set; } = new();
            public List<string> OutboundLinks { get; set; } = new();
            public List<dynamic> Images { get; set; } = new();
            public bool HasSchema { get; set; }
            public bool HasLlmsTxt { get; set; }
            public string MainContent { get; set; } = string.Empty;
            public string ContentHash { get; set; } = string.Empty;
        }
    }
}
