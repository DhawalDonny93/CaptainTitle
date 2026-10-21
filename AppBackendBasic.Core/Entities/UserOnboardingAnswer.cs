using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Stores user's answers to onboarding questions
/// </summary>
public class UserOnboardingAnswer
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public int? OnboardingQuestionId { get; set; } // Nullable to support client-defined questions
    
    public int? UserOnboardingProgressId { get; set; } // Link to progress record
    
    [MaxLength(100)]
    public string? QuestionId { get; set; } // Client-provided question ID
    
    [MaxLength(100)]
    public string? QuestionKey { get; set; } // Client-provided question key
    
    [Required]
    public string AnswerValue { get; set; } = string.Empty; // Stored as string, can be JSON for complex answers
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual OnboardingQuestion? OnboardingQuestion { get; set; }
    public virtual UserOnboardingProgress? UserOnboardingProgress { get; set; }
}
