using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.LlmTemplates
{
    public class CreateModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly ApiClient _apiClient;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            AuthService authService,
            ApiClient apiClient,
            ILogger<CreateModel> logger)
        {
            _authService = authService;
            _apiClient = apiClient;
            _logger = logger;
        }

        [BindProperty]
        public CreateTemplateDto Template { get; set; } = new CreateTemplateDto
        {
            Model = "gpt-4o",
            Temperature = 0.7,
            MaxTokens = 1000,
            ResponseFormat = "json",
            UserPromptTemplate = "{userInput}",
            IsActive = true
        };

        public IActionResult OnGet()
        {
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }
            return Page();
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
                var response = await _apiClient.PostAsync<CreateTemplateDto, object>(
                    "/LlmPromptTemplate", 
                    Template);

                if (response != null)
                {
                    TempData["SuccessMessage"] = "Template created successfully!";
                    return RedirectToPage("./Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create template.";
                    return Page();
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return Page();
            }
        }
    }

    public class CreateTemplateDto
    {
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
