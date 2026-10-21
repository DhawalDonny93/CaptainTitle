using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string DeviceModel { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string AppVersion { get; set; } = string.Empty;
    
    public DateTime LastActiveAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Session Timeout
    public DateTime ExpiresAt { get; set; }
    public int TimeoutMinutes { get; set; } = 30;
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
