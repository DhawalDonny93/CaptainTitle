using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Tracks user's progress through onboarding steps
/// </summary>
public class UserOnboardingProgress
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public int? OnboardingStepId { get; set; } // Nullable to support client-defined steps
    
    public int? UserOnboardingSyncId { get; set; } // Link to sync record
    
    [MaxLength(100)]
    public string? StepId { get; set; } // Client-provided step ID
    
    [MaxLength(100)]
    public string? StepKey { get; set; } // Client-provided step key
    
    [MaxLength(50)]
    public string? StepType { get; set; } // "questions", "video", "content"
    
    public bool IsCompleted { get; set; } = false;
    
    public DateTime? CompletedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual OnboardingStep? OnboardingStep { get; set; }
    public virtual UserOnboardingSync? UserOnboardingSync { get; set; }
}
