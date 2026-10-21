using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities;

public class PaywallEvent
{
    public int Id { get; set; }
    
    public int? UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty; // 'impression', 'click', 'purchase', 'close'
    
    [Required]
    [MaxLength(255)]
    public string PaywallId { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? DeviceId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Platform { get; set; } = string.Empty; // 'ios', 'android', 'web'
    
    [Column(TypeName = "jsonb")]
    public string? EventData { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
}
