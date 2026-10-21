using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.AdminUI.Pages.Notifications;

public class SendModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ApiClient _apiClient;
    private readonly ILogger<SendModel> _logger;

    [BindProperty]
    public AdminNotificationRequest Notification { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public List<NotificationHistory> RecentNotifications { get; set; } = new();
    public NotificationStats Stats { get; set; } = new();

    public SendModel(
        AuthService authService,
        ApiClient apiClient,
        ILogger<SendModel> logger)
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
            // Load notification history
            RecentNotifications = await LoadNotificationHistory();
            Stats = await LoadNotificationStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notification data");
            RecentNotifications = new List<NotificationHistory>();
            Stats = new NotificationStats();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSendAsync()
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please fill in all required fields";
            RecentNotifications = await LoadNotificationHistory();
            Stats = await LoadNotificationStats();
            return Page();
        }

        try
        {
            // Map AdminUI request to API request format
            if (Notification.IsBroadcast)
            {
                var broadcastRequest = new BroadcastNotificationRequest
                {
                    Title = Notification.Title,
                    Body = Notification.Message,
                    Type = "info",
                    Priority = Notification.Priority == "High" ? 1 : Notification.Priority == "Normal" ? 0 : -1
                };

                var response = await _apiClient.PostAsync<BroadcastNotificationRequest, NotificationResponse>(
                    "/admin/notifications/broadcast",
                    broadcastRequest
                );

                if (response != null && response.Success)
                {
                    SuccessMessage = $"Broadcast sent to all users successfully!";
                    Notification = new AdminNotificationRequest();
                }
                else
                {
                    ErrorMessage = "Failed to send notification";
                }
            }
            else
            {
                // For individual user notifications, get userId from email
                if (string.IsNullOrWhiteSpace(Notification.UserEmail))
                {
                    ErrorMessage = "User email is required for individual notifications";
                    RecentNotifications = await LoadNotificationHistory();
                    Stats = await LoadNotificationStats();
                    return Page();
                }

                try
                {
                    // Get user by email
                    var userResponse = await _apiClient.GetAsync<UserByEmailResponse>(
                        $"/admin/users/by-email/{Uri.EscapeDataString(Notification.UserEmail)}"
                    );

                    if (userResponse == null || !userResponse.Success || userResponse.User == null)
                    {
                        ErrorMessage = $"User with email '{Notification.UserEmail}' not found";
                        RecentNotifications = await LoadNotificationHistory();
                        Stats = await LoadNotificationStats();
                        return Page();
                    }

                    // Send notification to specific user
                    var sendRequest = new SendNotificationRequest
                    {
                        UserId = userResponse.User.Id,
                        Title = Notification.Title,
                        Body = Notification.Message,
                        Type = "info",
                        Priority = Notification.Priority == "High" ? 1 : Notification.Priority == "Normal" ? 0 : -1
                    };

                    var response = await _apiClient.PostAsync<SendNotificationRequest, NotificationResponse>(
                        "/admin/notifications/send-to-user",
                        sendRequest
                    );

                    if (response != null && response.Success)
                    {
                        SuccessMessage = $"Notification sent to {Notification.UserEmail} successfully!";
                        Notification = new AdminNotificationRequest();
                    }
                    else
                    {
                        ErrorMessage = "Failed to send notification";
                    }
                }
                catch (Exception userEx)
                {
                    _logger.LogError(userEx, "Error getting user by email {Email}", Notification.UserEmail);
                    ErrorMessage = $"Error finding user: {userEx.Message}";
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            ErrorMessage = "Failed to send notification. Please try again.";
        }

        RecentNotifications = await LoadNotificationHistory();
        Stats = await LoadNotificationStats();
        return Page();
    }

    private async Task<List<NotificationHistory>> LoadNotificationHistory()
    {
        try
        {
            var response = await _apiClient.GetAsync<NotificationHistoryResponse>("/admin/notifications/history?page=1&pageSize=10");
            return response?.Notifications?.Select(dto => new NotificationHistory
            {
                // Map properties from NotificationHistoryDto to NotificationHistory
                Id = dto.Id,
                Title = dto.Title,
                Message = dto.Message,
                SentAt = dto.SentAt
            }).ToList() ?? new List<NotificationHistory>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notification history");
            return new List<NotificationHistory>();
        }
    }

    private async Task<NotificationStats> LoadNotificationStats()
    {
        try
        {
            var stats = await _apiClient.GetAsync<NotificationStats>("/admin/notifications/stats");
            return stats ?? new NotificationStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notification stats");
            return new NotificationStats();
        }
    }

}

// AdminUI-specific request model (for form binding only)
public class AdminNotificationRequest
{
    public string? UserEmail { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsBroadcast { get; set; }
    public string Priority { get; set; } = "Normal";
}

// All other DTOs are now in AppBackendBasic.Core.DTOs namespace
