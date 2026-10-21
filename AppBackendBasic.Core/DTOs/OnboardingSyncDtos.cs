using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.DTOs;

/// <summary>
/// Request DTO for syncing offline onboarding answers
/// Client receives configuration from /api/app/initialize
/// Client sends back only the answers after completion
/// </summary>
public class OnboardingSyncRequestDto
{
    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = string.Empty;
    
    [Required]
    public DateTime CompletedAt { get; set; }
    
    [Required]
    [MinLength(1)]
    public List<OnboardingStepSyncDto> Steps { get; set; } = new();
}

/// <summary>
/// Individual step completion data in sync request
/// Only includes stepId (from backend) and answers
/// </summary>
public class OnboardingStepSyncDto
{
    [Required]
    public int StepId { get; set; } // Backend-provided step ID from /initialize
    
    [Required]
    public DateTime CompletedAt { get; set; }
    
    public List<OnboardingAnswerSyncDto>? Answers { get; set; }
}

/// <summary>
/// Individual answer data in sync request
/// Only includes questionId (from backend) and answer value
/// </summary>
public class OnboardingAnswerSyncDto
{
    [Required]
    public int QuestionId { get; set; } // Backend-provided question ID from /initialize
    
    [Required]
    public string AnswerValue { get; set; } = string.Empty;
}

/// <summary>
/// Success response for onboarding sync
/// </summary>
public class OnboardingSyncResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SyncedSteps { get; set; }
    public int SyncedAnswers { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Error response for onboarding sync
/// </summary>
public class OnboardingSyncErrorDto
{
    public bool Success { get; set; } = false;
    public string Error { get; set; } = string.Empty;
    public string? Message { get; set; }
    public OnboardingSyncErrorDetailsDto? Details { get; set; }
    public OnboardingExistingDataDto? ExistingData { get; set; }
}

/// <summary>
/// Validation error details
/// </summary>
public class OnboardingSyncErrorDetailsDto
{
    public List<string>? MissingFields { get; set; }
    public List<string>? InvalidSteps { get; set; }
    public List<string>? InvalidAnswers { get; set; }
}

/// <summary>
/// Existing onboarding data info (for conflict response)
/// </summary>
public class OnboardingExistingDataDto
{
    public DateTime SyncedAt { get; set; }
    public int StepCount { get; set; }
}
