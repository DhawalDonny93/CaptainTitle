using System;

namespace AppBackendBasic.Core.Entities;

public class NotificationPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool PushEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool SystemNotifications { get; set; } = true;
    public bool SubscriptionNotifications { get; set; } = true;
    public bool SocialNotifications { get; set; } = true;
    public bool MarketingNotifications { get; set; } = false;
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public virtual User User { get; set; } = null!;
}
