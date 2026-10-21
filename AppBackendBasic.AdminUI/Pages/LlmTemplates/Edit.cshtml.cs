using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.LlmTemplates
{
    public class EditModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly ApiClient _apiClient;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            AuthService authService,
            ApiClient apiClient,
            ILogger<EditModel> logger)
        {
            _authService = authService;
            _apiClient = apiClient;
            _logger = logger;
        }

        [BindProperty]
        public UpdateTemplateDto Template { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var template = await _apiClient.GetAsync<UpdateTemplateDto>($"/LlmPromptTemplate/{id}");

                if (template != null)
                {
                    Template = template;
                    return Page();
                }

                TempData["ErrorMessage"] = "Template not found.";
                return RedirectToPage("./Index");
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template {Id}", id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var response = await _apiClient.PutAsync<UpdateTemplateDto, object>(
                    $"/LlmPromptTemplate/{Template.Id}", 
                    Template);

                if (response != null)
                {
                    TempData["SuccessMessage"] = "Template updated successfully!";
                    return RedirectToPage("./Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update template.";
                    return Page();
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {Id}", Template.Id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return Page();
            }
        }
    }

    public class UpdateTemplateDto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string SystemPrompt { get; set; } = string.Empty;

        public string? UserPromptTemplate { get; set; }

        [Required]
        [MaxLength(20)]
        public string ResponseFormat { get; set; } = "json";

        public string? ResponseSchema { get; set; }

        [Required]
        [MaxLength(50)]
        public string Model { get; set; } = "gpt-4o";

        [Range(0, 2)]
        public double Temperature { get; set; } = 0.7;

        [Range(1, 10000)]
        public int MaxTokens { get; set; } = 1000;

        public bool RequiresImage { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "general";

        public bool IsActive { get; set; } = true;
    }
}
