using System.Text.Json.Serialization;

namespace AppBackendBasic.Core.DTOs;

public class RevenueCatWebhookPayload
{
    [JsonPropertyName("event")]
    public RevenueCatEvent Event { get; set; } = new();
    
    [JsonPropertyName("api_version")]
    public string ApiVersion { get; set; } = string.Empty;
}

public class RevenueCatEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "INITIAL_PURCHASE", "RENEWAL", "CANCELLATION", "EXPIRATION", etc.
    
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;
    
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("original_app_user_id")]
    public string OriginalAppUserId { get; set; } = string.Empty;
    
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;
    
    [JsonPropertyName("entitlement_id")]
    public string EntitlementId { get; set; } = string.Empty;
    
    [JsonPropertyName("purchased_at_ms")]
    public long PurchasedAtMs { get; set; }
    
    [JsonPropertyName("expiration_at_ms")]
    public long? ExpirationAtMs { get; set; }
    
    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty; // "PRODUCTION" or "SANDBOX"
    
    [JsonPropertyName("original_transaction_id")]
    public string OriginalTransactionId { get; set; } = string.Empty;
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("offer_code")]
    public string? OfferCode { get; set; }
    
    [JsonPropertyName("store")]
    public string Store { get; set; } = string.Empty; // "APP_STORE", "PLAY_STORE", "STRIPE", etc.
    
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }
    
    [JsonPropertyName("renewal_number")]
    public int? RenewalNumber { get; set; }
    
    [JsonPropertyName("is_trial_conversion")]
    public bool? IsTrialConversion { get; set; }
    
    [JsonPropertyName("presented_offering_id")]
    public string? PresentedOfferingId { get; set; }
}

public class RevenueCatUserDto
{
    [JsonPropertyName("app_user_id")]
    public string AppUserId { get; set; } = string.Empty;
    
    [JsonPropertyName("backend_user_id")]
    public string? BackendUserId { get; set; }
}

public class RevenueCatSubscriptionStatusDto
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    
    [JsonPropertyName("product_id")]
    public string? ProductId { get; set; }
    
    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }
    
    [JsonPropertyName("auto_renew")]
    public bool AutoRenew { get; set; }
    
    [JsonPropertyName("store")]
    public string? Store { get; set; }
}

public class RevenueCatSyncRequestDto
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    
    [JsonPropertyName("revenue_cat_id")]
    public string RevenueCatId { get; set; } = string.Empty;
    
    [JsonPropertyName("subscription_data")]
    public Dictionary<string, object>? SubscriptionData { get; set; }
}
