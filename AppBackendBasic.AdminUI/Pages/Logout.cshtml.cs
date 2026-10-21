using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages;

public class LogoutModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(AuthService authService, ILogger<LogoutModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userEmail = _authService.GetCurrentUserEmail();
        await _authService.LogoutAsync();
        
        _logger.LogInformation("User {Email} logged out", userEmail);
        
        return RedirectToPage("/Login");
    }
}
