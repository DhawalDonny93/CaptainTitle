using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// User preferences for UI and app behavior
/// </summary>
public class UserPreference
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [MaxLength(10)]
    public string Language { get; set; } = "en"; // en, es, fr, de, etc.
    
    [MaxLength(20)]
    public string Theme { get; set; } = "light"; // light, dark, auto
    
    [MaxLength(50)]
    public string Timezone { get; set; } = "UTC"; // IANA timezone
    
    public string CustomPreferences { get; set; } = "{}"; // JSON for additional preferences
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public User? User { get; set; }
}
