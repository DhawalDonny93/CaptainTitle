using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/user/preferences")]
public class PreferencesController : ControllerBase
{
    private readonly INotificationPreferencesService _preferencesService;
    private readonly ILogger<PreferencesController> _logger;

    public PreferencesController(
        INotificationPreferencesService preferencesService,
        ILogger<PreferencesController> logger)
    {
        _preferencesService = preferencesService;
        _logger = logger;
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationPreferences()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var preferences = await _preferencesService.GetPreferencesAsync(userInt);

        if (preferences == null)
        {
            return NotFound(new { message = "Preferences not found" });
        }

        return Ok(new
        {
            success = true,
            preferences = new
            {
                pushEnabled = preferences.PushEnabled,
                emailEnabled = preferences.EmailEnabled,
                smsEnabled = preferences.SmsEnabled,
                categories = new
                {
                    marketing = preferences.MarketingNotifications,
                    system = preferences.SystemNotifications,
                    social = preferences.SocialNotifications,
                    subscription = preferences.SubscriptionNotifications
                }
            }
        });
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] UpdateNotificationPreferencesRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var update = new NotificationPreferencesUpdate
        {
            PushEnabled = request.PushEnabled,
            EmailEnabled = request.EmailEnabled,
            SmsEnabled = request.SmsEnabled,
            Marketing = request.Categories?.Marketing,
            Social = request.Categories?.Social
        };

        var preferences = await _preferencesService.UpdatePreferencesAsync(userInt, update);

        _logger.LogInformation("Notification preferences updated for user {UserId}", userId);

        return Ok(new
        {
            success = true,
            preferences = new
            {
                pushEnabled = preferences.PushEnabled,
                emailEnabled = preferences.EmailEnabled,
                smsEnabled = preferences.SmsEnabled,
                categories = new
                {
                    marketing = preferences.MarketingNotifications,
                    system = preferences.SystemNotifications,
                    social = preferences.SocialNotifications,
                    subscription = preferences.SubscriptionNotifications
                }
            }
        });
    }
}
