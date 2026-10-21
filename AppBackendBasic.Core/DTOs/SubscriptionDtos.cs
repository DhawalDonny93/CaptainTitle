using System.Text.Json.Serialization;

namespace AppBackendBasic.Core.DTOs;

public class SubscriptionStatusDto
{
    public bool IsSubscribed { get; set; }
    public SubscriptionDetailsDto? SubscriptionDetails { get; set; }
}

public class SubscriptionDetailsDto
{
    public string ProductId { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public bool AutoRenew { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RegisterSubscriptionDto
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;
    
    [JsonPropertyName("purchaseToken")]
    public string PurchaseToken { get; set; } = string.Empty;
    
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;
    
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }
    
    [JsonPropertyName("originalTransactionId")]
    public string? OriginalTransactionId { get; set; }
    
    [JsonPropertyName("purchaseDate")]
    public DateTime PurchaseDate { get; set; }
    
    [JsonPropertyName("expiryDate")]
    public DateTime? ExpiryDate { get; set; }
}

public class VerifyReceiptDto
{
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;
    
    [JsonPropertyName("receipt")]
    public string Receipt { get; set; } = string.Empty;
}

public class VerifyReceiptResponseDto
{
    public bool IsValid { get; set; }
    public string? ProductId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CancelSubscriptionDto
{
    [JsonPropertyName("subscriptionId")]
    public int SubscriptionId { get; set; }
}

public class SubscriptionResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? SubscriptionId { get; set; }
}

public class PaywallEventDto
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;
    
    [JsonPropertyName("paywallId")]
    public string PaywallId { get; set; } = string.Empty;
    
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }
    
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;
    
    [JsonPropertyName("eventData")]
    public Dictionary<string, object>? EventData { get; set; }
}

public class PaywallPerformanceDto
{
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public int Purchases { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal Revenue { get; set; }
    public string Currency { get; set; } = "USD";
    public Dictionary<string, PaywallPerformanceDto>? ByPaywall { get; set; }
}

public class FreeSubscriptionRequestDto
{
    [JsonPropertyName("planType")]
    public string PlanType { get; set; } = "free";
    
    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

public class SubscriptionPlanDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
    
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty; // "month", "year", "lifetime"
    
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
}

public class EntitlementDto
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("willRenew")]
    public bool WillRenew { get; set; }
    
    [JsonPropertyName("periodType")]
    public string PeriodType { get; set; } = "normal";
    
    [JsonPropertyName("latestPurchaseDate")]
    public DateTime? LatestPurchaseDate { get; set; }
    
    [JsonPropertyName("originalPurchaseDate")]
    public DateTime? OriginalPurchaseDate { get; set; }
    
    [JsonPropertyName("expirationDate")]
    public DateTime? ExpirationDate { get; set; }
    
    [JsonPropertyName("store")]
    public string Store { get; set; } = string.Empty;
    
    [JsonPropertyName("productIdentifier")]
    public string ProductIdentifier { get; set; } = string.Empty;
    
    [JsonPropertyName("isSandbox")]
    public bool IsSandbox { get; set; }
}

public class EnhancedSubscriptionStatusDto
{
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("subscriptionStatus")]
    public string SubscriptionStatus { get; set; } = "inactive";
    
    [JsonPropertyName("subscriptionType")]
    public string? SubscriptionType { get; set; }
    
    [JsonPropertyName("productId")]
    public string? ProductId { get; set; }
    
    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }
    
    [JsonPropertyName("expirationDate")]
    public DateTime? ExpirationDate { get; set; }
    
    [JsonPropertyName("entitlements")]
    public Dictionary<string, EntitlementDto> Entitlements { get; set; } = new();
}

public class SyncRevenueCatRequestDto
{
    [JsonPropertyName("revenueCatUserId")]
    public string RevenueCatUserId { get; set; } = string.Empty;
    
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;
    
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;
    
    [JsonPropertyName("purchaseDate")]
    public DateTime PurchaseDate { get; set; }
    
    [JsonPropertyName("expirationDate")]
    public DateTime? ExpirationDate { get; set; }
    
    [JsonPropertyName("store")]
    public string Store { get; set; } = string.Empty;
    
    [JsonPropertyName("entitlements")]
    public Dictionary<string, object>? Entitlements { get; set; }
}

public class LinkRevenueCatRequestDto
{
    [JsonPropertyName("revenueCatUserId")]
    public string RevenueCatUserId { get; set; } = string.Empty;
}

public class CancelSubscriptionRequestDto
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class SubscriptionPlansResponseDto
{
    [JsonPropertyName("plans")]
    public List<SubscriptionPlanDto> Plans { get; set; } = new();
}

public class SubscriptionTransactionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("transactionDate")]
    public DateTime TransactionDate { get; set; }
    
    [JsonPropertyName("transactionType")]
    public string? TransactionType { get; set; }
    
    [JsonPropertyName("productId")]
    public string? ProductId { get; set; }
}

// Quota Management DTOs
public class QuotaStatusDto
{
    public string QuotaType { get; set; } = string.Empty;
    public int Limit { get; set; }
    public int Used { get; set; }
    public int Remaining { get; set; }
    public double PercentageUsed { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool IsExceeded { get; set; }
    public bool IsNearLimit { get; set; } // 80% or more used
}

public class FeatureLimitsDto
{
    public int MaxProjects { get; set; }
    public int MaxPromptsPerProject { get; set; }
    public int MaxVisibilityChecksPerMonth { get; set; }
    public int MaxSiteAuditsPerMonth { get; set; }
    public int MaxContentArticles { get; set; }
    public int MaxLlmRequestsPerMonth { get; set; }
    public int MaxLlmTokensPerMonth { get; set; }
    public bool AllowCompetitorTracking { get; set; }
    public bool AllowAdvancedAnalytics { get; set; }
    public bool AllowApiAccess { get; set; }
    public bool AllowWhiteLabel { get; set; }
    public bool AllowPrioritySupport { get; set; }
    public bool AllowCustomIntegrations { get; set; }
}

public class SubscriptionMetricsDto
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TrialSubscriptions { get; set; }
    public int CancelledSubscriptions { get; set; }
    public int ExpiredSubscriptions { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal AnnualRecurringRevenue { get; set; }
    public Dictionary<string, int> SubscriptionsByPlan { get; set; } = new();
    public Dictionary<string, decimal> RevenueByPlan { get; set; } = new();
    public double ChurnRate { get; set; }
    public double AverageRevenuePerUser { get; set; }
}

public class QuotaWarningDto
{
    public string QuotaType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int PercentageUsed { get; set; }
    public int Remaining { get; set; }
    public string Severity { get; set; } = "Info"; // Info, Warning, Critical
    public bool ShowUpgradePrompt { get; set; }
    public string? UpgradeUrl { get; set; }
}

public class SubscriptionUpgradeDto
{
    public int CurrentPlanId { get; set; }
    public string CurrentPlanName { get; set; } = string.Empty;
    public int TargetPlanId { get; set; }
    public string TargetPlanName { get; set; } = string.Empty;
    public decimal PriceDifference { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
    public DateTime EffectiveDate { get; set; }
    public bool ProrateBilling { get; set; } = true;
}
