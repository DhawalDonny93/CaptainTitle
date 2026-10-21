using System;
using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Unified notification entity for all notification types (push, web, in-app)
/// </summary>
public class Notification
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Body { get; set; } = string.Empty;
    
    // Info, Success, Warning, Error
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "info";
    
    // Web notification specific fields
    [MaxLength(50)]
    public string? Icon { get; set; }
    
    public bool IsGlobal { get; set; } = false;
    
    public bool IsDismissible { get; set; } = true;
    
    public int? DurationMs { get; set; } = 5000;
    
    [MaxLength(500)]
    public string? ActionUrl { get; set; }
    
    [MaxLength(100)]
    public string? ActionText { get; set; }
    
    // Push notification specific fields
    public string? Data { get; set; } // JSON string
    
    public string? ImageUrl { get; set; }
    
    public bool IsSent { get; set; } = false;
    
    public DateTime? SentAt { get; set; }
    
    public DateTime? ScheduledFor { get; set; }
    
    // Common fields
    public bool IsRead { get; set; } = false;
    
    public DateTime? ReadAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual User User { get; set; } = null!;
}
