using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Tracks parental consent for users under 18 (COPPA compliance)
/// </summary>
public class ParentalConsent
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ParentEmail { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? ParentName { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ConsentToken { get; set; } = string.Empty;
    
    public bool IsGranted { get; set; } = false;
    
    public DateTime? GrantedAt { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
