using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.AuditLogs;

public class IndexModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;

    public List<AuditLog> Logs { get; set; } = new();
    public int TotalLogs { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling(TotalLogs / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ActionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UserFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public AuditStats Stats { get; set; } = new();

    public IndexModel(
        AuthService authService,
        ApiClient apiClient,
        ILogger<IndexModel> logger)
    {
        _authService = authService;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        CurrentPage = page;

        try
        {
            // Build query string with filters
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={PageSize}"
            };

            if (!string.IsNullOrEmpty(ActionFilter) && ActionFilter != "all")
                queryParams.Add($"action={ActionFilter}");

            if (!string.IsNullOrEmpty(UserFilter))
                queryParams.Add($"userId={UserFilter}");

            var queryString = string.Join("&", queryParams);
            var response = await _apiClient.GetAsync<AuditLogApiResponse>($"/admin/audit-logs?{queryString}");

            if (response != null && response.Success)
            {
                Logs = response.Logs.Select(l => new AuditLog
                {
                    Action = l.Action,
                    Entity = l.EntityType,
                    UserEmail = l.UserId?.ToString() ?? "System",
                    IpAddress = l.IpAddress,
                    Details = l.Changes ?? string.Empty,
                    Timestamp = l.Timestamp,
                    Success = l.Success
                }).ToList();
                TotalLogs = response.TotalCount;
                Stats = new AuditStats
                {
                    TotalLogs = response.TotalCount,
                    LogsToday = 0,
                    LogsThisWeek = 0,
                    LogsThisMonth = 0,
                    SuccessRate = 0,
                    FailedActions = 0
                };
            }
            else
            {
                Logs = new List<AuditLog>();
                TotalLogs = 0;
                Stats = new AuditStats();
            }

            // Apply client-side filters
            ApplyClientFilters();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit logs");
            Logs = new List<AuditLog>();
            TotalLogs = 0;
            Stats = new AuditStats();
        }

        return Page();
    }

    private void ApplyClientFilters()
    {
        if (!string.IsNullOrEmpty(SearchQuery))
        {
            Logs = Logs.Where(l =>
                l.Action.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                l.UserEmail.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                l.Details.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (StartDate.HasValue)
        {
            Logs = Logs.Where(l => l.Timestamp >= StartDate.Value).ToList();
        }

        if (EndDate.HasValue)
        {
            Logs = Logs.Where(l => l.Timestamp <= EndDate.Value.AddDays(1)).ToList();
        }
    }

}

public class AuditLogResponse
{
    public List<AuditLog> Logs { get; set; } = new();
    public int Total { get; set; }
    public AuditStats Stats { get; set; } = new();
}

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
}

public class AuditStats
{
    public int TotalLogs { get; set; }
    public int LogsToday { get; set; }
    public int LogsThisWeek { get; set; }
    public int LogsThisMonth { get; set; }
    public double SuccessRate { get; set; }
    public int FailedActions { get; set; }
}

public class AuditLogApiResponse
{
    public bool Success { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<AuditLogApiItem> Logs { get; set; } = new();
}

public class AuditLogApiItem
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Changes { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
