using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

public class UserRevenueCatMapping
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string RevenueCatId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
