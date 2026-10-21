using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Onboarding;

/// <summary>
/// Service to seed onboarding steps and questions
/// </summary>
public class OnboardingSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OnboardingSeedService> _logger;

    public OnboardingSeedService(
        ApplicationDbContext context,
        ILogger<OnboardingSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed onboarding steps and questions
    /// </summary>
    public async Task SeedOnboardingDataAsync()
    {
        try
        {
            // Check if onboarding data already exists
            var existingSteps = await _context.OnboardingSteps.AnyAsync();
            if (existingSteps)
            {
                _logger.LogInformation("Onboarding data already exists, skipping seed");
                return;
            }

            // Step 1: Basic Profile Information
            var basicInfoStep = new OnboardingStep
            {
                StepKey = "basic_info",
                StepName = "Basic Information",
                Description = "Tell us about yourself",
                DisplayOrder = 1,
                IsRequired = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            basicInfoStep.Questions = new List<OnboardingQuestion>
            {
                new OnboardingQuestion
                {
                    QuestionKey = "first_name",
                    QuestionText = "What's your first name?",
                    QuestionType = "text",
                    Placeholder = "Enter your first name",
                    HelpText = "This will be displayed on your profile",
                    IsRequired = true,
                    DisplayOrder = 1,
                    ValidationRules = "{\"minLength\": 1, \"maxLength\": 30}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new OnboardingQuestion
                {
                    QuestionKey = "last_name",
                    QuestionText = "What's your last name?",
                    QuestionType = "text",
                    Placeholder = "Enter your last name",
                    HelpText = "This will be displayed on your profile",
                    IsRequired = true,
                    DisplayOrder = 2,
                    ValidationRules = "{\"minLength\": 1, \"maxLength\": 30}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new OnboardingQuestion
                {
                    QuestionKey = "date_of_birth",
                    QuestionText = "When's your birthday?",
                    QuestionType = "date",
                    Placeholder = "Select your date of birth",
                    HelpText = "We need this to verify your age (13+)",
                    IsRequired = true,
                    DisplayOrder = 3,
                    ValidationRules = "{\"minAge\": 13}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new OnboardingQuestion
                {
                    QuestionKey = "phone_number",
                    QuestionText = "What's your phone number?",
                    QuestionType = "phone",
                    Placeholder = "+1 (555) 000-0000",
                    HelpText = "Optional - for account recovery and notifications",
                    IsRequired = false,
                    DisplayOrder = 4,
                    ValidationRules = "{\"pattern\": \"^[+]?[0-9]{10,15}$\"}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.OnboardingSteps.Add(basicInfoStep);

            // Step 2: App Preferences
            var preferencesStep = new OnboardingStep
            {
                StepKey = "app_preferences",
                StepName = "App Preferences",
                Description = "Customize your experience",
                DisplayOrder = 2,
                IsRequired = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            preferencesStep.Questions = new List<OnboardingQuestion>
            {
                new OnboardingQuestion
                {
                    QuestionKey = "language",
                    QuestionText = "Preferred language",
                    QuestionType = "select",
                    Placeholder = "Select your language",
                    HelpText = "Choose your preferred app language",
                    IsRequired = false,
                    DisplayOrder = 1,
                    Options = "[{\"value\":\"en\",\"label\":\"English\"},{\"value\":\"es\",\"label\":\"Español\"},{\"value\":\"fr\",\"label\":\"Français\"},{\"value\":\"de\",\"label\":\"Deutsch\"}]",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new OnboardingQuestion
                {
                    QuestionKey = "theme",
                    QuestionText = "Choose your theme",
                    QuestionType = "select",
                    Placeholder = "Select theme",
                    HelpText = "Select light or dark mode",
                    IsRequired = false,
                    DisplayOrder = 2,
                    Options = "[{\"value\":\"light\",\"label\":\"Light\"},{\"value\":\"dark\",\"label\":\"Dark\"},{\"value\":\"auto\",\"label\":\"Auto (System)\"}]",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new OnboardingQuestion
                {
                    QuestionKey = "timezone",
                    QuestionText = "Your timezone",
                    QuestionType = "timezone",
                    Placeholder = "Select your timezone",
                    HelpText = "Used for scheduling and time-based features",
                    IsRequired = false,
                    DisplayOrder = 3,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.OnboardingSteps.Add(preferencesStep);

            // Step 3: Notification Preferences
            var notificationsStep = new OnboardingStep
            {
                StepKey = "notification_settings",
                StepName = "Notification Settings",
                Description = "Choose how you want to be notified",
                DisplayOrder = 3,
                IsRequired = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            notificationsStep.Questions = new List<OnboardingQuestion>
            {
                new OnboardingQuestion
                {
                    QuestionKey = "push_notifications",
                    QuestionText = "Enable push notifications",
                    QuestionType = "boolean",
                    HelpText = "Receive notifications on your device",
                    IsRequired = false,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new OnboardingQuestion
                {
                    QuestionKey = "email_notifications",
                    QuestionText = "Enable email notifications",
                    QuestionType = "boolean",
                    HelpText = "Receive notifications via email",
                    IsRequired = false,
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new OnboardingQuestion
                {
                    QuestionKey = "marketing_notifications",
                    QuestionText = "Receive marketing updates",
                    QuestionType = "boolean",
                    HelpText = "Get updates about new features and promotions",
                    IsRequired = false,
                    DisplayOrder = 3,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.OnboardingSteps.Add(notificationsStep);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Onboarding data seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding onboarding data");
            throw;
        }
    }

    /// <summary>
    /// Clear all onboarding data (use with caution)
    /// </summary>
    public async Task ClearOnboardingDataAsync()
    {
        try
        {
            var answers = await _context.UserOnboardingAnswers.ToListAsync();
            _context.UserOnboardingAnswers.RemoveRange(answers);

            var progress = await _context.UserOnboardingProgress.ToListAsync();
            _context.UserOnboardingProgress.RemoveRange(progress);

            var questions = await _context.Set<OnboardingQuestion>().ToListAsync();
            _context.Set<OnboardingQuestion>().RemoveRange(questions);

            var steps = await _context.OnboardingSteps.ToListAsync();
            _context.OnboardingSteps.RemoveRange(steps);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Onboarding data cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing onboarding data");
            throw;
        }
    }
}
