using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.Onboarding
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly AuthService _authService;

        public string ApiBaseUrl { get; set; } = string.Empty;
        public string JwtToken { get; set; } = string.Empty;

        public IndexModel(IConfiguration configuration, AuthService authService)
        {
            _configuration = configuration;
            _authService = authService;
        }

        public IActionResult OnGet()
        {
            // Check if user is authenticated
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5082";
            
            // Get JWT token from session to pass to client-side
            JwtToken = HttpContext.Session.GetString("JwtToken") ?? string.Empty;
            
            return Page();
        }
    }
}
