using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Stores device tokens for push notifications
/// </summary>
public class DeviceToken
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Platform { get; set; } = string.Empty; // iOS, Android
    
    [MaxLength(20)]
    public string TokenType { get; set; } = "Native"; // Native, Expo
    
    [MaxLength(50)]
    public string? DeviceId { get; set; }
    
    [MaxLength(100)]
    public string? DeviceModel { get; set; }
    
    [MaxLength(20)]
    public string? AppVersion { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public DateTime? LastUsedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
