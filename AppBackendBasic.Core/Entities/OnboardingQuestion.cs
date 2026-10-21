using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Defines individual questions within an onboarding step
/// </summary>
public class OnboardingQuestion
{
    public int Id { get; set; }
    
    public int OnboardingStepId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string QuestionKey { get; set; } = string.Empty; // e.g., "first_name", "date_of_birth"
    
    [Required]
    [MaxLength(500)]
    public string QuestionText { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string QuestionType { get; set; } = string.Empty; // text, date, select, multiselect, number, etc.
    
    public string? ValidationRules { get; set; } // JSON string for validation rules
    
    public string? Options { get; set; } // JSON string for select/multiselect options
    
    [MaxLength(500)]
    public string? Placeholder { get; set; }
    
    [MaxLength(1000)]
    public string? HelpText { get; set; }
    
    public bool IsRequired { get; set; } = true;
    
    public int DisplayOrder { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual OnboardingStep OnboardingStep { get; set; } = null!;
    public virtual ICollection<UserOnboardingAnswer> UserAnswers { get; set; } = new List<UserOnboardingAnswer>();
}
