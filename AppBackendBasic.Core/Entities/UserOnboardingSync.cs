using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Tracks overall onboarding sync status for a user (offline-first sync)
/// </summary>
public class UserOnboardingSync
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = string.Empty; // Onboarding config version (e.g., "1.0.0")
    
    public bool IsCompleted { get; set; } = true; // Always true for synced data
    
    public DateTime CompletedAt { get; set; } // When user completed onboarding on client
    
    public DateTime SyncedAt { get; set; } // When data was synced to server
    
    public int TotalSteps { get; set; } // Number of steps synced
    
    public int TotalAnswers { get; set; } // Number of answers synced
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
