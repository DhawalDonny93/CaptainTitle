using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class PromptResearchService : IPromptResearchService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILlmProviderService _llmService;
        private readonly IPromptTemplateService _promptTemplateService;
        private readonly ILogger<PromptResearchService> _logger;

        public PromptResearchService(
            ApplicationDbContext context,
            ILlmProviderService llmService,
            IPromptTemplateService promptTemplateService,
            ILogger<PromptResearchService> logger)
        {
            _context = context;
            _llmService = llmService;
            _promptTemplateService = promptTemplateService;
            _logger = logger;
        }

        public async Task<List<PromptResearchData>> GeneratePromptResearchAsync(
            int projectId,
            string topic,
            string industry,
            string? audience = null,
            string? businessType = null,
            int count = 20)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    throw new Exception($"Project {projectId} not found");
                }

                // Prepare template variables
                var variables = new Dictionary<string, string>
                {
                    { "TOPIC", topic },
                    { "INDUSTRY", industry },
                    { "AUDIENCE", audience ?? "general audience" },
                    { "BUSINESS_TYPE", businessType ?? "business" },
                    { "BRAND_NAME", project.Name }
                };

                // Get template and replace variables
                var prompt = await _promptTemplateService.GetTemplateWithVariablesAsync("PromptResearchGeneration", variables);

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
                    return new List<PromptResearchData>();
                }

                // Parse JSON response
                var parsedResponse = ParsePromptResearchResponse(llmResponse.ResponseText ?? "");

                if (parsedResponse == null || parsedResponse.Count == 0)
                {
                    _logger.LogWarning("No prompts generated from LLM response");
                    return new List<PromptResearchData>();
                }

                // Save to database
                var researchData = new List<PromptResearchData>();
                foreach (var promptData in parsedResponse.Take(count))
                {
                    var research = new PromptResearchData
                    {
                        ProjectId = projectId,
                        PromptText = promptData.Text,
                        Topic = promptData.Topic,
                        EstimatedVolume = promptData.EstimatedVolume,
                        Difficulty = promptData.Difficulty,
                        RelevanceScore = promptData.RelevanceScore,
                        Status = "New",
                        Source = "LLM Generated",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PromptResearchDatas.Add(research);
                    researchData.Add(research);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated {Count} prompt research entries for project {ProjectId}", 
                    researchData.Count, projectId);

                return researchData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prompt research for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<PromptResearchData>> GetPromptResearchAsync(int projectId, string? topic = null)
        {
            var query = _context.PromptResearchDatas
                .Where(p => p.ProjectId == projectId);

            if (!string.IsNullOrEmpty(topic))
            {
                query = query.Where(p => p.Topic == topic);
            }

            return await query
                .OrderByDescending(p => p.RelevanceScore)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<PromptResearchData> AnalyzePromptRelevanceAsync(int projectId, string promptText)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    throw new Exception($"Project {projectId} not found");
                }

                // For now, create a basic analysis
                // In future, could use LLM to analyze relevance
                var research = new PromptResearchData
                {
                    ProjectId = projectId,
                    PromptText = promptText,
                    Topic = "Uncategorized",
                    EstimatedVolume = "Unknown",
                    Difficulty = "Medium",
                    RelevanceScore = 50, // Default score
                    Status = "New",
                    Source = "Manual",
                    CreatedAt = DateTime.UtcNow
                };

                _context.PromptResearchDatas.Add(research);
                await _context.SaveChangesAsync();

                return research;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing prompt relevance");
                throw;
            }
        }

        public async Task<Dictionary<string, List<PromptResearchData>>> CategorizePromptsAsync(int projectId)
        {
            var prompts = await _context.PromptResearchDatas
                .Where(p => p.ProjectId == projectId)
                .ToListAsync();

            return prompts
                .GroupBy(p => p.Topic ?? "Uncategorized")
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<bool> UpdatePromptStatusAsync(int researchId, string status)
        {
            var research = await _context.PromptResearchDatas.FindAsync(researchId);
            if (research == null)
                return false;

            research.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePromptResearchAsync(int researchId)
        {
            var research = await _context.PromptResearchDatas.FindAsync(researchId);
            if (research == null)
                return false;

            _context.PromptResearchDatas.Remove(research);
            await _context.SaveChangesAsync();
            return true;
        }

        private List<GeneratedPrompt> ParsePromptResearchResponse(string jsonResponse)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var response = JsonSerializer.Deserialize<PromptResearchResponse>(jsonResponse, options);
                return response?.Prompts ?? new List<GeneratedPrompt>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing prompt research JSON: {Response}", jsonResponse);
                return new List<GeneratedPrompt>();
            }
        }

        // Helper classes for JSON parsing
        private class PromptResearchResponse
        {
            public List<GeneratedPrompt> Prompts { get; set; } = new();
        }

        private class GeneratedPrompt
        {
            public string Text { get; set; } = string.Empty;
            public string Topic { get; set; } = string.Empty;
            public string EstimatedVolume { get; set; } = string.Empty;
            public string Difficulty { get; set; } = string.Empty;
            public int RelevanceScore { get; set; }
        }
    }
}
