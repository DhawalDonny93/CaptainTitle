using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

public class LlmUsageQuota
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    // Quota period
    public DateTime PeriodStart { get; set; } = DateTime.UtcNow;
    public DateTime PeriodEnd { get; set; } = DateTime.UtcNow.AddDays(30);
    
    // Usage tracking
    public int RequestsUsed { get; set; } = 0;
    public int RequestsLimit { get; set; } = 100; // Based on subscription
    
    public int TokensUsed { get; set; } = 0;
    public int TokensLimit { get; set; } = 100000; // Based on subscription
    
    public int ImagesUsed { get; set; } = 0;
    public int ImagesLimit { get; set; } = 50; // Based on subscription
    
    // Rate limiting
    public int RequestsPerMinute { get; set; } = 10; // Based on subscription
    public int RequestsPerHour { get; set; } = 100; // Based on subscription
    public int RequestsPerDay { get; set; } = 500; // Based on subscription
    
    // Last request tracking
    public DateTime? LastRequestAt { get; set; }
    public int ConsecutiveRequests { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
}
