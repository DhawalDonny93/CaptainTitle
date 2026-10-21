using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppBackendBasic.AdminUI.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // Redirect to login page
        return RedirectToPage("/Login");
    }
}
