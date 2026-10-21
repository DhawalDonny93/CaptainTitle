using System.Text.RegularExpressions;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.Templates;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class PromptTemplateService : IPromptTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PromptTemplateService> _logger;

        private const string CacheKeyPrefix = "prompt_template_";
        private const string AllTemplatesCacheKey = "prompt_templates_all";
        private const string CategoryCacheKeyPrefix = "prompt_templates_cat_";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

        public PromptTemplateService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<PromptTemplateService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GetTemplateAsync(string templateName)
        {
            var cacheKey = CacheKeyPrefix + templateName;

            if (_cache.TryGetValue(cacheKey, out string? cachedTemplate) && cachedTemplate != null)
            {
                _logger.LogDebug("PromptTemplate cache hit for {TemplateName}", templateName);
                return cachedTemplate;
            }

            var template = await _context.PromptTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive);

            if (template != null)
            {
                _cache.Set(cacheKey, template.Template, CacheTtl);
                _logger.LogDebug("PromptTemplate loaded from DB for {TemplateName}", templateName);
                return template.Template;
            }

            // Fallback to hardcoded templates
            var hardcodedTemplate = PromptTemplates.GetTemplate(templateName);
            if (!string.IsNullOrEmpty(hardcodedTemplate))
            {
                _logger.LogDebug("Using hardcoded template for {TemplateName}", templateName);
                return hardcodedTemplate;
            }

            _logger.LogWarning("Template not found: {TemplateName}", templateName);
            throw new InvalidOperationException($"Template '{templateName}' not found");
        }

        public async Task<string> GetTemplateWithVariablesAsync(string templateName, Dictionary<string, string> variables)
        {
            var template = await GetTemplateAsync(templateName);
            
            // Increment usage count asynchronously (fire and forget)
            _ = Task.Run(() => IncrementUsageAsync(templateName));

            return ReplaceVariables(template, variables);
        }

        public async Task<List<PromptTemplate>> GetTemplatesByCategoryAsync(string category)
        {
            var cacheKey = CategoryCacheKeyPrefix + category;

            if (_cache.TryGetValue(cacheKey, out List<PromptTemplate>? cached) && cached != null)
                return cached;

            var templates = await _context.PromptTemplates
                .AsNoTracking()
                .Where(t => t.Category == category && t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            _cache.Set(cacheKey, templates, CacheTtl);
            return templates;
        }

        public async Task<List<PromptTemplate>> GetAllActiveTemplatesAsync()
        {
            if (_cache.TryGetValue(AllTemplatesCacheKey, out List<PromptTemplate>? cached) && cached != null)
                return cached;

            var templates = await _context.PromptTemplates
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync();

            _cache.Set(AllTemplatesCacheKey, templates, CacheTtl);
            return templates;
        }

        public async Task<PromptTemplate> UpsertTemplateAsync(PromptTemplate template)
        {
            var existing = await _context.PromptTemplates
                .FirstOrDefaultAsync(t => t.Name == template.Name);

            if (existing == null)
            {
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;
                _context.PromptTemplates.Add(template);
                _logger.LogInformation("Created PromptTemplate: {Name}", template.Name);
            }
            else
            {
                existing.Category = template.Category;
                existing.Template = template.Template;
                existing.VariablesJson = template.VariablesJson;
                existing.Description = template.Description;
                existing.IsActive = template.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                template = existing;
                _logger.LogInformation("Updated PromptTemplate: {Name}", template.Name);
            }

            await _context.SaveChangesAsync();

            // Invalidate caches
            _cache.Remove(CacheKeyPrefix + template.Name);
            _cache.Remove(AllTemplatesCacheKey);
            _cache.Remove(CategoryCacheKeyPrefix + template.Category);

            return template;
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var template = await _context.PromptTemplates.FindAsync(id);
            if (template == null)
                return false;

            _context.PromptTemplates.Remove(template);
            await _context.SaveChangesAsync();

            // Invalidate caches
            _cache.Remove(CacheKeyPrefix + template.Name);
            _cache.Remove(AllTemplatesCacheKey);
            _cache.Remove(CategoryCacheKeyPrefix + template.Category);

            _logger.LogInformation("Deleted PromptTemplate: {Name}", template.Name);
            return true;
        }

        public async Task IncrementUsageAsync(string templateName)
        {
            try
            {
                var template = await _context.PromptTemplates
                    .FirstOrDefaultAsync(t => t.Name == templateName);

                if (template != null)
                {
                    template.UsageCount++;
                    template.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to increment usage for template {TemplateName}", templateName);
            }
        }

        public void InvalidateCache(string? templateName = null)
        {
            if (templateName != null)
            {
                _cache.Remove(CacheKeyPrefix + templateName);
                _logger.LogInformation("Invalidated cache for template: {TemplateName}", templateName);
            }
            else
            {
                _cache.Remove(AllTemplatesCacheKey);
                _logger.LogInformation("Invalidated all template caches");
            }
        }

        public async Task SeedTemplatesAsync()
        {
            var existingCount = await _context.PromptTemplates.CountAsync();
            if (existingCount > 0)
            {
                _logger.LogInformation("PromptTemplates already seeded, skipping");
                return;
            }

            _logger.LogInformation("Seeding PromptTemplates from hardcoded templates");

            var templates = new List<PromptTemplate>
            {
                new PromptTemplate
                {
                    Name = "PromptResearchGeneration",
                    Category = "Research",
                    Template = PromptTemplates.PromptResearchGeneration,
                    Description = "Generate research prompts for a given topic",
                    VariablesJson = "{\"TOPIC\": \"string\", \"INDUSTRY\": \"string\", \"AUDIENCE\": \"string\", \"BUSINESS_TYPE\": \"string\", \"BRAND_NAME\": \"string\"}",
                    IsActive = true,
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PromptTemplate
                {
                    Name = "VisibilityTest",
                    Category = "Tracking",
                    Template = PromptTemplates.VisibilityTest,
                    Description = "Test brand visibility in AI responses",
                    VariablesJson = "{\"PROMPT_TEXT\": \"string\", \"BRAND_NAME\": \"string\", \"BRAND_DOMAIN\": \"string\"}",
                    IsActive = true,
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PromptTemplate
                {
                    Name = "ContentGapAnalysis",
                    Category = "Analysis",
                    Template = PromptTemplates.GetTemplate("ContentGapAnalysis"),
                    Description = "Analyze content gaps and opportunities",
                    VariablesJson = "{\"BRAND_NAME\": \"string\", \"INDUSTRY\": \"string\", \"COMPETITORS\": \"string\", \"CURRENT_CONTENT\": \"string\"}",
                    IsActive = true,
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PromptTemplate
                {
                    Name = "SentimentAnalysis",
                    Category = "Analysis",
                    Template = PromptTemplates.GetTemplate("SentimentAnalysis"),
                    Description = "Analyze sentiment of AI responses",
                    VariablesJson = "{\"RESPONSE_TEXT\": \"string\", \"BRAND_NAME\": \"string\"}",
                    IsActive = true,
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PromptTemplate
                {
                    Name = "CitationExtraction",
                    Category = "Analysis",
                    Template = PromptTemplates.GetTemplate("CitationExtraction"),
                    Description = "Extract citations from AI responses",
                    VariablesJson = "{\"RESPONSE_TEXT\": \"string\"}",
                    IsActive = true,
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.PromptTemplates.AddRange(templates);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} PromptTemplates", templates.Count);
        }

        private static string ReplaceVariables(string template, Dictionary<string, string> variables)
        {
            var result = template;
            foreach (var kvp in variables)
            {
                var placeholder = "{" + kvp.Key + "}";
                result = result.Replace(placeholder, kvp.Value ?? string.Empty);
            }
            return result;
        }
    }
}
