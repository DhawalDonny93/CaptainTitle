using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.LlmTemplates
{
    public class DetailsModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly ApiClient _apiClient;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            AuthService authService,
            ApiClient apiClient,
            ILogger<DetailsModel> logger)
        {
            _authService = authService;
            _apiClient = apiClient;
            _logger = logger;
        }

        public LlmPromptTemplateDto Template { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var template = await _apiClient.GetAsync<LlmPromptTemplateDto>($"/LlmPromptTemplate/{id}");

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
                _logger.LogError(ex, "Error loading template details {Id}", id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToPage("./Index");
            }
        }
    }
}
