using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.LlmProviders
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
        public CreateLlmProviderDto Provider { get; set; } = new CreateLlmProviderDto
        {
            Priority = 1,
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
                var response = await _apiClient.PostAsync<CreateLlmProviderDto, object>(
                    "/admin/llm-providers",
                    Provider);

                if (response != null)
                {
                    TempData["SuccessMessage"] = "LLM Provider created successfully!";
                    return RedirectToPage("./Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create provider.";
                    return Page();
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating LLM provider");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return Page();
            }
        }
    }

    public class CreateLlmProviderDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DisplayName { get; set; }

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? BaseUrl { get; set; }

        [Required]
        [Range(1, 100)]
        public int Priority { get; set; } = 1;

        [Range(1, 10000)]
        public int? RateLimitPerMinute { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
