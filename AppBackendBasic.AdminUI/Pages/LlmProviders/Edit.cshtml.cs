using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.LlmProviders
{
    public class EditModel : PageModel
    {
        private readonly AuthService _authService;

        public EditModel(AuthService authService)
        {
            _authService = authService;
        }

        public int Id { get; set; }

        public IActionResult OnGet(int id)
        {
            // Check authentication
            if (!_authService.IsAuthenticated())
            {
                return RedirectToPage("/Login");
            }

            Id = id;
            return Page();
        }
    }
}
