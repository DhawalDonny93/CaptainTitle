using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities;

public class Subscription
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string ProductId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // 'active', 'canceled', 'expired', 'refunded'
    
    [Required]
    [MaxLength(20)]
    public string Platform { get; set; } = string.Empty; // 'ios', 'android', 'web'
    
    [Required]
    [MaxLength(255)]
    public string PurchaseToken { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? TransactionId { get; set; }
    
    [MaxLength(255)]
    public string? OriginalTransactionId { get; set; }
    
    public DateTime PurchaseDate { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    public bool AutoRenew { get; set; } = false;
    
    // RevenueCat specific fields
    [MaxLength(255)]
    public string? RevenueCatId { get; set; }
    
    [MaxLength(255)]
    public string? EntitlementIdentifier { get; set; }
    
    [MaxLength(255)]
    public string? OfferIdentifier { get; set; }
    
    public bool IsSandbox { get; set; } = false;
    
    [Column(TypeName = "jsonb")]
    public string? RenewalInfo { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<SubscriptionTransaction> Transactions { get; set; } = new List<SubscriptionTransaction>();
}
