using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Stores privacy policy and terms of service documents
/// </summary>
public class PolicyDocument
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // 'Privacy', 'Terms'
    
    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = string.Empty; // e.g., "1.0", "2.1"
    
    [Required]
    public string Content { get; set; } = string.Empty; // Full policy text (HTML or Markdown)
    
    [MaxLength(500)]
    public string? Summary { get; set; } // Brief summary of changes
    
    public DateTime EffectiveDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public virtual ICollection<UserPolicyAcceptance> UserAcceptances { get; set; } = new List<UserPolicyAcceptance>();
}
