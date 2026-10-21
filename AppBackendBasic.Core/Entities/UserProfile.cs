using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// Consolidated user profile and preferences table
/// Stores all user-related information outside the main User table
/// </summary>
public class UserProfile
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    // UI Preferences
    [MaxLength(10)]
    public string Language { get; set; } = "en"; // en, es, fr, de, etc.
    
    [MaxLength(20)]
    public string Theme { get; set; } = "light"; // light, dark, auto
    
    [MaxLength(50)]
    public string Timezone { get; set; } = "UTC"; // IANA timezone
    
    // Additional Profile Information
    public string? Bio { get; set; }
    
    [MaxLength(200)]
    public string? Company { get; set; }
    
    [MaxLength(100)]
    public string? JobTitle { get; set; }
    
    [MaxLength(500)]
    public string? Website { get; set; }
    
    [MaxLength(200)]
    public string? Location { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    // Social Links
    [MaxLength(500)]
    public string? LinkedInUrl { get; set; }
    
    [MaxLength(500)]
    public string? TwitterUrl { get; set; }
    
    [MaxLength(500)]
    public string? GitHubUrl { get; set; }
    
    // Communication Preferences
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool MarketingEmails { get; set; } = false;
    public bool WeeklyDigest { get; set; } = true;
    
    // Display Preferences
    [MaxLength(20)]
    public string DateFormat { get; set; } = "MM/DD/YYYY";
    
    [MaxLength(10)]
    public string TimeFormat { get; set; } = "12h"; // 12h, 24h
    
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";
    
    // Privacy Settings
    [MaxLength(20)]
    public string ProfileVisibility { get; set; } = "private"; // public, private, connections
    
    public bool ShowEmail { get; set; } = false;
    public bool ShowPhone { get; set; } = false;
    
    // Extended properties as JSON for flexibility
    public string CustomPreferences { get; set; } = "{}"; // JSONB in PostgreSQL
    public string ExtendedProperties { get; set; } = "{}"; // JSONB in PostgreSQL
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
