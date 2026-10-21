using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    /// <summary>
    /// Enhanced LLM crawlability checks for SiteAuditorService
    /// These methods analyze what's missing from a site for optimal LLM indexing
    /// </summary>
    public partial class SiteAuditorService
    {
        /// <summary>
        /// Comprehensive LLM Crawlability Analysis - identifies issues that make sites hard for LLMs to understand
        /// </summary>
        public async Task<AuditCheckResult> CheckLlmCrawlabilityAsync(string url)
        {
            var result = new AuditCheckResult
            {
                CheckName = "LLM Crawlability & Optimization Check",
                CheckType = "LlmCrawlability",
                Status = "Fail",
                Score = 0,
                Priority = 5,
                Details = new Dictionary<string, object>(),
                AutoFixAvailable = false
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

                var issues = new List<string>();
                var recommendations = new List<string>();
                int score = 0;

                // 1. Check for LLMs.txt file (20 points)
                var llmsTxtCheck = await CheckLlmsTxtAsync(url);
                if (llmsTxtCheck.Score >= 80)
                {
                    score += 20;
                }
                else if (llmsTxtCheck.Score >= 60)
                {
                    score += 10;
                    issues.Add("LLMs.txt file exists but needs improvement");
                    recommendations.Add("Enhance your LLMs.txt file with proper sections (Prompt, Sitemap, Preferred)");
                }
                else
                {
                    issues.Add("Missing LLMs.txt file");
                    recommendations.Add("Create /llms.txt file to guide AI crawlers on how to understand your site");
                }
                result.Details["llmsTxtScore"] = llmsTxtCheck.Score;

                // 2. Structured Data Quality (20 points)
                var structuredDataScore = await AnalyzeStructuredDataQuality(doc, issues, recommendations);
                score += structuredDataScore;
                result.Details["structuredDataScore"] = structuredDataScore;

                // 3. Content Clarity & Readability (15 points)
                var contentClarityScore = AnalyzeContentClarity(doc, issues, recommendations);
                score += contentClarityScore;
                result.Details["contentClarityScore"] = contentClarityScore;

                // 4. Semantic HTML & Accessibility (15 points)
                var semanticScore = AnalyzeSemanticStructure(doc, issues, recommendations);
                score += semanticScore;
                result.Details["semanticScore"] = semanticScore;

                // 5. Meta Information Quality (10 points)
                var metaScore = AnalyzeMetaInformation(doc, issues, recommendations);
                score += metaScore;
                result.Details["metaScore"] = metaScore;

                // 6. Content Extractability (10 points)
                var extractabilityScore = AnalyzeContentExtractability(doc, issues, recommendations);
                score += extractabilityScore;
                result.Details["extractabilityScore"] = extractabilityScore;

                // 7. Navigation & Site Structure (10 points)
                var navigationScore = AnalyzeNavigationStructure(doc, issues, recommendations);
                score += navigationScore;
                result.Details["navigationScore"] = navigationScore;

                result.Score = Math.Min(score, 100);
                result.Details["totalIssues"] = issues.Count;
                result.Details["issues"] = issues;

                // Determine status
                if (result.Score >= 80) result.Status = "Pass";
                else if (result.Score >= 60) result.Status = "Warning";
                else result.Status = "Fail";

                result.Message = $"LLM Crawlability Score: {result.Score}/100 - {issues.Count} issues found";
                result.Recommendation = string.Join(" | ", recommendations.Take(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking LLM crawlability for {Url}", url);
                result.Message = $"Error analyzing LLM crawlability: {ex.Message}";
                result.Status = "Warning";
            }

            return result;
        }

        /// <summary>
        /// Analyze structured data quality for LLM understanding
        /// </summary>
        private async Task<int> AnalyzeStructuredDataQuality(HtmlDocument doc, List<string> issues, List<string> recommendations)
        {
            int score = 0;
            var schemaScripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");

            if (schemaScripts == null || !schemaScripts.Any())
            {
                issues.Add("No structured data (Schema.org) found");
                recommendations.Add("Add JSON-LD structured data to help LLMs understand your content context");
                return 0;
            }

            var schemas = new HashSet<string>();
            foreach (var script in schemaScripts)
            {
                try
                {
                    var jsonContent = script.InnerText;
                    var jsonDoc = JsonDocument.Parse(jsonContent);
                    
                    if (jsonDoc.RootElement.TryGetProperty("@type", out var typeElement))
                    {
                        var type = typeElement.GetString();
                        if (type != null) schemas.Add(type);
                    }
                }
                catch { }
            }

            // Essential schemas for LLM understanding
            if (schemas.Contains("Organization") || schemas.Contains("LocalBusiness")) score += 5;
            else
            {
                issues.Add("Missing Organization/Business schema");
                recommendations.Add("Add Organization schema to define your business entity");
            }

            if (schemas.Contains("WebSite")) score += 3;
            if (schemas.Any(s => s.Contains("Article") || s.Contains("BlogPosting"))) score += 4;
            else if (doc.DocumentNode.SelectNodes("//article") != null)
            {
                issues.Add("Articles found but missing Article schema");
                recommendations.Add("Add Article schema to content pages");
            }

            if (schemas.Contains("BreadcrumbList")) score += 3;
            if (schemas.Contains("FAQPage")) score += 3;
            if (schemas.Contains("Product")) score += 2;

            return score;
        }

        /// <summary>
        /// Analyze content clarity and readability for LLMs
        /// </summary>
        private int AnalyzeContentClarity(HtmlDocument doc, List<string> issues, List<string> recommendations)
        {
            int score = 0;

            // Check for clear main content
            var mainContent = doc.DocumentNode.SelectSingleNode("//main") ?? 
                             doc.DocumentNode.SelectSingleNode("//article") ??
                             doc.DocumentNode.SelectSingleNode("//*[@role='main']");

            if (mainContent != null)
            {
                score += 5;
                
                // Analyze content length
                var textContent = mainContent.InnerText.Trim();
                var wordCount = textContent.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                
                if (wordCount >= 300) score += 5;
                else if (wordCount >= 150) score += 3;
                else
                {
                    issues.Add($"Thin content detected ({wordCount} words)");
                    recommendations.Add("Increase content depth to at least 300 words for better LLM understanding");
                }
            }
            else
            {
                issues.Add("No clearly defined main content area");
                recommendations.Add("Use <main> or <article> tags to clearly mark primary content");
            }

            // Check for content structure
            var headings = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3");
            if (headings != null && headings.Count >= 3)
            {
                score += 5;
            }
            else
            {
                issues.Add("Insufficient heading structure");
                recommendations.Add("Use hierarchical headings (H1-H3) to structure content clearly");
            }

            return score;
        }

        /// <summary>
        /// Analyze semantic HTML structure
        /// </summary>
        private int AnalyzeSemanticStructure(HtmlDocument doc, List<string> issues, List<string> recommendations)
        {
            int score = 0;
            var semanticTags = new[] { "header", "nav", "main", "article", "section", "aside", "footer" };
            var foundTags = semanticTags.Count(tag => doc.DocumentNode.SelectNodes($"//{tag}") != null);

            if (foundTags >= 5) score += 10;
            else if (foundTags >= 3) score += 7;
            else if (foundTags >= 1) score += 4;
            else
            {
                issues.Add("Poor semantic HTML structure");
                recommendations.Add("Use HTML5 semantic tags (header, nav, main, article, footer) for better content understanding");
            }

            // Check for proper heading hierarchy
            var h1Tags = doc.DocumentNode.SelectNodes("//h1");
            var h1Count = h1Tags?.Count ?? 0;

            if (h1Count == 1) score += 5;
            else if (h1Count == 0)
            {
                issues.Add("Missing H1 heading");
                recommendations.Add("Add exactly one H1 heading to define the page's main topic");
            }
            else
            {
                issues.Add($"Multiple H1 headings found ({h1Count})");
                recommendations.Add("Use only one H1 heading per page");
            }

            return score;
        }

        /// <summary>
        /// Analyze meta information quality
        /// </summary>
        private int AnalyzeMetaInformation(HtmlDocument doc, List<string> issues, List<string> recommendations)
        {
            int score = 0;

            // Title tag
            var title = doc.DocumentNode.SelectSingleNode("//title");
            if (title != null && !string.IsNullOrWhiteSpace(title.InnerText))
            {
                var titleLength = title.InnerText.Length;
                if (titleLength >= 30 && titleLength <= 60) score += 3;
                else if (titleLength > 0) score += 2;
            }
            else
            {
                issues.Add("Missing or empty title tag");
                recommendations.Add("Add descriptive title tag (30-60 characters)");
            }

            // Meta description
            var metaDesc = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
            if (metaDesc != null)
            {
                var content = metaDesc.GetAttributeValue("content", "");
                if (content.Length >= 120 && content.Length <= 160) score += 4;
                else if (content.Length > 0) score += 2;
            }
            else
            {
                issues.Add("Missing meta description");
                recommendations.Add("Add meta description (120-160 characters) to summarize page content");
            }

            // Open Graph tags
            var ogTags = doc.DocumentNode.SelectNodes("//meta[starts-with(@property, 'og:')]");
            if (ogTags != null && ogTags.Count >= 3) score += 3;

            return score;
        }

        /// <summary>
        /// Analyze how easily content can be extracted by LLMs
        /// </summary>
        private int AnalyzeContentExtractability(HtmlDocument doc, List<string> issues, List<string> recommendations)
        {
            int score = 0;

            // Check for excessive JavaScript dependency
            var scripts = doc.DocumentNode.SelectNodes("//script");
            var scriptCount = scripts?.Count ?? 0;
            
            // Check if main content is in HTML (not just rendered by JS)
            var bodyText = doc.DocumentNode.SelectSingleNode("//body")?.InnerText ?? "";
            var hasSubstantialContent = bodyText.Length > 500;

            if (hasSubstantialContent)
            {
                score += 5;
            }
            else
            {
                issues.Add("Content appears to be JavaScript-rendered");
                recommendations.Add("Ensure primary content is in HTML for better LLM accessibility");
            }

            // Check for clean URLs
            var links = doc.DocumentNode.SelectNodes("//a[@href]");
            if (links != null)
            {
                var cleanUrlCount = links.Count(link => 
                {
                    var href = link.GetAttributeValue("href", "");
                    return !href.Contains("?") || href.Split('?')[0].Length > href.Length * 0.7;
                });
                
                if (links.Count > 0 && cleanUrlCount >= links.Count * 0.8)
                {
                    score += 5;
                }
            }

            return score;
        }

        /// <summary>
        /// Analyze navigation and site structure
        /// </summary>
        private int AnalyzeNavigationStructure(HtmlDocument doc, List<string> issues, List<string> recommendations)
        {
            int score = 0;

            // Check for navigation element
            var nav = doc.DocumentNode.SelectSingleNode("//nav");
            if (nav != null)
            {
                score += 5;
                
                // Check for breadcrumbs
                var breadcrumbs = doc.DocumentNode.SelectNodes("//*[contains(@class, 'breadcrumb')] | //nav[@aria-label='breadcrumb']");
                if (breadcrumbs != null) score += 3;
            }
            else
            {
                issues.Add("No navigation element found");
                recommendations.Add("Add <nav> element to help LLMs understand site structure");
            }

            // Check for sitemap link
            var sitemapLink = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'sitemap')]");
            if (sitemapLink != null) score += 2;

            return score;
        }

        /// <summary>
        /// Detect specific opportunities for LLM optimization
        /// </summary>
        public async Task<List<LlmOptimizationOpportunity>> DetectLlmOptimizationOpportunitiesAsync(string url)
        {
            var opportunities = new List<LlmOptimizationOpportunity>();

            try
            {
                var html = await FetchHtmlAsync(url);
                if (string.IsNullOrEmpty(html)) return opportunities;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Opportunity 1: Missing FAQ schema
                var faqElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'faq')] | //*[contains(@class, 'question')]");
                var hasFaqSchema = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']")
                    ?.Any(s => s.InnerText.Contains("FAQPage")) ?? false;

                if (faqElements != null && !hasFaqSchema)
                {
                    opportunities.Add(new LlmOptimizationOpportunity
                    {
                        Type = "MissingFAQSchema",
                        Title = "Add FAQ Schema Markup",
                        Description = "FAQ content detected but missing FAQPage schema",
                        Impact = "High",
                        Effort = "Low",
                        Priority = 1
                    });
                }

                // Opportunity 2: Missing How-To schema
                var howToIndicators = doc.DocumentNode.SelectNodes("//ol | //ul")
                    ?.Any(list => list.InnerText.ToLower().Contains("step") || list.InnerText.ToLower().Contains("how to")) ?? false;

                if (howToIndicators)
                {
                    opportunities.Add(new LlmOptimizationOpportunity
                    {
                        Type = "MissingHowToSchema",
                        Title = "Add HowTo Schema Markup",
                        Description = "Step-by-step content detected - add HowTo schema",
                        Impact = "High",
                        Effort = "Medium",
                        Priority = 2
                    });
                }

                // Opportunity 3: Improve content structure
                var paragraphs = doc.DocumentNode.SelectNodes("//p");
                if (paragraphs != null && paragraphs.Any(p => p.InnerText.Length > 500))
                {
                    opportunities.Add(new LlmOptimizationOpportunity
                    {
                        Type = "LongParagraphs",
                        Title = "Break Up Long Paragraphs",
                        Description = "Long paragraphs detected - break into smaller chunks for better LLM processing",
                        Impact = "Medium",
                        Effort = "Low",
                        Priority = 3
                    });
                }

                // Opportunity 4: Add table of contents
                var headings = doc.DocumentNode.SelectNodes("//h2 | //h3");
                var hasToc = doc.DocumentNode.SelectNodes("//*[contains(@class, 'toc')] | //*[@id='toc']") != null;

                if (headings != null && headings.Count >= 5 && !hasToc)
                {
                    opportunities.Add(new LlmOptimizationOpportunity
                    {
                        Type = "MissingTableOfContents",
                        Title = "Add Table of Contents",
                        Description = "Multiple sections detected - add TOC for better navigation",
                        Impact = "Medium",
                        Effort = "Low",
                        Priority = 4
                    });
                }

                // Opportunity 5: Enhance images with better alt text
                var images = doc.DocumentNode.SelectNodes("//img");
                if (images != null)
                {
                    var poorAltText = images.Count(img =>
                    {
                        var alt = img.GetAttributeValue("alt", "");
                        return string.IsNullOrWhiteSpace(alt) || alt.Length < 10;
                    });

                    if (poorAltText > images.Count * 0.3)
                    {
                        opportunities.Add(new LlmOptimizationOpportunity
                        {
                            Type = "PoorImageAltText",
                            Title = "Improve Image Alt Text",
                            Description = $"{poorAltText} images have missing or poor alt text",
                            Impact = "Medium",
                            Effort = "Medium",
                            Priority = 5
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting LLM optimization opportunities for {Url}", url);
            }

            return opportunities.OrderBy(o => o.Priority).ToList();
        }
    }

    public class LlmOptimizationOpportunity
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = "Medium"; // High, Medium, Low
        public string Effort { get; set; } = "Medium"; // High, Medium, Low
        public int Priority { get; set; }
    }
}
