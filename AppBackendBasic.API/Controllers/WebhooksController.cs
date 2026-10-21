using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("webhooks")] 
public class WebhooksController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPaywallAnalyticsService _analyticsService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<WebhooksController> _logger;
    private readonly string _revenueCatWebhookAuthToken;

    public WebhooksController(
        ApplicationDbContext db, 
        IPaywallAnalyticsService analyticsService,
        ISubscriptionService subscriptionService,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _db = db;
        _analyticsService = analyticsService;
        _subscriptionService = subscriptionService;
        _logger = logger;
        _revenueCatWebhookAuthToken = configuration["RevenueCat:WebhookAuthToken"] ?? "";
    }

    // Superwall webhook (MVP): accept payload, optionally verify signature later, persist as paywall event when applicable
    [HttpPost("superwall")]
    public async Task<IActionResult> Superwall([FromBody] Dictionary<string, object> body)
    {
        // TODO: Implement X-Superwall-Signature verification to ensure webhook authenticity
        // Action: Retrieve Superwall API key from config, validate HMAC-SHA256 signature of request body
        // Reference: https://docs.superwall.com/webhooks - check signature verification section
        var eventType = body.TryGetValue("event_type", out var et) ? et?.ToString() ?? string.Empty : string.Empty;
        var paywallId = body.TryGetValue("paywall_id", out var pid) ? pid?.ToString() ?? string.Empty : "unknown";
        var deviceId = body.TryGetValue("device_id", out var did) ? did?.ToString() : null;
        var platform = (body.TryGetValue("event_data", out var ed) && ed is not null && ed is System.Text.Json.JsonElement je && je.TryGetProperty("platform", out var plat))
            ? plat.GetString() ?? "unknown"
            : "unknown";

        int? userId = null;
        if (body.TryGetValue("user_id", out var uid) && int.TryParse(uid?.ToString(), out var parsed))
            userId = parsed;

        var dto = new PaywallEventDto
        {
            EventType = string.IsNullOrWhiteSpace(eventType) ? "event" : eventType,
            PaywallId = paywallId,
            DeviceId = deviceId,
            Platform = platform,
            EventData = body
        };
        var id = await _analyticsService.TrackEventAsync(userId, dto);
        return Ok(new { success = true, eventId = id });
    }

    // Apple App Store Server Notifications (MVP): accept payload, TODO: verify JWT and map to subscription updates
    // Currently MVP - logs receipt but doesn't validate or process subscription changes
    [HttpPost("app-store")]
    public async Task<IActionResult> AppStore([FromBody] Dictionary<string, object> payload)
    {
        // TODO: Verify notification JWT with Apple's root certificate and decode signedTransactionInfo
        // Action: Validate X-Apple-Request-Signature header, extract transactionId, update subscription status in database
        // Reference: https://developer.apple.com/documentation/appstoreservernotiifications
        // MVP: no-op persistence for visibility
        await _db.PaywallEvents.AddAsync(new Core.Entities.PaywallEvent
        {
            EventType = "apple_notification",
            PaywallId = "app_store_notifications",
            Platform = "ios",
            EventData = System.Text.Json.JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // Google Play RTDN (MVP): accept payload, TODO: verify message signature and update subscription state
    // Currently MVP - logs receipt but doesn't validate or process subscription changes
    [HttpPost("google-play")]
    public async Task<IActionResult> GooglePlay([FromBody] Dictionary<string, object> payload)
    {
        // TODO: Verify Google Play message signature with public key and decode subscriptionNotification
        // Action: Validate request signature, extract purchase details, update subscription status for affected userId
        // Reference: https://developer.android.com/google/play/billing/rtdn-reference
        await _db.PaywallEvents.AddAsync(new Core.Entities.PaywallEvent
        {
            EventType = "google_notification",
            PaywallId = "google_play_notifications",
            Platform = "android",
            EventData = System.Text.Json.JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
    
    // RevenueCat webhook: handle subscription events from RevenueCat
    [HttpPost("revenuecat")]
    public async Task<IActionResult> RevenueCat(
        [FromBody] RevenueCatWebhookPayload payload,
        [FromHeader(Name = "Authorization")] string authHeader)
    {
        try
        {
            // Validate webhook auth token
            if (!string.IsNullOrEmpty(_revenueCatWebhookAuthToken) && 
                (string.IsNullOrEmpty(authHeader) || authHeader != $"Bearer {_revenueCatWebhookAuthToken}"))
            {
                _logger.LogWarning("Unauthorized RevenueCat webhook attempt");
                return Unauthorized();
            }
            
            _logger.LogInformation($"Received RevenueCat webhook: {payload.Event.Type}");
            
            // Process the webhook
            await _subscriptionService.ProcessRevenueCatWebhookAsync(payload);
            
            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RevenueCat webhook");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}
