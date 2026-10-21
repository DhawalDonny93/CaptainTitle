using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// In-app notification for user notification center
/// </summary>
public class InAppNotification
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Body { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Type { get; set; } = "info"; // info, success, warning, error
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    [MaxLength(500)]
    public string? ActionUrl { get; set; }
    
    [MaxLength(100)]
    public string? ActionText { get; set; }
    
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    public string? DataJson { get; set; } // Additional metadata
    
    public bool IsRead { get; set; } = false;
    
    public DateTime? ReadAt { get; set; }
    
    public bool IsArchived { get; set; } = false;
    
    public DateTime? ArchivedAt { get; set; }
    
    public int Priority { get; set; } = 0; // 0 = normal, 1 = high, 2 = urgent
    
    public DateTime? ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
