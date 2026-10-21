using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

public class UserProvider
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty; // 'apple', 'google', 'email', 'mobile'
    
    [MaxLength(255)]
    public string ProviderId { get; set; } = string.Empty;
    
    public string? ProviderData { get; set; } // JSON data
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}