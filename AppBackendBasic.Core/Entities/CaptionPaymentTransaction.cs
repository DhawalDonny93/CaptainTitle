using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>Stripe Checkout attempt (old Mongo `payment_transactions`).</summary>
public class CaptionPaymentTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required, MaxLength(255)]
    public string SessionId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string PackageId { get; set; } = string.Empty;

    /// <summary>initiated | completed</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "initiated";

    /// <summary>pending | paid</summary>
    [Required, MaxLength(20)]
    public string PaymentStatus { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
