using AppBackendBasic.AdminUI.Services;
using AppBackendBasic.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppBackendBasic.AdminUI.Pages.Notifications;

[Authorize]
public class ManageModel : PageModel
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<ManageModel> _logger;

    public List<WebNotificationDto>? Notifications { get; set; }

    public ManageModel(IApiClient apiClient, ILogger<ManageModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync<List<WebNotificationDto>>(
                "api/webnotifications?pageSize=100");
            
            if (response != null)
            {
                Notifications = response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notifications");
            TempData["ErrorMessage"] = "Failed to load notifications";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string Type,
        string? Title,
        string Message,
        bool IsGlobal,
        int? UserId,
        int? DurationMs,
        string? ActionUrl,
        string? ActionText,
        DateTime? ExpiresAt,
        bool IsDismissible = true)
    {
        try
        {
            var dto = new CreateWebNotificationDto
            {
                Type = Type,
                Title = Title ?? string.Empty,
                Body = Message,
                IsGlobal = IsGlobal,
                UserId = IsGlobal ? null : UserId,
                DurationMs = DurationMs ?? 5000,
                ActionUrl = ActionUrl,
                ActionText = ActionText,
                ExpiresAt = ExpiresAt,
                IsDismissible = IsDismissible
            };

            var response = await _apiClient.PostAsync<CreateWebNotificationDto, WebNotificationDto>(
                "api/webnotifications", dto);

            if (response != null)
            {
                TempData["SuccessMessage"] = "Notification created successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to create notification";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            TempData["ErrorMessage"] = "An error occurred while creating the notification";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var response = await _apiClient.DeleteAsync($"api/webnotifications/{id}");

            if (response)
            {
                TempData["SuccessMessage"] = "Notification deleted successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete notification";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            TempData["ErrorMessage"] = "An error occurred while deleting the notification";
        }

        return RedirectToPage();
    }
}
