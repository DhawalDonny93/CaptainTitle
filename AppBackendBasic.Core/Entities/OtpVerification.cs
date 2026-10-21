using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

public class OtpVerification
{
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; } = string.Empty;
    
    [MaxLength(6)]
    public string Otp { get; set; } = string.Empty;
    
    public string SessionId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    public int AttemptCount { get; set; } = 0;
    
    public int MaxAttempts { get; set; } = 3;
}
