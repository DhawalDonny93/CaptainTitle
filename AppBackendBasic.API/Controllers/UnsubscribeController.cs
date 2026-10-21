using Microsoft.AspNetCore.Mvc;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/unsubscribe")]
public class UnsubscribeController : ControllerBase
{
    private readonly INotificationPreferencesService _preferencesService;
    private readonly UnsubscribeTokenService _tokenService;
    private readonly ILogger<UnsubscribeController> _logger;

    public UnsubscribeController(
        INotificationPreferencesService preferencesService,
        UnsubscribeTokenService tokenService,
        ILogger<UnsubscribeController> logger)
    {
        _preferencesService = preferencesService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Unsubscribe from all marketing emails (one-click unsubscribe)
    /// </summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> Unsubscribe(string token)
    {
        try
        {
            var (isValid, userId, email) = _tokenService.ValidateUnsubscribeToken(token);

            if (!isValid)
            {
                return Content(GenerateErrorPage("Invalid or Expired Link", 
                    "This unsubscribe link is invalid or has expired. Please use a recent email or contact support."), 
                    "text/html");
            }

            // Get or create preferences
            var preferences = await _preferencesService.GetPreferencesAsync(userId);
            if (preferences == null)
            {
                preferences = await _preferencesService.CreateDefaultPreferencesAsync(userId);
            }

            // Disable marketing emails
            var update = new Core.Interfaces.NotificationPreferencesUpdate
            {
                Marketing = false,
                EmailEnabled = false
            };
            await _preferencesService.UpdatePreferencesAsync(userId, update);

            _logger.LogInformation("User {UserId} ({Email}) unsubscribed from marketing emails", userId, email);

            return Content(GenerateSuccessPage(email), "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing unsubscribe request");
            return Content(GenerateErrorPage("Error", 
                "An error occurred while processing your request. Please try again later."), 
                "text/html");
        }
    }

    /// <summary>
    /// Unsubscribe from specific notification types
    /// </summary>
    [HttpPost("{token}/preferences")]
    public async Task<IActionResult> UpdatePreferences(string token, [FromBody] UnsubscribePreferencesRequest request)
    {
        try
        {
            var (isValid, userId, email) = _tokenService.ValidateUnsubscribeToken(token);

            if (!isValid)
            {
                return BadRequest(new { message = "Invalid or expired token" });
            }

            var preferences = await _preferencesService.GetPreferencesAsync(userId);
            if (preferences == null)
            {
                preferences = await _preferencesService.CreateDefaultPreferencesAsync(userId);
            }

            // Update specific preferences
            var update = new Core.Interfaces.NotificationPreferencesUpdate();
            
            if (request.DisableMarketing.HasValue)
                update.Marketing = !request.DisableMarketing.Value;
            
            if (request.DisableUpdates.HasValue)
                update.Updates = !request.DisableUpdates.Value;
            
            if (request.DisableReminders.HasValue)
                update.Reminders = !request.DisableReminders.Value;
            
            if (request.DisableAllEmails.HasValue && request.DisableAllEmails.Value)
                update.EmailEnabled = false;

            preferences = await _preferencesService.UpdatePreferencesAsync(userId, update);

            _logger.LogInformation("User {UserId} updated email preferences via unsubscribe", userId);

            return Ok(new
            {
                success = true,
                message = "Preferences updated successfully",
                preferences = new
                {
                    preferences.EmailEnabled,
                    preferences.MarketingNotifications,
                    preferences.SystemNotifications,
                    preferences.SocialNotifications
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unsubscribe preferences");
            return StatusCode(500, new { message = "Error updating preferences" });
        }
    }

    /// <summary>
    /// Get unsubscribe preferences page
    /// </summary>
    [HttpGet("{token}/preferences")]
    public async Task<IActionResult> GetPreferencesPage(string token)
    {
        try
        {
            var (isValid, userId, email) = _tokenService.ValidateUnsubscribeToken(token);

            if (!isValid)
            {
                return Content(GenerateErrorPage("Invalid or Expired Link", 
                    "This link is invalid or has expired."), 
                    "text/html");
            }

            var preferences = await _preferencesService.GetPreferencesAsync(userId);
            if (preferences == null)
            {
                preferences = await _preferencesService.CreateDefaultPreferencesAsync(userId);
            }

            return Content(GeneratePreferencesPage(email, token, preferences), "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading preferences page");
            return Content(GenerateErrorPage("Error", 
                "An error occurred while loading your preferences."), 
                "text/html");
        }
    }

    private string GenerateSuccessPage(string email)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Unsubscribed Successfully</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f5f5f5;
            margin: 0;
            padding: 20px;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
        }}
        .container {{
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            padding: 40px;
            max-width: 500px;
            text-align: center;
        }}
        .success-icon {{
            font-size: 64px;
            color: #4CAF50;
            margin-bottom: 20px;
        }}
        h1 {{
            color: #333;
            margin-bottom: 10px;
        }}
        p {{
            color: #666;
            line-height: 1.6;
        }}
        .email {{
            background: #f0f0f0;
            padding: 10px;
            border-radius: 4px;
            margin: 20px 0;
            word-break: break-all;
        }}
        .button {{
            display: inline-block;
            background: #2196F3;
            color: white;
            padding: 12px 24px;
            text-decoration: none;
            border-radius: 4px;
            margin-top: 20px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='success-icon'>✓</div>
        <h1>You've Been Unsubscribed</h1>
        <p>You will no longer receive marketing emails at:</p>
        <div class='email'>{email}</div>
        <p>You can still receive important account-related emails like password resets and security alerts.</p>
        <p>If you change your mind, you can update your preferences in your account settings.</p>
    </div>
</body>
</html>";
    }

    private string GenerateErrorPage(string title, string message)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>{title}</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f5f5f5;
            margin: 0;
            padding: 20px;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
        }}
        .container {{
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            padding: 40px;
            max-width: 500px;
            text-align: center;
        }}
        .error-icon {{
            font-size: 64px;
            color: #f44336;
            margin-bottom: 20px;
        }}
        h1 {{
            color: #333;
            margin-bottom: 10px;
        }}
        p {{
            color: #666;
            line-height: 1.6;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='error-icon'>✕</div>
        <h1>{title}</h1>
        <p>{message}</p>
    </div>
</body>
</html>";
    }

    private string GeneratePreferencesPage(string email, string token, Core.Entities.NotificationPreference preferences)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Email Preferences</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f5f5f5;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            padding: 40px;
            max-width: 600px;
            margin: 0 auto;
        }}
        h1 {{
            color: #333;
            margin-bottom: 10px;
        }}
        .email {{
            background: #f0f0f0;
            padding: 10px;
            border-radius: 4px;
            margin: 20px 0;
            word-break: break-all;
        }}
        .preference {{
            padding: 15px;
            border-bottom: 1px solid #eee;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        .preference:last-child {{
            border-bottom: none;
        }}
        .preference-label {{
            flex: 1;
        }}
        .preference-title {{
            font-weight: 600;
            color: #333;
            margin-bottom: 5px;
        }}
        .preference-description {{
            font-size: 14px;
            color: #666;
        }}
        .toggle {{
            position: relative;
            width: 50px;
            height: 26px;
        }}
        .toggle input {{
            opacity: 0;
            width: 0;
            height: 0;
        }}
        .slider {{
            position: absolute;
            cursor: pointer;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background-color: #ccc;
            transition: .4s;
            border-radius: 26px;
        }}
        .slider:before {{
            position: absolute;
            content: '';
            height: 18px;
            width: 18px;
            left: 4px;
            bottom: 4px;
            background-color: white;
            transition: .4s;
            border-radius: 50%;
        }}
        input:checked + .slider {{
            background-color: #4CAF50;
        }}
        input:checked + .slider:before {{
            transform: translateX(24px);
        }}
        .button {{
            background: #2196F3;
            color: white;
            padding: 12px 24px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
            width: 100%;
            margin-top: 20px;
        }}
        .button:hover {{
            background: #1976D2;
        }}
        #message {{
            padding: 15px;
            border-radius: 4px;
            margin-top: 20px;
            display: none;
        }}
        #message.success {{
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>Email Preferences</h1>
        <p>Manage your email notification preferences for:</p>
        <div class='email'>{email}</div>
        
        <div class='preference'>
            <div class='preference-label'>
                <div class='preference-title'>Marketing Emails</div>
                <div class='preference-description'>Product updates, tips, and special offers</div>
            </div>
            <label class='toggle'>
                <input type='checkbox' id='marketing' {(preferences.MarketingNotifications ? "checked" : "")}>
                <span class='slider'></span>
            </label>
        </div>
        
        <div class='preference'>
            <div class='preference-label'>
                <div class='preference-title'>Reminders</div>
                <div class='preference-description'>Helpful reminders and notifications</div>
            </div>
            <label class='toggle'>
                <input type='checkbox' id='reminders' {(preferences.SubscriptionNotifications ? "checked" : "")}>
                <span class='slider'></span>
            </label>
        </div>
        
        <button class='button' onclick='savePreferences()'>Save Preferences</button>
        
        <div id='message'></div>
    </div>
    
    <script>
        async function savePreferences() {{
            const marketing = document.getElementById('marketing').checked;
            const updates = document.getElementById('updates').checked;
            const reminders = document.getElementById('reminders').checked;
            
            try {{
                const response = await fetch('/api/unsubscribe/{token}/preferences', {{
                    method: 'POST',
                    headers: {{
                        'Content-Type': 'application/json'
                    }},
                    body: JSON.stringify({{
                        disableMarketing: !marketing,
                        disableUpdates: !updates,
                        disableReminders: !reminders
                    }})
                }});
                
                const data = await response.json();
                
                const messageDiv = document.getElementById('message');
                messageDiv.style.display = 'block';
                messageDiv.className = 'success';
                messageDiv.textContent = 'Preferences saved successfully!';
                
                setTimeout(() => {{
                    messageDiv.style.display = 'none';
                }}, 3000);
            }} catch (error) {{
                alert('Error saving preferences. Please try again.');
            }}
        }}
    </script>
";
    }
}
