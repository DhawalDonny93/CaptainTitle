using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.DTOs;

/// <summary>
/// DTO for creating a new onboarding step
/// </summary>
public class CreateOnboardingStepDto
{
    [Required]
    [MaxLength(100)]
    public string StepKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string StepName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string StepType { get; set; } = "questions"; // "questions", "video", "content"
    
    [MaxLength(1000)]
    public string? MediaUrl { get; set; }
    
    [MaxLength(50)]
    public string? MediaType { get; set; } // "video", "image"
    
    public string? ContentHtml { get; set; }
    
    [Range(1, 100)]
    public int DisplayOrder { get; set; } = 1;
    
    public bool IsRequired { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating an existing onboarding step
/// </summary>
public class UpdateOnboardingStepDto
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string StepKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string StepName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string StepType { get; set; } = "questions";
    
    [MaxLength(1000)]
    public string? MediaUrl { get; set; }
    
    [MaxLength(50)]
    public string? MediaType { get; set; }
    
    public string? ContentHtml { get; set; }
    
    [Range(1, 100)]
    public int DisplayOrder { get; set; }
    
    public bool IsRequired { get; set; }
    
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for onboarding step response
/// </summary>
public class OnboardingStepResponseDto
{
    public int Id { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StepType { get; set; } = "questions";
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public string? ContentHtml { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int QuestionCount { get; set; }
    public List<OnboardingQuestionResponseDto> Questions { get; set; } = new();
}

/// <summary>
/// DTO for creating a new onboarding question
/// </summary>
public class CreateOnboardingQuestionDto
{
    [Required]
    public int OnboardingStepId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string QuestionKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string QuestionText { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string QuestionType { get; set; } = string.Empty; // text, date, select, multiselect, number, boolean, phone, email, etc.
    
    public string? ValidationRules { get; set; } // JSON string
    
    public string? Options { get; set; } // JSON string for select/multiselect
    
    [MaxLength(500)]
    public string? Placeholder { get; set; }
    
    [MaxLength(1000)]
    public string? HelpText { get; set; }
    
    public bool IsRequired { get; set; } = true;
    
    [Range(1, 100)]
    public int DisplayOrder { get; set; } = 1;
    
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating an existing onboarding question
/// </summary>
public class UpdateOnboardingQuestionDto
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    public int OnboardingStepId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string QuestionKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string QuestionText { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string QuestionType { get; set; } = string.Empty;
    
    public string? ValidationRules { get; set; }
    
    public string? Options { get; set; }
    
    [MaxLength(500)]
    public string? Placeholder { get; set; }
    
    [MaxLength(1000)]
    public string? HelpText { get; set; }
    
    public bool IsRequired { get; set; }
    
    [Range(1, 100)]
    public int DisplayOrder { get; set; }
    
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for onboarding question response
/// </summary>
public class OnboardingQuestionResponseDto
{
    public int Id { get; set; }
    public int OnboardingStepId { get; set; }
    public string QuestionKey { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? ValidationRules { get; set; }
    public string? Options { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for bulk creating step with questions
/// </summary>
public class CreateStepWithQuestionsDto
{
    [Required]
    public CreateOnboardingStepDto Step { get; set; } = new();
    
    public List<CreateOnboardingQuestionDto> Questions { get; set; } = new();
}

/// <summary>
/// DTO for reordering steps
/// </summary>
public class ReorderStepsDto
{
    [Required]
    public List<StepOrderDto> Steps { get; set; } = new();
}

public class StepOrderDto
{
    [Required]
    public int StepId { get; set; }
    
    [Range(1, 100)]
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for reordering questions within a step
/// </summary>
public class ReorderQuestionsDto
{
    [Required]
    public int StepId { get; set; }
    
    [Required]
    public List<QuestionOrderDto> Questions { get; set; } = new();
}

public class QuestionOrderDto
{
    [Required]
    public int QuestionId { get; set; }
    
    [Range(1, 100)]
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for onboarding statistics
/// </summary>
public class OnboardingStatisticsDto
{
    public int TotalSteps { get; set; }
    public int ActiveSteps { get; set; }
    public int TotalQuestions { get; set; }
    public int ActiveQuestions { get; set; }
    public int TotalUsersStarted { get; set; }
    public int TotalUsersCompleted { get; set; }
    public double CompletionRate { get; set; }
    public List<StepStatisticsDto> StepStatistics { get; set; } = new();
}

public class StepStatisticsDto
{
    public int StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int UsersStarted { get; set; }
    public int UsersCompleted { get; set; }
    public double CompletionRate { get; set; }
    public TimeSpan AverageCompletionTime { get; set; }
}
