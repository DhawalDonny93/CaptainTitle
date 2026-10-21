using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages;

public class LoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<LoginModel> _logger;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public LoginModel(AuthService authService, ILogger<LoginModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        // If already authenticated, redirect to dashboard
        if (_authService.IsAuthenticated())
        {
            return RedirectToPage("/Dashboard/Index");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _authService.LoginAsync(Email, Password);

        if (result.Success)
        {
            _logger.LogInformation("User {Email} logged in successfully", Email);
            return RedirectToPage("/Dashboard/Index");
        }

        ErrorMessage = result.ErrorMessage;
        _logger.LogWarning("Failed login attempt for {Email}", Email);
        return Page();
    }
}
