using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities;

public class SubscriptionTransaction
{
    public int Id { get; set; }
    
    public int SubscriptionId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string TransactionId { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // 'completed', 'failed', 'refunded'
    
    public DateTime TransactionDate { get; set; }
    
    // RevenueCat specific fields
    [MaxLength(255)]
    public string? RevenueCatEventId { get; set; }
    
    [MaxLength(255)]
    public string? StoreTransactionId { get; set; }
    
    [MaxLength(50)]
    public string? TransactionType { get; set; } // 'initial_purchase', 'renewal', 'cancellation', etc.
    
    [MaxLength(255)]
    public string? StripeInvoiceId { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? RawData { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Subscription Subscription { get; set; } = null!;
}
