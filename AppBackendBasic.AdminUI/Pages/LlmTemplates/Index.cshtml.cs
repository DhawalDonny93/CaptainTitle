using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.LlmTemplates
{
    public class IndexModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly ApiClient _apiClient;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            AuthService authService,
            ApiClient apiClient,
            ILogger<IndexModel> logger)
        {
            _authService = authService;
            _apiClient = apiClient;
            _logger = logger;
        }

        public List<LlmPromptTemplateDto> Templates { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public bool? IsActiveFilter { get; set; }

        public async Task<IActionResult> OnGetAsync(string? category, bool? isActive)
        {
            // Check authentication
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            SelectedCategory = category;
            IsActiveFilter = isActive;

            try
            {
                // Fetch all templates
                var allTemplates = await _apiClient.GetAsync<List<LlmPromptTemplateDto>>("/LlmPromptTemplate");
                
                if (allTemplates != null)
                {
                    // Apply filters
                    Templates = allTemplates;

                    if (!string.IsNullOrEmpty(category))
                    {
                        Templates = Templates.Where(t => t.Category == category).ToList();
                    }

                    if (isActive.HasValue)
                    {
                        Templates = Templates.Where(t => t.IsActive == isActive.Value).ToList();
                    }

                    // Get unique categories
                    Categories = allTemplates.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to fetch templates from API.";
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading LLM templates");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
        {
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var success = await _apiClient.DeleteAsync($"/LlmPromptTemplate/{id}");

                if (success)
                {
                    TempData["SuccessMessage"] = "Template deactivated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to deactivate template.";
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating template {Id}", id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateAsync(Guid id)
        {
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            try
            {
                // Get template first
                var template = await _apiClient.GetAsync<LlmPromptTemplateDto>($"/LlmPromptTemplate/{id}");
                
                if (template != null)
                {
                    template.IsActive = true;
                    
                    var response = await _apiClient.PutAsync<LlmPromptTemplateDto, object>(
                        $"/LlmPromptTemplate/{id}", 
                        template);

                    if (response != null)
                    {
                        TempData["SuccessMessage"] = "Template activated successfully.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to activate template.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to fetch template.";
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating template {Id}", id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToPage();
        }
    }

    public class LlmPromptTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? UserPromptTemplate { get; set; }
        public string ResponseFormat { get; set; } = "json";
        public string? ResponseSchema { get; set; }
        public string Model { get; set; } = "gpt-4o";
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 1000;
        public bool RequiresImage { get; set; }
        public bool IsActive { get; set; } = true;
        public string Category { get; set; } = "general";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
