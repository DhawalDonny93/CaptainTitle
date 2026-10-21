namespace AppBackendBasic.Core.DTOs;

/// <summary>
/// Complete app initialization response sent after splash screen
/// Contains all necessary configuration and user state information
/// </summary>
public class AppInitializationDto
{
    /// <summary>
    /// Public system settings (app configuration)
    /// </summary>
    public AppConfigDto AppConfig { get; set; } = new();
    
    /// <summary>
    /// User-specific information (null if not authenticated)
    /// </summary>
    public UserStateDto? UserState { get; set; }
}

/// <summary>
/// Public app configuration settings
/// </summary>
public class AppConfigDto
{
    public string AppName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string MinAppVersion { get; set; } = string.Empty;
    public bool MaintenanceMode { get; set; }
    public bool RegistrationEnabled { get; set; }
    public bool SocialLoginEnabled { get; set; }
    public bool SubscriptionsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool InAppNotificationsEnabled { get; set; }
}

/// <summary>
/// User state and progress information
/// </summary>
public class UserStateDto
{
    /// <summary>
    /// User profile information
    /// </summary>
    public UserProfileSummaryDto Profile { get; set; } = new();
    
    /// <summary>
    /// User preferences (theme, language, timezone)
    /// </summary>
    public UserPreferencesDto Preferences { get; set; } = new();
    
    /// <summary>
    /// Notification preferences
    /// </summary>
    public NotificationPreferencesDto NotificationPreferences { get; set; } = new();
    
    /// <summary>
    /// Onboarding progress and status
    /// </summary>
    public OnboardingSummaryDto Onboarding { get; set; } = new();
    
    /// <summary>
    /// Subscription status and details
    /// </summary>
    public SubscriptionSummaryDto Subscription { get; set; } = new();
    
    /// <summary>
    /// Account status flags
    /// </summary>
    public AccountStatusDto AccountStatus { get; set; } = new();
}

/// <summary>
/// Condensed user profile information
/// </summary>
public class UserProfileSummaryDto
{
    public int UserId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName => !string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName) 
        ? $"{FirstName} {LastName}".Trim() 
        : null;
    public string? ProfilePhotoUrl { get; set; }
    public string? ProfilePhotoThumbnailUrl { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// User UI/UX preferences
/// </summary>
public class UserPreferencesDto
{
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "light";
    public string Timezone { get; set; } = "UTC";
    public string? CustomPreferences { get; set; }
}

/// <summary>
/// Notification preferences summary
/// </summary>
public class NotificationPreferencesDto
{
    public bool PushEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public NotificationCategoriesDto Categories { get; set; } = new();
}

public class NotificationCategoriesDto
{
    public bool Marketing { get; set; }
    public bool System { get; set; }
    public bool Social { get; set; }
    public bool Subscription { get; set; }
}

/// <summary>
/// Onboarding progress summary with full configuration
/// </summary>
public class OnboardingSummaryDto
{
    public bool IsCompleted { get; set; }
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public int RemainingSteps { get; set; }
    public double CompletionPercentage { get; set; }
    public int? NextStepId { get; set; }
    public string? NextStepName { get; set; }
    
    /// <summary>
    /// Full onboarding configuration (steps with questions and media)
    /// </summary>
    public List<OnboardingStepConfigDto> Steps { get; set; } = new();
}

/// <summary>
/// Onboarding step configuration for client
/// </summary>
public class OnboardingStepConfigDto
{
    public int Id { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StepType { get; set; } = "questions"; // "questions", "video", "content"
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; } // "video", "image"
    public string? ContentHtml { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public List<OnboardingQuestionConfigDto> Questions { get; set; } = new();
}

/// <summary>
/// Onboarding question configuration for client
/// </summary>
public class OnboardingQuestionConfigDto
{
    public int Id { get; set; }
    public string QuestionKey { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? ValidationRules { get; set; }
    public string? Options { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Subscription status summary
/// </summary>
public class SubscriptionSummaryDto
{
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public string Status { get; set; } = "none"; // none, active, expired, cancelled
    public string? ProductId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysRemaining { get; set; }
    public bool AutoRenew { get; set; }
    public string? Platform { get; set; }
}

/// <summary>
/// Account status and verification flags
/// </summary>
public class AccountStatusDto
{
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public bool AgeVerified { get; set; }
    public bool RequiresParentalConsent { get; set; }
    public bool ParentalConsentGranted { get; set; }
    public DateTime? LockoutEnd { get; set; }
}
