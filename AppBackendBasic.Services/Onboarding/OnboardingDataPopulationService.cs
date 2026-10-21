using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Onboarding;

/// <summary>
/// Service to populate user data across multiple tables based on onboarding answers
/// </summary>
public class OnboardingDataPopulationService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly INotificationPreferencesService _notificationPreferencesService;
    private readonly ILogger<OnboardingDataPopulationService> _logger;

    public OnboardingDataPopulationService(
        ApplicationDbContext context,
        IUserRepository userRepository,
        INotificationPreferencesService notificationPreferencesService,
        ILogger<OnboardingDataPopulationService> logger)
    {
        _context = context;
        _userRepository = userRepository;
        _notificationPreferencesService = notificationPreferencesService;
        _logger = logger;
    }

    /// <summary>
    /// Process onboarding answers and populate relevant tables
    /// </summary>
    public async Task ProcessOnboardingAnswersAsync(int userId, string stepKey, List<UserOnboardingAnswer> answers)
    {
        try
        {
            switch (stepKey.ToLower())
            {
                case "basic_info":
                case "profile_setup":
                    await PopulateUserProfileAsync(userId, answers);
                    break;

                case "preferences":
                case "app_preferences":
                    await PopulateUserPreferencesAsync(userId, answers);
                    break;

                case "notifications":
                case "notification_settings":
                    await PopulateNotificationPreferencesAsync(userId, answers);
                    break;

                case "interests":
                case "user_interests":
                    // Can be extended for custom preferences
                    await PopulateCustomPreferencesAsync(userId, answers);
                    break;
            }

            _logger.LogInformation("Processed onboarding answers for user {UserId}, step {StepKey}", userId, stepKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing onboarding answers for user {UserId}, step {StepKey}", userId, stepKey);
            throw;
        }
    }

    /// <summary>
    /// Populate User table with profile information
    /// </summary>
    private async Task PopulateUserProfileAsync(int userId, List<UserOnboardingAnswer> answers)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return;
        }

        var questions = await GetQuestionsForAnswersAsync(answers);
        bool updated = false;

        foreach (var answer in answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.OnboardingQuestionId);
            if (question == null || string.IsNullOrWhiteSpace(answer.AnswerValue))
                continue;

            switch (question.QuestionKey.ToLower())
            {
                case "first_name":
                case "firstname":
                    if (string.IsNullOrEmpty(user.FirstName))
                    {
                        user.FirstName = answer.AnswerValue;
                        updated = true;
                    }
                    break;

                case "last_name":
                case "lastname":
                    if (string.IsNullOrEmpty(user.LastName))
                    {
                        user.LastName = answer.AnswerValue;
                        updated = true;
                    }
                    break;

                case "phone_number":
                case "phone":
                    if (string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        user.PhoneNumber = answer.AnswerValue;
                        updated = true;
                    }
                    break;

                case "date_of_birth":
                case "dob":
                case "birthday":
                    if (!user.DateOfBirth.HasValue && DateTime.TryParse(answer.AnswerValue, out var dob))
                    {
                        user.DateOfBirth = dob;
                        // Auto-verify age if over 13
                        var age = DateTime.UtcNow.Year - dob.Year;
                        if (dob > DateTime.UtcNow.AddYears(-age)) age--;
                        
                        if (age >= 13)
                        {
                            user.AgeVerified = true;
                            user.RequiresParentalConsent = false;
                        }
                        else
                        {
                            user.AgeVerified = false;
                            user.RequiresParentalConsent = true;
                        }
                        updated = true;
                    }
                    break;

                case "gender":
                    if (string.IsNullOrEmpty(user.Gender))
                    {
                        user.Gender = answer.AnswerValue;
                        updated = true;
                    }
                    break;

                case "profile_photo":
                case "avatar":
                    if (string.IsNullOrEmpty(user.ProfilePhotoUrl))
                    {
                        user.ProfilePhotoUrl = answer.AnswerValue;
                        updated = true;
                    }
                    break;
            }
        }

        if (updated)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Updated user profile for {UserId}", userId);
        }
    }

    /// <summary>
    /// Populate UserPreference table
    /// </summary>
    private async Task PopulateUserPreferencesAsync(int userId, List<UserOnboardingAnswer> answers)
    {
        var preference = await _context.Set<UserPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preference == null)
        {
            preference = new UserPreference
            {
                UserId = userId,
                Language = "en",
                Theme = "light",
                Timezone = "UTC",
                CustomPreferences = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Set<UserPreference>().Add(preference);
        }

        var questions = await GetQuestionsForAnswersAsync(answers);
        bool updated = false;

        foreach (var answer in answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.OnboardingQuestionId);
            if (question == null || string.IsNullOrWhiteSpace(answer.AnswerValue))
                continue;

            switch (question.QuestionKey.ToLower())
            {
                case "language":
                case "preferred_language":
                    preference.Language = answer.AnswerValue;
                    updated = true;
                    break;

                case "theme":
                case "app_theme":
                case "dark_mode":
                    // Handle boolean dark_mode or direct theme value
                    if (bool.TryParse(answer.AnswerValue, out var isDark))
                    {
                        preference.Theme = isDark ? "dark" : "light";
                    }
                    else
                    {
                        preference.Theme = answer.AnswerValue;
                    }
                    updated = true;
                    break;

                case "timezone":
                case "time_zone":
                    preference.Timezone = answer.AnswerValue;
                    updated = true;
                    break;
            }
        }

        if (updated)
        {
            preference.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated user preferences for {UserId}", userId);
        }
    }

    /// <summary>
    /// Populate NotificationPreference table
    /// </summary>
    private async Task PopulateNotificationPreferencesAsync(int userId, List<UserOnboardingAnswer> answers)
    {
        var preference = await _context.Set<NotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preference == null)
        {
            // Create default preferences
            preference = await _notificationPreferencesService.CreateDefaultPreferencesAsync(userId);
        }

        var questions = await GetQuestionsForAnswersAsync(answers);
        bool updated = false;

        foreach (var answer in answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.OnboardingQuestionId);
            if (question == null || string.IsNullOrWhiteSpace(answer.AnswerValue))
                continue;

            if (!bool.TryParse(answer.AnswerValue, out var boolValue))
                continue;

            switch (question.QuestionKey.ToLower())
            {
                case "push_notifications":
                case "enable_push":
                    preference.PushEnabled = boolValue;
                    updated = true;
                    break;

                case "email_notifications":
                case "enable_email":
                    preference.EmailEnabled = boolValue;
                    updated = true;
                    break;

                case "sms_notifications":
                case "enable_sms":
                    preference.SmsEnabled = boolValue;
                    updated = true;
                    break;

                case "marketing_notifications":
                case "marketing_emails":
                    preference.MarketingNotifications = boolValue;
                    updated = true;
                    break;

                case "social_notifications":
                    preference.SocialNotifications = boolValue;
                    updated = true;
                    break;

                case "subscription_notifications":
                    preference.SubscriptionNotifications = boolValue;
                    updated = true;
                    break;
            }
        }

        if (updated)
        {
            preference.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated notification preferences for {UserId}", userId);
        }
    }

    /// <summary>
    /// Populate custom preferences in UserPreference.CustomPreferences JSON field
    /// </summary>
    private async Task PopulateCustomPreferencesAsync(int userId, List<UserOnboardingAnswer> answers)
    {
        var preference = await _context.Set<UserPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preference == null)
        {
            preference = new UserPreference
            {
                UserId = userId,
                Language = "en",
                Theme = "light",
                Timezone = "UTC",
                CustomPreferences = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Set<UserPreference>().Add(preference);
        }

        var questions = await GetQuestionsForAnswersAsync(answers);
        var customData = new Dictionary<string, string>();

        // Parse existing custom preferences
        if (!string.IsNullOrEmpty(preference.CustomPreferences) && preference.CustomPreferences != "{}")
        {
            try
            {
                customData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(preference.CustomPreferences) 
                    ?? new Dictionary<string, string>();
            }
            catch
            {
                customData = new Dictionary<string, string>();
            }
        }

        foreach (var answer in answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.OnboardingQuestionId);
            if (question == null || string.IsNullOrWhiteSpace(answer.AnswerValue))
                continue;

            // Store any custom question answers
            customData[question.QuestionKey] = answer.AnswerValue;
        }

        preference.CustomPreferences = System.Text.Json.JsonSerializer.Serialize(customData);
        preference.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated custom preferences for {UserId}", userId);
    }

    /// <summary>
    /// Helper method to get questions for the given answers
    /// </summary>
    private async Task<List<OnboardingQuestion>> GetQuestionsForAnswersAsync(List<UserOnboardingAnswer> answers)
    {
        var questionIds = answers.Select(a => a.OnboardingQuestionId).ToList();
        return await _context.Set<OnboardingQuestion>()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();
    }

    /// <summary>
    /// Create default entries for a new user
    /// NOTE: This method does NOT call SaveChangesAsync - caller must save changes
    /// </summary>
    public async Task CreateDefaultEntriesForUserAsync(int userId)
    {
        try
        {
            // Create UserPreference if not exists
            var userPref = await _context.Set<UserPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (userPref == null)
            {
                userPref = new UserPreference
                {
                    UserId = userId,
                    Language = "en",
                    Theme = "light",
                    Timezone = "UTC",
                    CustomPreferences = "{}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Set<UserPreference>().Add(userPref);
                _logger.LogInformation("Added default UserPreference for {UserId} to context", userId);
            }

            // Create NotificationPreference if not exists
            var notifPref = await _context.Set<NotificationPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (notifPref == null)
            {
                var preferences = new NotificationPreference
                {
                    UserId = userId,
                    PushEnabled = true,
                    EmailEnabled = true,
                    SmsEnabled = false,
                    MarketingNotifications = false,
                    SystemNotifications = true,
                    SocialNotifications = true,
                    SubscriptionNotifications = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Set<NotificationPreference>().Add(preferences);
                _logger.LogInformation("Added default NotificationPreference for {UserId} to context", userId);
            }

            // NOTE: Caller must call SaveChangesAsync to persist these changes
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default entries for user {UserId}", userId);
            throw;
        }
    }
}
