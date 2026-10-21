using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities;

public class RevenueEvent
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string RevenueId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    [Required]
    [MaxLength(255)]
    public string ProductId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string TransactionId { get; set; } = string.Empty;
    
    [Column(TypeName = "jsonb")]
    public string? Properties { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public bool MixpanelTracked { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
}
