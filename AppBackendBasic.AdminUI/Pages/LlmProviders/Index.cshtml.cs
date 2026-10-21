using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.LlmProviders
{
    public class IndexModel : PageModel
    {
        private readonly ApiClient _apiClient;
        private readonly AuthService _authService;

        public IndexModel(
            ApiClient apiClient,
            AuthService authService)
        {
            _apiClient = apiClient;
            _authService = authService;
        }

        public List<LlmProviderViewModel> Providers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var providers = await _apiClient.GetAsync<List<LlmProviderViewModel>>("/admin/llm-providers");
                Providers = providers ?? new List<LlmProviderViewModel>();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching LLM providers: {ex.Message}");
            }

            return Page();
        }

        public class LlmProviderViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string? BaseUrl { get; set; }
            public bool IsActive { get; set; }
            public int Priority { get; set; }
            public int? RateLimitPerMinute { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
    }
}
