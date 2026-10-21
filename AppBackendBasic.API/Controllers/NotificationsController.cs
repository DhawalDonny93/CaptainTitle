using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ApplicationDbContext context,
        IPushNotificationService pushNotificationService,
        ILogger<NotificationsController> logger)
    {
        _context = context;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }
// GET: api/Notifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
        {
            return await _context.Notifications
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        // PUT: api/Notifications/read
        [HttpPut("read")]
        public async Task<IActionResult> MarkAllRead()
        {
            await _context.Notifications
                .Where(n => !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
            return NoContent();
        }
    [HttpPost("register-device")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var success = await _pushNotificationService.RegisterDeviceTokenAsync(
            userInt,
            request.Token,
            request.Platform,
            request.DeviceId,
            request.DeviceModel,
            request.AppVersion
        );

        if (!success)
        {
            return BadRequest(new { message = "Failed to register device token." });
        }

        _logger.LogInformation("Device token registered for user {UserId}", userId);
        
        return Ok(new { success = true, message = "Device token registered successfully." });
    }

    [HttpPost("unregister-device")]
    public async Task<IActionResult> UnregisterDevice([FromBody] UnregisterDeviceRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var success = await _pushNotificationService.UnregisterDeviceTokenAsync(userInt, request.Token);

        if (!success)
        {
            return BadRequest(new { message = "Failed to unregister device token." });
        }

        _logger.LogInformation("Device token unregistered for user {UserId}", userId);
        
        return Ok(new { success = true, message = "Device token unregistered successfully." });
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var message = new PushNotificationMessage
        {
            Title = "Test Notification",
            Body = "This is a test notification from your app!",
            Sound = "default"
        };

        var result = await _pushNotificationService.SendNotificationAsync(userInt, message);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage ?? "Failed to send notification." });
        }

        return Ok(new
        {
            success = true,
            message = "Test notification sent successfully.",
            successCount = result.SuccessCount,
            failureCount = result.FailureCount
        });
    }
}
