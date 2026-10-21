using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.Users;

public class DetailsModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ApiClient _apiClient;
    private readonly ILogger<DetailsModel> _logger;

    public new UserDetails? User { get; set; }
    public List<UserActivity> RecentActivity { get; set; } = new();
    public List<UserSession> ActiveSessions { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public DetailsModel(
        AuthService authService,
        ApiClient apiClient,
        ILogger<DetailsModel> logger)
    {
        _authService = authService;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        try
        {
            // Fetch user details from API
            User = await _apiClient.GetAsync<UserDetails>($"/admin/users/{id}");
            
            if (User == null)
            {
                // Use mock data for demo
                User = GenerateMockUser(id);
            }

            RecentActivity = GenerateMockActivity();
            ActiveSessions = GenerateMockSessions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user details for {UserId}", id);
            User = GenerateMockUser(id);
            RecentActivity = GenerateMockActivity();
            ActiveSessions = GenerateMockSessions();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostToggleAdminAsync(Guid id, bool isAdmin)
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var endpoint = isAdmin 
                ? $"/admin/users/{id}/assign-admin" 
                : $"/admin/users/{id}/remove-admin";
            
            var success = await _apiClient.PostAsync<object, object>(endpoint, new { });

            SuccessMessage = isAdmin 
                ? "Admin role assigned successfully" 
                : "Admin role removed successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling admin role for {UserId}", id);
            ErrorMessage = "Failed to update user role";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id)
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var success = await _apiClient.PostAsync<object, object>($"/admin/users/{id}/toggle-status", new { });
            SuccessMessage = "User status updated successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling status for {UserId}", id);
            ErrorMessage = "Failed to update user status";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var success = await _apiClient.DeleteAsync($"/admin/users/{id}");
            
            if (success)
            {
                TempData["SuccessMessage"] = "User deleted successfully";
                return RedirectToPage("/Users/Index");
            }
            
            ErrorMessage = "Failed to delete user";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            ErrorMessage = "Failed to delete user";
        }

        return RedirectToPage(new { id });
    }

    private UserDetails GenerateMockUser(Guid id)
    {
        return new UserDetails
        {
            Id = (int)id.GetHashCode(),
            Email = "john.doe@example.com",
            FullName = "John Doe",
            PhoneNumber = "+1 234 567 8900",
            IsActive = true,
            IsAdmin = false,
            EmailVerified = true,
            PhoneVerified = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow.AddHours(-2),
            SubscriptionStatus = "Premium",
            SubscriptionExpiresAt = DateTime.UtcNow.AddMonths(1)
        };
    }

    private List<UserActivity> GenerateMockActivity()
    {
        return new List<UserActivity>
        {
            new() { Action = "Logged in", Timestamp = DateTime.UtcNow.AddHours(-2), IpAddress = "192.168.1.1" },
            new() { Action = "Updated profile", Timestamp = DateTime.UtcNow.AddDays(-1), IpAddress = "192.168.1.1" },
            new() { Action = "Changed password", Timestamp = DateTime.UtcNow.AddDays(-5), IpAddress = "192.168.1.1" },
            new() { Action = "Logged in", Timestamp = DateTime.UtcNow.AddDays(-7), IpAddress = "192.168.1.2" },
        };
    }

    private List<UserSession> GenerateMockSessions()
    {
        return new List<UserSession>
        {
            new() { Device = "Chrome on Windows", IpAddress = "192.168.1.1", LastActive = DateTime.UtcNow.AddMinutes(-5), IsCurrentSession = true },
            new() { Device = "Safari on iPhone", IpAddress = "192.168.1.10", LastActive = DateTime.UtcNow.AddHours(-3), IsCurrentSession = false },
        };
    }
}

public class UserDetails
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}

public class UserActivity
{
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

public class UserSession
{
    public string Device { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime LastActive { get; set; }
    public bool IsCurrentSession { get; set; }
}
