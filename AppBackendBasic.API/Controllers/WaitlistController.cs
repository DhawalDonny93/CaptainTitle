using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/waitlist")]
public class WaitlistController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WaitlistController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public WaitlistController(
        ApplicationDbContext context,
        ILogger<WaitlistController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// POST /api/waitlist
    /// Accepts email + client metadata, deduplicates, saves to DB, notifies owner via email.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] WaitlistRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });

        if (!IsValidEmail(request.Email))
            return BadRequest(new { message = "Please enter a valid email address." });

        var email = request.Email.Trim().ToLowerInvariant();

        // Deduplicate against DB
        var exists = await _context.WaitlistSignups
            .AnyAsync(w => w.Email == email);

        if (exists)
            return Ok(new { message = "You're already on the list!" });

        var entry = new WaitlistSignup
        {
            Email       = email,
            IpAddress   = GetClientIp(),
            UserAgent   = request.UserAgent,
            Language    = request.Language,
            Timezone    = request.Timezone,
            ScreenResolution = request.ScreenResolution,
            Referrer    = request.Referrer,
            SignedUpAt  = request.Timestamp ?? DateTime.UtcNow,
        };

        _context.WaitlistSignups.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Waitlist signup saved: {Email} from {Ip}", email, entry.IpAddress);

        // Notify owner via SendGrid (fire-and-forget)
        _ = SendOwnerNotificationAsync(entry);

        return Ok(new { message = "You're on the list!" });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch { return false; }
    }

    private async Task SendOwnerNotificationAsync(WaitlistSignup entry)
    {
        try
        {
            var apiKey    = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"] ?? "yadav.dhawal93@gmail.com";
            var ownerEmail = "yadav.dhawal93@gmail.com";

            if (string.IsNullOrEmpty(apiKey)) return;

            var payload = new
            {
                personalizations = new[] { new { to = new[] { new { email = ownerEmail } } } },
                from    = new { email = fromEmail, name = "AuraSEO Waitlist" },
                subject = $"New waitlist signup: {entry.Email}",
                content = new[]
                {
                    new
                    {
                        type = "text/html",
                        value = $@"<h2>New Waitlist Signup</h2>
<table style='border-collapse:collapse;font-family:sans-serif;font-size:14px'>
  <tr><td style='padding:6px 12px;font-weight:bold'>Email</td><td style='padding:6px 12px'>{entry.Email}</td></tr>
  <tr><td style='padding:6px 12px;font-weight:bold'>IP Address</td><td style='padding:6px 12px'>{entry.IpAddress}</td></tr>
  <tr><td style='padding:6px 12px;font-weight:bold'>Timezone</td><td style='padding:6px 12px'>{entry.Timezone}</td></tr>
  <tr><td style='padding:6px 12px;font-weight:bold'>Language</td><td style='padding:6px 12px'>{entry.Language}</td></tr>
  <tr><td style='padding:6px 12px;font-weight:bold'>Screen</td><td style='padding:6px 12px'>{entry.ScreenResolution}</td></tr>
  <tr><td style='padding:6px 12px;font-weight:bold'>Referrer</td><td style='padding:6px 12px'>{entry.Referrer}</td></tr>
  <tr><td style='padding:6px 12px;font-weight:bold'>User Agent</td><td style='padding:6px 12px'>{entry.UserAgent}</td></tr>
  <tr><td style='padding:6px 12px;font-weight:bold'>Time</td><td style='padding:6px 12px'>{entry.SignedUpAt:u}</td></tr>
</table>"
                    }
                }
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.sendgrid.com/v3/mail/send", content);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("SendGrid notification failed: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send waitlist notification email");
        }
    }
}

public class WaitlistRequest
{
    public string Email { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
    public string? ScreenResolution { get; set; }
    public string? Referrer { get; set; }
    public DateTime? Timestamp { get; set; }
}
