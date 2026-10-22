using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>Per-user caption credit balances (old Mongo `usage` collection).</summary>
public class CaptionUsage
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public virtual User? User { get; set; }

    public int FreeRemaining { get; set; }
    public int OneoffCredits { get; set; }
    public int OneoffPurchaseCount { get; set; }
    public int BasicCredits { get; set; }
    public bool SubscriptionActive { get; set; }
    public int VideosCreated { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
