using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.Users;

public class IndexModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;

    public List<UserListItem> Users { get; set; } = new();
    public int TotalUsers { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public IndexModel(
        AuthService authService,
        ApiClient apiClient,
        ILogger<IndexModel> logger)
    {
        _authService = authService;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var response = await _apiClient.GetAsync<UserListResponse>("/admin/users");
            
            if (response != null && response.Users != null)
            {
                Users = response.Users;
                TotalUsers = response.Total;

                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    Users = Users.Where(u => 
                        u.Email.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                        (u.FullName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "all")
                {
                    Users = StatusFilter.ToLower() switch
                    {
                        "active" => Users.Where(u => u.IsActive).ToList(),
                        "inactive" => Users.Where(u => !u.IsActive).ToList(),
                        _ => Users
                    };
                }
            }
            else
            {
                TotalUsers = Users.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
            TotalUsers = Users.Count;
        }

        return Page();
    }

}

public class UserListResponse
{
    public List<UserListItem> Users { get; set; }
    public int Total { get; set; }
}

public class UserListItem
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
}
