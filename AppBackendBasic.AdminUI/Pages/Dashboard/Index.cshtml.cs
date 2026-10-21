using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.Dashboard;

public class IndexModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;

    public DashboardStats? Stats { get; set; }

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
        // Check authentication
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        try
        {
            // Fetch dashboard stats from API
            Stats = await _apiClient.GetAsync<DashboardStats>("/admin/analytics/dashboard-summary");
            
            if (Stats == null)
            {
                Stats = new DashboardStats
                {
                    TotalUsers = 0,
                    ActiveUsers = 0,
                    TotalSubscriptions = 0,
                    ActiveSubscriptions = 0
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard stats");
            Stats = new DashboardStats();
        }

        return Page();
    }
}

public class DashboardStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
    public double ChurnRate { get; set; }
}
