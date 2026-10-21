using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.DTOs;

public class OnboardingStepDto
{
    public int Id { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<OnboardingQuestionDto> Questions { get; set; } = new();
}

public class OnboardingQuestionDto
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
    public string? UserAnswer { get; set; }
}

public class SubmitOnboardingAnswersDto
{
    [Required]
    public int OnboardingStepId { get; set; }
    
    [Required]
    public List<OnboardingAnswerDto> Answers { get; set; } = new();
}

public class OnboardingAnswerDto
{
    [Required]
    public int QuestionId { get; set; }
    
    [Required]
    public string AnswerValue { get; set; } = string.Empty;
}

public class OnboardingProgressDto
{
    public bool IsOnboardingCompleted { get; set; }
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public double CompletionPercentage { get; set; }
    public List<OnboardingStepDto> Steps { get; set; } = new();
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public bool OnboardingCompleted { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
