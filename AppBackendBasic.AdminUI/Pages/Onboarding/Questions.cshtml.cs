using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.Onboarding
{
    public class QuestionsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly AuthService _authService;

        public string ApiBaseUrl { get; set; } = string.Empty;
        public string JwtToken { get; set; } = string.Empty;
        public string StepId { get; set; } = string.Empty;

        public QuestionsModel(IConfiguration configuration, AuthService authService)
        {
            _configuration = configuration;
            _authService = authService;
        }

        public IActionResult OnGet(string stepId)
        {
            // Check if user is authenticated
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(stepId))
            {
                return RedirectToPage("/Onboarding/Index");
            }

            ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5082";
            JwtToken = HttpContext.Session.GetString("JwtToken") ?? string.Empty;
            StepId = stepId;
            
            return Page();
        }
    }
}
