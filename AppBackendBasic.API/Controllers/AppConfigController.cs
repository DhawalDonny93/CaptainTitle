using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.API.Controllers;

/// <summary>
/// App configuration and initialization endpoint
/// Called after splash screen to get all necessary app state
/// </summary>
[ApiController]
[Route("api/app")]
public class AppConfigController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly SystemSettingsService _settingsService;
    private readonly IOnboardingService _onboardingService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly INotificationPreferencesService _notificationPreferencesService;
    private readonly ILogger<AppConfigController> _logger;

    public AppConfigController(
        ApplicationDbContext context,
        SystemSettingsService settingsService,
        IOnboardingService onboardingService,
        ISubscriptionService subscriptionService,
        INotificationPreferencesService notificationPreferencesService,
        ILogger<AppConfigController> logger)
    {
        _context = context;
        _settingsService = settingsService;
        _onboardingService = onboardingService;
        _subscriptionService = subscriptionService;
        _notificationPreferencesService = notificationPreferencesService;
        _logger = logger;
    }

    /// <summary>
    /// Get complete app initialization data
    /// Returns app config and user state (if authenticated)
    /// </summary>
    /// <remarks>
    /// This endpoint should be called after the splash screen.
    /// It provides:
    /// - Public app configuration (maintenance mode, feature flags, etc.)
    /// - User profile and preferences (if authenticated)
    /// - Onboarding progress
    /// - Subscription status
    /// - Account status
    /// </remarks>
    [HttpGet("initialize")]
    [AllowAnonymous]
    public async Task<IActionResult> Initialize()
    {
            var response = new AppInitializationDto();
        try
        {

            // 1. Get public app configuration (always returned)
            response.AppConfig = await GetAppConfigAsync();

            // 2. Get user-specific state (only if authenticated)
            var userId = GetUserIdIfAuthenticated();
            if (userId.HasValue)
            {
                response.UserState = await GetUserStateAsync(userId.Value);
            }

            return Ok(new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            if (ex.Message.ToLower() == "user not found")
            {
            _logger.LogError(ex, "Error getting app initialization data : user not found");
                return StatusCode(200, new
                {
                    success = false,
                    data = response,
                    message = "User not found."
                });
            }

            _logger.LogError(ex, "Error getting app initialization data");
            return StatusCode(500, new
            {
                success = false,
data = response,
                message = "Error loading app configuration"
            });
        }
    }
    #region Private Helper Methods

    private async Task<AppConfigDto> GetAppConfigAsync()
    {
        return new AppConfigDto
        {
            AppName = await _settingsService.GetSettingValueAsync(SettingKeys.AppName) ?? "MyApp",
            AppVersion = await _settingsService.GetSettingValueAsync(SettingKeys.AppVersion) ?? "1.0.0",
            MinAppVersion = await _settingsService.GetSettingValueAsync(SettingKeys.MinAppVersion) ?? "1.0.0",
            MaintenanceMode = await _settingsService.GetBoolSettingAsync(SettingKeys.MaintenanceMode, false),
            RegistrationEnabled = await _settingsService.GetBoolSettingAsync(SettingKeys.RegistrationEnabled, true),
            SocialLoginEnabled = await _settingsService.GetBoolSettingAsync(SettingKeys.SocialLoginEnabled, true),
            SubscriptionsEnabled = await _settingsService.GetBoolSettingAsync(SettingKeys.SubscriptionsEnabled, true),
            PushNotificationsEnabled = await _settingsService.GetBoolSettingAsync(SettingKeys.PushNotificationsEnabled, true),
            EmailNotificationsEnabled = await _settingsService.GetBoolSettingAsync(SettingKeys.EmailNotificationsEnabled, true),
            InAppNotificationsEnabled = await _settingsService.GetBoolSettingAsync(SettingKeys.InAppNotificationsEnabled, true)
        };
    }

    private async Task<UserStateDto> GetUserStateAsync(int userId)
    {
        var userState = new UserStateDto();

        // Get user profile
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            throw new Exception("User not found");
        }

        userState.Profile = new UserProfileSummaryDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            ProfilePhotoThumbnailUrl = user.ProfilePhotoThumbnailUrl,
            EmailVerified = user.EmailVerified,
            PhoneVerified = user.PhoneVerified,
            CreatedAt = user.CreatedAt
        };

        // Get user preferences
        var preferences = await _context.Set<UserPreference>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences != null)
        {
            userState.Preferences = new UserPreferencesDto
            {
                Language = preferences.Language,
                Theme = preferences.Theme,
                Timezone = preferences.Timezone,
                CustomPreferences = preferences.CustomPreferences
            };
        }

        // Get notification preferences
        var notificationPrefs = await _notificationPreferencesService.GetPreferencesAsync(userId);
        if (notificationPrefs != null)
        {
            userState.NotificationPreferences = new NotificationPreferencesDto
            {
                PushEnabled = notificationPrefs.PushEnabled,
                EmailEnabled = notificationPrefs.EmailEnabled,
                SmsEnabled = notificationPrefs.SmsEnabled,
                Categories = new NotificationCategoriesDto
                {
                    Marketing = notificationPrefs.MarketingNotifications,
                    System = notificationPrefs.SystemNotifications,
                    Social = notificationPrefs.SocialNotifications,
                    Subscription = notificationPrefs.SubscriptionNotifications
                }
            };
        }

        // Get onboarding progress
        var onboardingProgress = await _onboardingService.GetUserOnboardingProgressAsync(userId);
        var nextStep = onboardingProgress.Steps.FirstOrDefault(s => !s.IsCompleted);
        
        // Get all onboarding steps with questions and media
        var allSteps = await _context.OnboardingSteps
            .Include(s => s.Questions.Where(q => q.IsActive))
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .AsNoTracking()
            .ToListAsync();
        
        userState.Onboarding = new OnboardingSummaryDto
        {
            IsCompleted = onboardingProgress.IsOnboardingCompleted,
            TotalSteps = onboardingProgress.TotalSteps,
            CompletedSteps = onboardingProgress.CompletedSteps,
            RemainingSteps = onboardingProgress.TotalSteps - onboardingProgress.CompletedSteps,
            CompletionPercentage = onboardingProgress.CompletionPercentage,
            NextStepId = nextStep?.Id,
            NextStepName = nextStep?.StepName,
            Steps = allSteps.Select(s => new OnboardingStepConfigDto
            {
                Id = s.Id,
                StepKey = s.StepKey,
                StepName = s.StepName,
                Description = s.Description,
                StepType = s.StepType,
                MediaUrl = s.MediaUrl,
                MediaType = s.MediaType,
                ContentHtml = s.ContentHtml,
                DisplayOrder = s.DisplayOrder,
                IsRequired = s.IsRequired,
                Questions = s.Questions.OrderBy(q => q.DisplayOrder).Select(q => new OnboardingQuestionConfigDto
                {
                    Id = q.Id,
                    QuestionKey = q.QuestionKey,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    ValidationRules = q.ValidationRules,
                    Options = q.Options,
                    Placeholder = q.Placeholder,
                    HelpText = q.HelpText,
                    IsRequired = q.IsRequired,
                    DisplayOrder = q.DisplayOrder
                }).ToList()
            }).ToList()
        };

        // Get subscription status
        var subscriptionStatus = await _subscriptionService.GetStatusAsync(userId);
        var subscription = subscriptionStatus.SubscriptionDetails;
        
        var isExpired = false;
        int? daysRemaining = null;
        
        if (subscription?.ExpiryDate.HasValue == true)
        {
            var expiryDate = subscription.ExpiryDate.Value;
            isExpired = expiryDate <= DateTime.UtcNow;
            daysRemaining = isExpired ? 0 : (int)(expiryDate - DateTime.UtcNow).TotalDays;
        }

        var status = "none";
        if (subscriptionStatus.IsSubscribed)
        {
            status = isExpired ? "expired" : "active";
        }

        userState.Subscription = new SubscriptionSummaryDto
        {
            IsActive = subscriptionStatus.IsSubscribed && !isExpired,
            IsExpired = isExpired,
            Status = status,
            ProductId = subscription?.ProductId,
            ExpiryDate = subscription?.ExpiryDate,
            DaysRemaining = daysRemaining,
            AutoRenew = subscription?.AutoRenew ?? false,
            Platform = subscription?.Platform
        };

        // Get account status
        userState.AccountStatus = new AccountStatusDto
        {
            IsActive = user.IsActive,
            IsLocked = user.IsLockedOut,
            AgeVerified = user.AgeVerified,
            RequiresParentalConsent = user.RequiresParentalConsent,
            ParentalConsentGranted = user.ParentalConsentGranted,
            LockoutEnd = user.LockoutEnd
        };

        return userState;
    }

    private int? GetUserIdOrNull()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
            return null;

        if (int.TryParse(userIdClaim, out var userId))
            return userId;

        return null;
    }

    private int? GetUserIdIfAuthenticated()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return null;

        return GetUserIdOrNull();
    }

    #endregion
}
