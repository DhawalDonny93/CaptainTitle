using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppBackendBasic.AdminUI.Services;

namespace AppBackendBasic.AdminUI.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;

    [BindProperty]
    public SystemSettings Settings { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

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
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var response = await _apiClient.GetAsync<SettingsApiResponse>("/admin/settings");
            if (response != null && response.Success && response.Settings != null)
            {
                Settings = MapApiSettingsToModel(response.Settings);
            }
            else
            {
                Settings = GenerateMockSettings();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
            Settings = GenerateMockSettings();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please check your input";
            return Page();
        }

        try
        {
            var response = await _apiClient.PostAsync<SystemSettings, UpdateResponse>(
                "/admin/settings/update",
                Settings
            );

            if (response != null && response.Success)
            {
                SuccessMessage = "Settings updated successfully!";
            }
            else
            {
                ErrorMessage = "Failed to update settings";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            ErrorMessage = "Failed to update settings. Please try again.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleFeatureAsync(string feature, bool enabled)
    {
        if (!_authService.IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var response = await _apiClient.PostAsync<object, UpdateResponse>(
                $"/admin/settings/feature/{feature}",
                new { enabled }
            );

            SuccessMessage = $"Feature '{feature}' {(enabled ? "enabled" : "disabled")} successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling feature {Feature}", feature);
            ErrorMessage = $"Failed to toggle feature '{feature}'";
        }

        return RedirectToPage();
    }

    private SystemSettings GenerateMockSettings()
    {
        return new SystemSettings
        {
            // General Settings
            ApplicationName = "BasicFramework",
            ApplicationUrl = "https://yourapp.com",
            SupportEmail = "support@yourapp.com",
            MaintenanceMode = false,

            // Feature Flags
            EnableUserRegistration = true,
            EnableEmailVerification = true,
            EnableTwoFactorAuth = false,
            EnableSocialLogin = true,
            EnableNotifications = true,
            EnableAnalytics = true,

            // Email Settings
            SmtpHost = "smtp.gmail.com",
            SmtpPort = 587,
            SmtpUsername = "noreply@yourapp.com",
            SmtpFromEmail = "noreply@yourapp.com",
            SmtpFromName = "BasicFramework",

            // Security Settings
            PasswordMinLength = 8,
            PasswordRequireUppercase = true,
            PasswordRequireLowercase = true,
            PasswordRequireDigit = true,
            PasswordRequireSpecialChar = true,
            MaxLoginAttempts = 5,
            LockoutDurationMinutes = 30,

            // Rate Limiting
            RateLimitEnabled = true,
            RateLimitRequests = 100,
            RateLimitPeriodSeconds = 60,

            // Session Settings
            SessionTimeoutMinutes = 60,
            RefreshTokenExpiryDays = 30
        };
    }

    private SystemSettings MapApiSettingsToModel(List<SettingItem> apiSettings)
    {
        var settings = new SystemSettings();
        
        foreach (var item in apiSettings)
        {
            switch (item.Key)
            {
                // General Settings
                case "app.name":
                    settings.ApplicationName = item.Value;
                    break;
                case "app.url":
                    settings.ApplicationUrl = item.Value;
                    break;
                case "app.support_email":
                    settings.SupportEmail = item.Value;
                    break;
                    
                // Feature Flags
                case "feature.maintenance_mode":
                    settings.MaintenanceMode = item.Value?.ToLower() == "true";
                    break;
                case "feature.registration_enabled":
                    settings.EnableUserRegistration = item.Value?.ToLower() == "true";
                    break;
                case "feature.email_verification":
                    settings.EnableEmailVerification = item.Value?.ToLower() == "true";
                    break;
                case "feature.two_factor_auth":
                    settings.EnableTwoFactorAuth = item.Value?.ToLower() == "true";
                    break;
                case "feature.social_login":
                    settings.EnableSocialLogin = item.Value?.ToLower() == "true";
                    break;
                case "feature.notifications":
                    settings.EnableNotifications = item.Value?.ToLower() == "true";
                    break;
                case "feature.analytics":
                    settings.EnableAnalytics = item.Value?.ToLower() == "true";
                    break;
                    
                // Email Settings
                case "email.smtp_host":
                    settings.SmtpHost = item.Value;
                    break;
                case "email.smtp_port":
                    if (int.TryParse(item.Value, out var smtpPort))
                        settings.SmtpPort = smtpPort;
                    break;
                case "email.smtp_username":
                    settings.SmtpUsername = item.Value;
                    break;
                case "email.from_email":
                    settings.SmtpFromEmail = item.Value;
                    break;
                case "email.from_name":
                    settings.SmtpFromName = item.Value;
                    break;
                    
                // Security Settings
                case "security.password_min_length":
                    if (int.TryParse(item.Value, out var minLength))
                        settings.PasswordMinLength = minLength;
                    break;
                case "security.password_require_uppercase":
                    settings.PasswordRequireUppercase = item.Value?.ToLower() == "true";
                    break;
                case "security.password_require_lowercase":
                    settings.PasswordRequireLowercase = item.Value?.ToLower() == "true";
                    break;
                case "security.password_require_digit":
                    settings.PasswordRequireDigit = item.Value?.ToLower() == "true";
                    break;
                case "security.password_require_special":
                    settings.PasswordRequireSpecialChar = item.Value?.ToLower() == "true";
                    break;
                case "security.max_login_attempts":
                    if (int.TryParse(item.Value, out var maxAttempts))
                        settings.MaxLoginAttempts = maxAttempts;
                    break;
                case "security.lockout_duration":
                    if (int.TryParse(item.Value, out var lockoutDuration))
                        settings.LockoutDurationMinutes = lockoutDuration;
                    break;
                    
                // Rate Limiting
                case "ratelimit.enabled":
                    settings.RateLimitEnabled = item.Value?.ToLower() == "true";
                    break;
                case "ratelimit.requests":
                    if (int.TryParse(item.Value, out var rateLimitRequests))
                        settings.RateLimitRequests = rateLimitRequests;
                    break;
                case "ratelimit.period_seconds":
                    if (int.TryParse(item.Value, out var rateLimitPeriod))
                        settings.RateLimitPeriodSeconds = rateLimitPeriod;
                    break;
                    
                // Session Settings
                case "session.timeout_minutes":
                    if (int.TryParse(item.Value, out var sessionTimeout))
                        settings.SessionTimeoutMinutes = sessionTimeout;
                    break;
                case "session.refresh_token_expiry_days":
                    if (int.TryParse(item.Value, out var refreshTokenExpiry))
                        settings.RefreshTokenExpiryDays = refreshTokenExpiry;
                    break;
            }
        }
        
        return settings;
    }
}

public class SystemSettings
{
    // General Settings
    public string ApplicationName { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public bool MaintenanceMode { get; set; }

    // Feature Flags
    public bool EnableUserRegistration { get; set; }
    public bool EnableEmailVerification { get; set; }
    public bool EnableTwoFactorAuth { get; set; }
    public bool EnableSocialLogin { get; set; }
    public bool EnableNotifications { get; set; }
    public bool EnableAnalytics { get; set; }

    // Email Settings
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpFromEmail { get; set; } = string.Empty;
    public string SmtpFromName { get; set; } = string.Empty;

    // Security Settings
    public int PasswordMinLength { get; set; }
    public bool PasswordRequireUppercase { get; set; }
    public bool PasswordRequireLowercase { get; set; }
    public bool PasswordRequireDigit { get; set; }
    public bool PasswordRequireSpecialChar { get; set; }
    public int MaxLoginAttempts { get; set; }
    public int LockoutDurationMinutes { get; set; }

    // Rate Limiting
    public bool RateLimitEnabled { get; set; }
    public int RateLimitRequests { get; set; }
    public int RateLimitPeriodSeconds { get; set; }

    // Session Settings
    public int SessionTimeoutMinutes { get; set; }
    public int RefreshTokenExpiryDays { get; set; }
}

public class UpdateResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SettingsApiResponse
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<SettingItem> Settings { get; set; } = new();
}

public class SettingItem
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
