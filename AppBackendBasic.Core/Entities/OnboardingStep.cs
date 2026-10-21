using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Defines the onboarding steps/questions that users need to complete
/// </summary>
public class OnboardingStep
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string StepKey { get; set; } = string.Empty; // e.g., "basic_info", "preferences", "goals"
    
    [Required]
    [MaxLength(200)]
    public string StepName { get; set; } = string.Empty; // Display name
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string StepType { get; set; } = "questions"; // "questions", "video", "content"
    
    [MaxLength(1000)]
    public string? MediaUrl { get; set; } // URL for video or image content
    
    [MaxLength(50)]
    public string? MediaType { get; set; } // "video", "image", null
    
    public string? ContentHtml { get; set; } // HTML content for "content" type steps
    
    public int DisplayOrder { get; set; } // Order in which steps should be shown
    
    public bool IsRequired { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<OnboardingQuestion> Questions { get; set; } = new List<OnboardingQuestion>();
    public virtual ICollection<UserOnboardingProgress> UserProgress { get; set; } = new List<UserOnboardingProgress>();
}
