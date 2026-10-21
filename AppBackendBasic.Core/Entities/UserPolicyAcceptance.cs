using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Tracks user acceptance of privacy policies and terms of service
/// </summary>
public class UserPolicyAcceptance
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public int PolicyId { get; set; }
    
    public DateTime AcceptedAt { get; set; }
    
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(50)]
    public string? DeviceId { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual PolicyDocument Policy { get; set; } = null!;
}
