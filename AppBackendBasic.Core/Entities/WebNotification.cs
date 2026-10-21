using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Web notifications for admin portal - toast/banner messages
/// </summary>
public class WebNotification
{
    public int Id { get; set; }
    
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Body { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "info"; // success, error, warning, info
    
    [MaxLength(50)]
    public string? Icon { get; set; }
    
    public int? UserId { get; set; }
    
    public bool IsGlobal { get; set; } = false;
    
    public bool IsDismissible { get; set; } = true;
    
    public int? DurationMs { get; set; } = 5000;
    
    [MaxLength(255)]
    public string? ActionUrl { get; set; }
    
    [MaxLength(50)]
    public string? ActionText { get; set; }
    
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    public DateTime? ReadAt { get; set; }
    
    // Navigation property
    public User? User { get; set; }
}

public static class NotificationTypes
{
    public const string Success = "success";
    public const string Error = "error";
    public const string Warning = "warning";
    public const string Info = "info";
}
