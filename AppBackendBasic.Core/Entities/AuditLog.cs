using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Audit log for tracking sensitive operations and changes
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    
    public int? UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // Login, Logout, Update, Delete, etc.
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty; // User, Subscription, etc.
    
    public int? EntityId { get; set; }
    
    public string? Changes { get; set; } // JSON of changes
    
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(50)]
    public string? DeviceId { get; set; }
    
    [MaxLength(500)]
    public string? AdditionalInfo { get; set; }
    
    public bool Success { get; set; } = true;
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}
