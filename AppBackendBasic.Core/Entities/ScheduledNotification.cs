using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Scheduled notification for future delivery
/// </summary>
public class ScheduledNotification
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Body { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Sound { get; set; } = "default";
    
    [MaxLength(10)]
    public string? Badge { get; set; }
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    public string? DataJson { get; set; } // Serialized dictionary
    
    public DateTime ScheduledFor { get; set; }
    
    public bool IsSent { get; set; } = false;
    
    public DateTime? SentAt { get; set; }
    
    public bool IsCancelled { get; set; } = false;
    
    public DateTime? CancelledAt { get; set; }
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public int MaxRetries { get; set; } = 3;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
