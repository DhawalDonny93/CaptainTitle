using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Reusable notification templates for common scenarios
/// </summary>
public class NotificationTemplate
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // Welcome, SubscriptionExpiring, etc.
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Body { get; set; } = string.Empty;
    
    public string? DataJson { get; set; } // Additional data as JSON
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    [MaxLength(50)]
    public string? Sound { get; set; } = "default";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}
