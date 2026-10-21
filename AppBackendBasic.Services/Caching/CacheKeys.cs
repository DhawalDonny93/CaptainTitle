namespace AppBackendBasic.Services.Caching;

/// <summary>
/// Centralized cache key definitions
/// </summary>
public static class CacheKeys
{
    // User-related keys
    public static string User(int userId) => $"user:{userId}";
    public static string UserProfile(int userId) => $"user:profile:{userId}";
    public static string UserRoles(int userId) => $"user:roles:{userId}";
    public static string UserSessions(int userId) => $"user:sessions:{userId}";
    
    // Subscription-related keys
    public static string UserSubscription(int userId) => $"subscription:user:{userId}";
    public static string ActiveSubscriptions() => "subscriptions:active";
    
    // Notification-related keys
    public static string NotificationPreferences(int userId) => $"notifications:preferences:{userId}";
    public static string UnreadNotifications(int userId) => $"notifications:unread:{userId}";
    public static string DeviceTokens(int userId) => $"notifications:devices:{userId}";
    
    // Onboarding-related keys
    public static string OnboardingProgress(int userId) => $"onboarding:progress:{userId}";
    public static string OnboardingAnswers(int userId) => $"onboarding:answers:{userId}";
    
    // System settings
    public static string SystemSetting(string key) => $"settings:{key}";
    public static string AllSystemSettings() => "settings:all";
    public static string SystemSettingsByCategory(string category) => $"settings:category:{category}";
    
    // Analytics (short-lived cache)
    public static string DashboardStats() => "analytics:dashboard";
    public static string UserGrowth(int days) => $"analytics:user-growth:{days}";
    public static string SubscriptionAnalytics(int days) => $"analytics:subscriptions:{days}";
    
    // Pattern matchers for bulk invalidation
    public static string UserPattern(int userId) => $"user:{userId}*";
    public static string SubscriptionPattern(int userId) => $"subscription:*:{userId}*";
    public static string NotificationPattern(int userId) => $"notifications:*:{userId}*";
    public static string SettingsPattern() => "settings:*";
    
    // Cache expiration times
    public static class Expiration
    {
        public static readonly TimeSpan UserProfile = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan UserRoles = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan Subscription = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan NotificationPreferences = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan OnboardingData = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan SystemSettings = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan Analytics = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan DeviceTokens = TimeSpan.FromMinutes(30);
    }
}
