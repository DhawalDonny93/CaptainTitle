using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities;

public class RevenueCatWebhookEvent
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string EventId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? AppUserId { get; set; }
    
    [MaxLength(255)]
    public string? OriginalAppUserId { get; set; }
    
    [MaxLength(255)]
    public string? ProductId { get; set; }
    
    [MaxLength(255)]
    public string? EntitlementId { get; set; }
    
    public long? PurchasedAtMs { get; set; }
    
    public long? ExpirationAtMs { get; set; }
    
    [MaxLength(20)]
    public string? Environment { get; set; }
    
    [MaxLength(255)]
    public string? OriginalTransactionId { get; set; }
    
    [MaxLength(10)]
    public string? Currency { get; set; }
    
    [Column(TypeName = "decimal(10, 2)")]
    public decimal? Price { get; set; }
    
    public bool Processed { get; set; } = false;
    
    public DateTime? ProcessedAt { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string RawPayload { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
}
