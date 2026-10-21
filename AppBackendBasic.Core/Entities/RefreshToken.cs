using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}