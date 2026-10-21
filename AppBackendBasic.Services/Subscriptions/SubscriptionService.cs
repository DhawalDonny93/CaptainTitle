using System.Text.Json;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Subscriptions;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ApplicationDbContext db,
        ILogger<SubscriptionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SubscriptionStatusDto> GetStatusAsync(int userId)
    {
        var now = DateTime.UtcNow;
        
        // Find active subscription (including free subscriptions with null expiry)
        var sub = await _db.Subscriptions
            .Where(s => s.UserId == userId && 
                       s.Status == "active" && 
                       (s.ExpiryDate == null || s.ExpiryDate > now))
            .OrderByDescending(s => s.ExpiryDate ?? DateTime.MaxValue) // Free subscriptions (null expiry) come first
            .FirstOrDefaultAsync();

        if (sub == null)
        {
            return new SubscriptionStatusDto { IsSubscribed = false };
        }

        return new SubscriptionStatusDto
        {
            IsSubscribed = true,
            SubscriptionDetails = new SubscriptionDetailsDto
            {
                ProductId = sub.ProductId,
                ExpiryDate = sub.ExpiryDate,
                AutoRenew = sub.AutoRenew,
                Platform = sub.Platform,
                Status = sub.Status
            }
        };
    }

    public async Task<SubscriptionResponseDto> RegisterAsync(int userId, RegisterSubscriptionDto request)
    {
        try
        {
            var existing = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ProductId == request.ProductId && s.PurchaseToken == request.PurchaseToken);

            if (existing == null)
            {
                var sub = new Subscription
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    Platform = request.Platform,
                    Status = "active",
                    PurchaseToken = request.PurchaseToken,
                    TransactionId = request.TransactionId,
                    OriginalTransactionId = request.OriginalTransactionId,
                    PurchaseDate = request.PurchaseDate,
                    ExpiryDate = request.ExpiryDate,
                    AutoRenew = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Subscriptions.Add(sub);
                await _db.SaveChangesAsync();
                return new SubscriptionResponseDto { Success = true, SubscriptionId = sub.Id };
            }
            else
            {
                existing.Status = "active";
                existing.TransactionId = request.TransactionId ?? existing.TransactionId;
                existing.OriginalTransactionId = request.OriginalTransactionId ?? existing.OriginalTransactionId;
                existing.PurchaseDate = request.PurchaseDate;
                existing.ExpiryDate = request.ExpiryDate;
                existing.AutoRenew = true;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return new SubscriptionResponseDto { Success = true, SubscriptionId = existing.Id };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering subscription for user {UserId}", userId);
            return new SubscriptionResponseDto { Success = false, Message = "Failed to register subscription" };
        }
    }

    public async Task<SubscriptionResponseDto> CancelAsync(int userId, int subscriptionId)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);
        if (sub == null)
            return new SubscriptionResponseDto { Success = false, Message = "Subscription not found" };

        sub.Status = "canceled";
        sub.AutoRenew = false;
        sub.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new SubscriptionResponseDto { Success = true, SubscriptionId = sub.Id };
    }
    
    #region RevenueCat Integration
    
    public async Task ProcessRevenueCatWebhookAsync(RevenueCatWebhookPayload payload)
    {
        try
        {
            // Store the raw webhook event for audit purposes
            var webhookEvent = new RevenueCatWebhookEvent
            {
                EventId = payload.Event.Id,
                EventType = payload.Event.Type,
                AppUserId = payload.Event.UserId,
                OriginalAppUserId = payload.Event.OriginalAppUserId,
                ProductId = payload.Event.ProductId,
                EntitlementId = payload.Event.EntitlementId,
                PurchasedAtMs = payload.Event.PurchasedAtMs,
                ExpirationAtMs = payload.Event.ExpirationAtMs,
                Environment = payload.Event.Environment,
                OriginalTransactionId = payload.Event.OriginalTransactionId,
                Currency = payload.Event.Currency,
                Price = payload.Event.Price,
                RawPayload = JsonSerializer.Serialize(payload)
            };
            
            _db.RevenueCatWebhookEvents.Add(webhookEvent);
            await _db.SaveChangesAsync();
            
            // Process the event based on its type
            SubscriptionResponseDto result = null;
            switch (payload.Event.Type)
            {
                case "INITIAL_PURCHASE":
                case "RENEWAL":
                    result = await ProcessSubscriptionPurchaseAsync(payload.Event);
                    break;
                    
                case "CANCELLATION":
                    result = await ProcessSubscriptionCancellationAsync(payload.Event);
                    break;
                    
                case "EXPIRATION":
                    result = await ProcessSubscriptionExpirationAsync(payload.Event);
                    break;
                    
                case "NON_RENEWING_PURCHASE":
                    result = await ProcessSubscriptionPurchaseAsync(payload.Event);
                    break;
                    
                case "PRODUCT_CHANGE":
                    // Handle product change
                    break;
                    
                default:
                    _logger.LogInformation($"Unhandled RevenueCat event type: {payload.Event.Type}");
                    break;
            }
            
            // Update the webhook event with processing status
            webhookEvent.Processed = result?.Success ?? false;
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            webhookEvent.ErrorMessage = result?.Success == false ? result.Message : null;
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RevenueCat webhook");
            throw;
        }
    }
    
    public async Task<SubscriptionResponseDto> ProcessSubscriptionPurchaseAsync(RevenueCatEvent eventData)
    {
        try
        {
            var userId = await GetOrCreateUserIdFromRevenueCatIdAsync(eventData.OriginalAppUserId);
            
            if (!userId.HasValue)
            {
                _logger.LogWarning($"Could not find or create user for RevenueCat ID: {eventData.OriginalAppUserId}");
                return new SubscriptionResponseDto { Success = false, Message = "User not found" };
            }
            
            // Check if subscription already exists
            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.OriginalTransactionId == eventData.OriginalTransactionId);
            
            var purchaseDate = DateTimeOffset.FromUnixTimeMilliseconds(eventData.PurchasedAtMs).UtcDateTime;
            var expirationDate = eventData.ExpirationAtMs.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(eventData.ExpirationAtMs.Value).UtcDateTime
                : DateTime.Now;
            
            if (subscription == null)
            {
                // Create new subscription
                subscription = new Subscription
                {
                    UserId = userId.Value,
                    ProductId = eventData.ProductId,
                    Status = "active",
                    Platform = DeterminePlatform(eventData.Store),
                    PurchaseToken = eventData.TransactionId ?? eventData.OriginalTransactionId,
                    TransactionId = eventData.TransactionId,
                    OriginalTransactionId = eventData.OriginalTransactionId,
                    PurchaseDate = purchaseDate,
                    ExpiryDate = expirationDate,
                    AutoRenew = true,
                    RevenueCatId = eventData.OriginalAppUserId,
                    EntitlementIdentifier = eventData.EntitlementId,
                    OfferIdentifier = eventData.OfferCode,
                    IsSandbox = eventData.Environment.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase),
                    RenewalInfo = JsonSerializer.Serialize(new { renewal_number = eventData.RenewalNumber }),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _db.Subscriptions.Add(subscription);
            }
            else
            {
                // Update existing subscription
                subscription.ExpiryDate = expirationDate;
                subscription.Status = "active";
                subscription.AutoRenew = true;
                subscription.TransactionId = eventData.TransactionId ?? subscription.TransactionId;
                subscription.RevenueCatId = eventData.OriginalAppUserId;
                subscription.EntitlementIdentifier = eventData.EntitlementId;
                subscription.OfferIdentifier = eventData.OfferCode ?? subscription.OfferIdentifier;
                subscription.IsSandbox = eventData.Environment.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase);
                subscription.RenewalInfo = JsonSerializer.Serialize(new { renewal_number = eventData.RenewalNumber });
                subscription.UpdatedAt = DateTime.UtcNow;
            }
            
            // Record transaction
            var transaction = new SubscriptionTransaction
            {
                SubscriptionId = subscription.Id,
                TransactionId = eventData.TransactionId ?? Guid.NewGuid().ToString(),
                Amount = eventData.Price,
                Currency = eventData.Currency,
                Status = "completed",
                TransactionDate = purchaseDate,
                RevenueCatEventId = eventData.Id,
                StoreTransactionId = eventData.TransactionId,
                TransactionType = eventData.Type.ToLowerInvariant(),
                RawData = JsonSerializer.Serialize(eventData),
                CreatedAt = DateTime.UtcNow
            };
            
            _db.SubscriptionTransactions.Add(transaction);
            await _db.SaveChangesAsync();
            
            return new SubscriptionResponseDto { Success = true, SubscriptionId = subscription.Id };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription purchase");
            return new SubscriptionResponseDto { Success = false, Message = ex.Message };
        }
    }
    
    public async Task<SubscriptionResponseDto> ProcessSubscriptionCancellationAsync(RevenueCatEvent eventData)
    {
        try
        {
            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.OriginalTransactionId == eventData.OriginalTransactionId);
            
            if (subscription == null)
            {
                _logger.LogWarning($"Could not find subscription for cancellation: {eventData.OriginalTransactionId}");
                return new SubscriptionResponseDto { Success = false, Message = "Subscription not found" };
            }
            
            // Update subscription status
            subscription.AutoRenew = false;
            subscription.Status = "canceled";
            subscription.UpdatedAt = DateTime.UtcNow;
            
            // Record transaction
            var transaction = new SubscriptionTransaction
            {
                SubscriptionId = subscription.Id,
                TransactionId = Guid.NewGuid().ToString(),
                Amount = 0,
                Currency = eventData.Currency,
                Status = "completed",
                TransactionDate = DateTimeOffset.FromUnixTimeMilliseconds(eventData.PurchasedAtMs).UtcDateTime,
                RevenueCatEventId = eventData.Id,
                StoreTransactionId = eventData.TransactionId,
                TransactionType = "cancellation",
                RawData = JsonSerializer.Serialize(eventData),
                CreatedAt = DateTime.UtcNow
            };
            
            _db.SubscriptionTransactions.Add(transaction);
            await _db.SaveChangesAsync();
            
            return new SubscriptionResponseDto { Success = true, SubscriptionId = subscription.Id };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription cancellation");
            return new SubscriptionResponseDto { Success = false, Message = ex.Message };
        }
    }
    
    public async Task<SubscriptionResponseDto> ProcessSubscriptionExpirationAsync(RevenueCatEvent eventData)
    {
        try
        {
            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.OriginalTransactionId == eventData.OriginalTransactionId);
            
            if (subscription == null)
            {
                _logger.LogWarning($"Could not find subscription for expiration: {eventData.OriginalTransactionId}");
                return new SubscriptionResponseDto { Success = false, Message = "Subscription not found" };
            }
            
            // Update subscription status
            subscription.Status = "expired";
            subscription.ExpiryDate = DateTimeOffset.FromUnixTimeMilliseconds(eventData.PurchasedAtMs).UtcDateTime;
            subscription.UpdatedAt = DateTime.UtcNow;
            
            // Record transaction
            var transaction = new SubscriptionTransaction
            {
                SubscriptionId = subscription.Id,
                TransactionId = Guid.NewGuid().ToString(),
                Amount = 0,
                Currency = eventData.Currency,
                Status = "completed",
                TransactionDate = DateTimeOffset.FromUnixTimeMilliseconds(eventData.PurchasedAtMs).UtcDateTime,
                RevenueCatEventId = eventData.Id,
                StoreTransactionId = eventData.TransactionId,
                TransactionType = "expiration",
                RawData = JsonSerializer.Serialize(eventData),
                CreatedAt = DateTime.UtcNow
            };
            
            _db.SubscriptionTransactions.Add(transaction);
            await _db.SaveChangesAsync();
            
            return new SubscriptionResponseDto { Success = true, SubscriptionId = subscription.Id };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription expiration");
            return new SubscriptionResponseDto { Success = false, Message = ex.Message };
        }
    }
    
    public async Task<bool> SyncSubscriptionStatusAsync(int userId, string revenueCatId, Dictionary<string, object>? subscriptionData)
    {
        try
        {
            // Link user to RevenueCat ID if not already linked
            await LinkUserToRevenueCatIdAsync(userId, revenueCatId);
            
            if (subscriptionData == null)
            {
                return false;
            }
            
            // Parse subscription data and update local database
            // This is a simplified implementation - in a real app, you would parse the actual RevenueCat subscription data
            bool hasActiveSubscription = false;
            string? productId = null;
            DateTime? expiryDate = null;
            
            if (subscriptionData.TryGetValue("active", out var active) && active is bool activeValue)
            {
                hasActiveSubscription = activeValue;
            }
            
            if (subscriptionData.TryGetValue("product_id", out var product) && product is string productValue)
            {
                productId = productValue;
            }
            
            if (subscriptionData.TryGetValue("expires_at", out var expires) && expires is long expiresValue)
            {
                expiryDate = DateTimeOffset.FromUnixTimeMilliseconds(expiresValue).UtcDateTime;
            }
            
            if (hasActiveSubscription && !string.IsNullOrEmpty(productId))
            {
                // Check if subscription already exists
                var subscription = await _db.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.RevenueCatId == revenueCatId);
                
                if (subscription == null)
                {
                    // Create new subscription
                    subscription = new Subscription
                    {
                        UserId = userId,
                        ProductId = productId,
                        Status = "active",
                        Platform = "unknown", // Would need to be determined from the data
                        PurchaseToken = Guid.NewGuid().ToString(),
                        ExpiryDate = expiryDate,
                        AutoRenew = true,
                        RevenueCatId = revenueCatId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    _db.Subscriptions.Add(subscription);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // Update existing subscription
                    subscription.Status = "active";
                    subscription.ProductId = productId;
                    subscription.ExpiryDate = expiryDate;
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
                
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing subscription status for user {UserId}", userId);
            return false;
        }
    }
    
    public async Task<int?> GetOrCreateUserIdFromRevenueCatIdAsync(string revenueCatId)
    {
        // First, check if there's a mapping
        var mapping = await _db.UserRevenueCatMappings
            .FirstOrDefaultAsync(m => m.RevenueCatId == revenueCatId);
        
        if (mapping != null)
        {
            return mapping.UserId;
        }
        
        // If no mapping exists, check if there's a subscription with this RevenueCat ID
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.RevenueCatId == revenueCatId);
        
        if (subscription != null)
        {
            // Create a mapping for future use
            await LinkUserToRevenueCatIdAsync(subscription.UserId, revenueCatId);
            return subscription.UserId;
        }
        
        // No existing user found for this RevenueCat ID
        return null;
    }
    
    public async Task<bool> LinkUserToRevenueCatIdAsync(int userId, string revenueCatId)
    {
        try
        {
            // Check if mapping already exists
            var existingMapping = await _db.UserRevenueCatMappings
                .FirstOrDefaultAsync(m => m.UserId == userId && m.RevenueCatId == revenueCatId);
            
            if (existingMapping != null)
            {
                // Mapping already exists
                return true;
            }
            
            // Create new mapping
            var mapping = new UserRevenueCatMapping
            {
                UserId = userId,
                RevenueCatId = revenueCatId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _db.UserRevenueCatMappings.Add(mapping);
            await _db.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking user {UserId} to RevenueCat ID {RevenueCatId}", userId, revenueCatId);
            return false;
        }
    }
    
    private string DeterminePlatform(string store)
    {
        return store.ToUpperInvariant() switch
        {
            "APP_STORE" => "ios",
            "PLAY_STORE" => "android",
            "STRIPE" => "web",
            _ => "unknown"
        };
    }
    
    #endregion
    
    #region Free Subscription
    
    public async Task<SubscriptionResponseDto> CreateFreeSubscriptionAsync(int userId, FreeSubscriptionRequestDto request)
    {
        try
        {
            // Check if user already has an active subscription
            var existingSubscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && 
                                         s.Status == "active" && 
                                         (s.ExpiryDate == null || s.ExpiryDate > DateTime.UtcNow));
            
            if (existingSubscription != null)
            {
                _logger.LogInformation("User {UserId} already has an active subscription", userId);
                return new SubscriptionResponseDto 
                { 
                    Success = true, 
                    Message = "User already has an active subscription",
                    SubscriptionId = existingSubscription.Id 
                };
            }
            
            // Deactivate any expired subscriptions
            var expiredSubscriptions = await _db.Subscriptions
                .Where(s => s.UserId == userId && s.Status == "active")
                .ToListAsync();
            
            foreach (var sub in expiredSubscriptions)
            {
                sub.Status = "inactive";
                sub.UpdatedAt = DateTime.UtcNow;
            }
            
            // Create free subscription
            var freeSubscription = new Subscription
            {
                UserId = userId,
                ProductId = "free",
                Status = "active",
                Platform = "app",
                PurchaseToken = $"free_{Guid.NewGuid()}",
                TransactionId = null,
                OriginalTransactionId = null,
                PurchaseDate = DateTime.UtcNow,
                ExpiryDate = null, // Free subscriptions don't expire
                AutoRenew = false, // Free subscriptions don't auto-renew
                RevenueCatId = null,
                EntitlementIdentifier = "free_access",
                OfferIdentifier = request.Source ?? "paywall_skip",
                IsSandbox = false,
                RenewalInfo = JsonSerializer.Serialize(new { plan_type = "free", source = request.Source }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _db.Subscriptions.Add(freeSubscription);
            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Free subscription created for user {UserId}", userId);
            
            return new SubscriptionResponseDto 
            { 
                Success = true, 
                Message = "Free subscription registered successfully",
                SubscriptionId = freeSubscription.Id 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating free subscription for user {UserId}", userId);
            return new SubscriptionResponseDto 
            { 
                Success = false, 
                Message = "Failed to register free subscription" 
            };
        }
    }
    
    #endregion
    
    #region Enhanced Subscription Methods
    
    public async Task<EnhancedSubscriptionStatusDto> GetEnhancedStatusAsync(int userId)
    {
        var now = DateTime.UtcNow;
        
        // Find active subscription (including free subscriptions with null expiry)
        var sub = await _db.Subscriptions
            .Where(s => s.UserId == userId && 
                       s.Status == "active" && 
                       (s.ExpiryDate == null || s.ExpiryDate > now))
            .OrderByDescending(s => s.ExpiryDate ?? DateTime.MaxValue)
            .FirstOrDefaultAsync();

        if (sub == null)
        {
            return new EnhancedSubscriptionStatusDto 
            { 
                IsActive = false,
                SubscriptionStatus = "inactive",
                SubscriptionType = null,
                ProductId = null,
                StartDate = null,
                ExpirationDate = null,
                Entitlements = new Dictionary<string, EntitlementDto>()
            };
        }

        // Determine subscription type
        var subscriptionType = sub.ProductId == "free" ? "free" : "premium";
        
        // Build entitlements
        var entitlements = new Dictionary<string, EntitlementDto>();
        
        if (!string.IsNullOrEmpty(sub.EntitlementIdentifier))
        {
            var entitlement = new EntitlementDto
            {
                Identifier = sub.EntitlementIdentifier,
                IsActive = true,
                WillRenew = sub.AutoRenew,
                PeriodType = "normal",
                LatestPurchaseDate = sub.PurchaseDate,
                OriginalPurchaseDate = sub.PurchaseDate,
                ExpirationDate = sub.ExpiryDate,
                Store = MapPlatformToStore(sub.Platform),
                ProductIdentifier = sub.ProductId,
                IsSandbox = sub.IsSandbox
            };
            
            entitlements[sub.EntitlementIdentifier] = entitlement;
        }
        else if (sub.ProductId == "free")
        {
            // Add free entitlement
            entitlements["free_access"] = new EntitlementDto
            {
                Identifier = "free_access",
                IsActive = true,
                WillRenew = false,
                PeriodType = "normal",
                LatestPurchaseDate = sub.PurchaseDate,
                OriginalPurchaseDate = sub.PurchaseDate,
                ExpirationDate = null,
                Store = "app",
                ProductIdentifier = "free",
                IsSandbox = false
            };
        }

        return new EnhancedSubscriptionStatusDto
        {
            IsActive = true,
            SubscriptionStatus = "active",
            SubscriptionType = subscriptionType,
            ProductId = sub.ProductId,
            StartDate = sub.PurchaseDate,
            ExpirationDate = sub.ExpiryDate,
            Entitlements = entitlements
        };
    }
    
    public async Task<SubscriptionPlansResponseDto> GetSubscriptionPlansAsync()
    {
        // Return hardcoded plans - in a real app, these might come from a database or configuration
        var plans = new List<SubscriptionPlanDto>
        {
            new SubscriptionPlanDto
            {
                Id = "monthly",
                ProductId = "com.yourapp.monthly",
                Name = "Monthly Premium",
                Description = "Premium features billed monthly",
                Price = 9.99m,
                Currency = "USD",
                Period = "month",
                Features = new List<string>
                {
                    "Unlimited access",
                    "Priority support",
                    "Advanced analytics"
                }
            },
            new SubscriptionPlanDto
            {
                Id = "yearly",
                ProductId = "com.yourapp.yearly",
                Name = "Annual Premium",
                Description = "Premium features billed annually (2 months free)",
                Price = 99.99m,
                Currency = "USD",
                Period = "year",
                Features = new List<string>
                {
                    "Unlimited access",
                    "Priority support",
                    "Advanced analytics",
                    "2 months free"
                }
            }
        };
        
        return await Task.FromResult(new SubscriptionPlansResponseDto { Plans = plans });
    }
    
    public async Task<SubscriptionResponseDto> CancelWithReasonAsync(int userId, string? reason)
    {
        var now = DateTime.UtcNow;
        
        // Find active subscription
        var sub = await _db.Subscriptions
            .Where(s => s.UserId == userId && 
                       s.Status == "active" && 
                       (s.ExpiryDate == null || s.ExpiryDate > now))
            .FirstOrDefaultAsync();
            
        if (sub == null)
        {
            return new SubscriptionResponseDto 
            { 
                Success = false, 
                Message = "No active subscription to cancel" 
            };
        }

        // Update subscription status
        sub.Status = "canceled";
        sub.AutoRenew = false;
        sub.UpdatedAt = now;
        
        // Store cancellation reason in RenewalInfo
        if (!string.IsNullOrEmpty(reason))
        {
            var renewalInfo = new Dictionary<string, object>
            {
                { "cancellation_reason", reason },
                { "cancelled_at", now }
            };
            sub.RenewalInfo = JsonSerializer.Serialize(renewalInfo);
        }
        
        await _db.SaveChangesAsync();

        return new SubscriptionResponseDto 
        { 
            Success = true, 
            Message = "Subscription cancelled successfully",
            SubscriptionId = sub.Id 
        };
    }
    
    public async Task<SubscriptionResponseDto> SyncRevenueCatPurchaseAsync(int userId, SyncRevenueCatRequestDto request)
    {
        try
        {
            // Link user to RevenueCat ID
            await LinkUserToRevenueCatIdAsync(userId, request.RevenueCatUserId);
            
            // Check if subscription already exists
            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.TransactionId == request.TransactionId);
            
            if (subscription == null)
            {
                // Create new subscription
                subscription = new Subscription
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    Status = "active",
                    Platform = DeterminePlatform(request.Store),
                    PurchaseToken = request.TransactionId,
                    TransactionId = request.TransactionId,
                    OriginalTransactionId = request.TransactionId,
                    PurchaseDate = request.PurchaseDate,
                    ExpiryDate = request.ExpirationDate,
                    AutoRenew = true,
                    RevenueCatId = request.RevenueCatUserId,
                    EntitlementIdentifier = ExtractEntitlementIdentifier(request.Entitlements),
                    IsSandbox = false,
                    RenewalInfo = request.Entitlements != null ? JsonSerializer.Serialize(request.Entitlements) : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _db.Subscriptions.Add(subscription);
            }
            else
            {
                // Update existing subscription
                subscription.ExpiryDate = request.ExpirationDate;
                subscription.Status = "active";
                subscription.AutoRenew = true;
                subscription.RevenueCatId = request.RevenueCatUserId;
                subscription.RenewalInfo = request.Entitlements != null ? JsonSerializer.Serialize(request.Entitlements) : subscription.RenewalInfo;
                subscription.UpdatedAt = DateTime.UtcNow;
            }
            
            await _db.SaveChangesAsync();
            
            return new SubscriptionResponseDto 
            { 
                Success = true, 
                Message = "Subscription synced successfully",
                SubscriptionId = subscription.Id 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing RevenueCat purchase for user {UserId}", userId);
            return new SubscriptionResponseDto 
            { 
                Success = false, 
                Message = "Failed to sync subscription" 
            };
        }
    }
    
    private string MapPlatformToStore(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "ios" => "app_store",
            "android" => "play_store",
            "web" => "stripe",
            _ => "unknown"
        };
    }
    
    private string ExtractEntitlementIdentifier(Dictionary<string, object>? entitlements)
    {
        if (entitlements == null || entitlements.Count == 0)
            return "premium";
            
        // Get the first entitlement key
        return entitlements.Keys.FirstOrDefault() ?? "premium";
    }
    
    public async Task<List<SubscriptionTransactionDto>> GetUserTransactionsAsync(int userId)
    {
        // Get user's subscriptions
        var subscriptionIds = await _db.Subscriptions
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .ToListAsync();
        
        if (!subscriptionIds.Any())
            return new List<SubscriptionTransactionDto>();
        
        // Get transactions for user's subscriptions
        var transactions = await _db.SubscriptionTransactions
            .Include(t => t.Subscription)
            .Where(t => subscriptionIds.Contains(t.SubscriptionId))
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new SubscriptionTransactionDto
            {
                Id = t.Id,
                TransactionId = t.TransactionId,
                Amount = t.Amount,
                Currency = t.Currency,
                Status = t.Status,
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType,
                ProductId = t.Subscription.ProductId
            })
            .ToListAsync();
        
        return transactions;
    }
    
    #endregion

    #region Quota Management

    public async Task<QuotaStatusDto> GetQuotaStatusAsync(int userId, string quotaType)
    {
        var subscription = await _db.UserSubscriptions
            .Include(s => s.UsageQuotas)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

        if (subscription == null)
            throw new Exception("No active subscription found");

        var quota = subscription.UsageQuotas.FirstOrDefault(q => q.QuotaType == quotaType);
        if (quota == null)
            throw new Exception($"Quota type {quotaType} not found");

        return new QuotaStatusDto
        {
            QuotaType = quota.QuotaType,
            Limit = quota.QuotaLimit,
            Used = quota.QuotaUsed,
            Remaining = quota.QuotaLimit - quota.QuotaUsed,
            PercentageUsed = quota.QuotaLimit > 0 ? (double)quota.QuotaUsed / quota.QuotaLimit * 100 : 0,
            PeriodStart = quota.PeriodStart,
            PeriodEnd = quota.PeriodEnd,
            IsExceeded = quota.QuotaUsed >= quota.QuotaLimit,
            IsNearLimit = quota.QuotaLimit > 0 && (double)quota.QuotaUsed / quota.QuotaLimit >= 0.8
        };
    }

    public async Task<Dictionary<string, QuotaStatusDto>> GetAllQuotasAsync(int userId)
    {
        var subscription = await _db.UserSubscriptions
            .Include(s => s.UsageQuotas)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

        if (subscription == null)
            return new Dictionary<string, QuotaStatusDto>();

        return subscription.UsageQuotas.ToDictionary(
            q => q.QuotaType,
            q => new QuotaStatusDto
            {
                QuotaType = q.QuotaType,
                Limit = q.QuotaLimit,
                Used = q.QuotaUsed,
                Remaining = q.QuotaLimit - q.QuotaUsed,
                PercentageUsed = q.QuotaLimit > 0 ? (double)q.QuotaUsed / q.QuotaLimit * 100 : 0,
                PeriodStart = q.PeriodStart,
                PeriodEnd = q.PeriodEnd,
                IsExceeded = q.QuotaUsed >= q.QuotaLimit,
                IsNearLimit = q.QuotaLimit > 0 && (double)q.QuotaUsed / q.QuotaLimit >= 0.8
            });
    }

    public async Task<bool> CheckQuotaAvailableAsync(int userId, string quotaType, int amount = 1)
    {
        var subscription = await _db.UserSubscriptions
            .Include(s => s.UsageQuotas)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

        if (subscription == null)
            return false;

        var quota = subscription.UsageQuotas.FirstOrDefault(q => q.QuotaType == quotaType);
        if (quota == null)
            return false;

        return (quota.QuotaUsed + amount) <= quota.QuotaLimit;
    }

    public async Task<bool> ConsumeQuotaAsync(int userId, string quotaType, int amount = 1, string? resourceId = null, string? resourceType = null, int? projectId = null)
    {
        var subscription = await _db.UserSubscriptions
            .Include(s => s.UsageQuotas)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

        if (subscription == null)
            return false;

        var quota = subscription.UsageQuotas.FirstOrDefault(q => q.QuotaType == quotaType);
        if (quota == null)
            return false;

        if ((quota.QuotaUsed + amount) > quota.QuotaLimit)
            return false;

        quota.QuotaUsed += amount;
        quota.UpdatedAt = DateTime.UtcNow;

        // Log usage
        var usageLog = new QuotaUsageLog
        {
            UserId = userId,
            ProjectId = projectId,
            QuotaType = quotaType,
            AmountUsed = amount,
            ResourceId = resourceId,
            ResourceType = resourceType,
            CreatedAt = DateTime.UtcNow
        };

        _db.QuotaUsageLogs.Add(usageLog);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task ResetMonthlyQuotasAsync(int userId)
    {
        var subscription = await _db.UserSubscriptions
            .Include(s => s.UsageQuotas)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

        if (subscription == null)
            return;

        foreach (var quota in subscription.UsageQuotas.Where(q => q.IsResetMonthly))
        {
            quota.QuotaUsed = 0;
            quota.PeriodStart = DateTime.UtcNow;
            quota.PeriodEnd = DateTime.UtcNow.AddMonths(1);
            quota.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<QuotaUsageLog>> GetQuotaUsageHistoryAsync(int userId, string? quotaType = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _db.QuotaUsageLogs.Where(l => l.UserId == userId);

        if (!string.IsNullOrEmpty(quotaType))
            query = query.Where(l => l.QuotaType == quotaType);

        if (startDate.HasValue)
            query = query.Where(l => l.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.CreatedAt <= endDate.Value);

        return await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
    }

    #endregion

    #region Feature Access Control

    public async Task<bool> HasFeatureAccessAsync(int userId, string featureName)
    {
        var plan = await GetUserSubscriptionPlanAsync(userId);
        if (plan == null)
            return false;

        return featureName switch
        {
            "CompetitorTracking" => plan.AllowCompetitorTracking,
            "AdvancedAnalytics" => plan.AllowAdvancedAnalytics,
            "ApiAccess" => plan.AllowApiAccess,
            "WhiteLabel" => plan.AllowWhiteLabel,
            "PrioritySupport" => plan.AllowPrioritySupport,
            "CustomIntegrations" => plan.AllowCustomIntegrations,
            _ => false
        };
    }

    public async Task<FeatureLimitsDto> GetFeatureLimitsAsync(int userId)
    {
        var plan = await GetUserSubscriptionPlanAsync(userId);
        if (plan == null)
            return new FeatureLimitsDto();

        return new FeatureLimitsDto
        {
            MaxProjects = plan.MaxProjects,
            MaxPromptsPerProject = plan.MaxPromptsPerProject,
            MaxVisibilityChecksPerMonth = plan.MaxVisibilityChecksPerMonth,
            MaxSiteAuditsPerMonth = plan.MaxSiteAuditsPerMonth,
            MaxContentArticles = plan.MaxContentArticles,
            MaxLlmRequestsPerMonth = plan.MaxLlmRequestsPerMonth,
            MaxLlmTokensPerMonth = plan.MaxLlmTokensPerMonth,
            AllowCompetitorTracking = plan.AllowCompetitorTracking,
            AllowAdvancedAnalytics = plan.AllowAdvancedAnalytics,
            AllowApiAccess = plan.AllowApiAccess,
            AllowWhiteLabel = plan.AllowWhiteLabel,
            AllowPrioritySupport = plan.AllowPrioritySupport,
            AllowCustomIntegrations = plan.AllowCustomIntegrations
        };
    }

    public async Task<SubscriptionPlan?> GetUserSubscriptionPlanAsync(int userId)
    {
        var subscription = await _db.UserSubscriptions
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

        return subscription?.SubscriptionPlan;
    }

    #endregion

    #region Admin Methods

    public async Task<List<SubscriptionPlan>> GetAllPlansAsync()
    {
        return await _db.SubscriptionPlans
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan> CreatePlanAsync(SubscriptionPlan plan)
    {
        plan.CreatedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;
        
        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync();
        
        return plan;
    }

    public async Task<SubscriptionPlan> UpdatePlanAsync(int planId, SubscriptionPlan plan)
    {
        var existing = await _db.SubscriptionPlans.FindAsync(planId);
        if (existing == null)
            throw new Exception("Plan not found");

        existing.Name = plan.Name;
        existing.DisplayName = plan.DisplayName;
        existing.Description = plan.Description;
        existing.MonthlyPrice = plan.MonthlyPrice;
        existing.YearlyPrice = plan.YearlyPrice;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeletePlanAsync(int planId)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
            return false;

        _db.SubscriptionPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<UserSubscription> AssignPlanToUserAsync(int userId, int planId, string billingCycle = "Monthly")
    {
        var plan = await _db.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
            throw new Exception("Plan not found");

        var subscription = new UserSubscription
        {
            UserId = userId,
            SubscriptionPlanId = planId,
            Status = "Active",
            BillingCycle = billingCycle,
            StartDate = DateTime.UtcNow,
            CurrentPrice = billingCycle == "Yearly" ? plan.YearlyPrice : plan.MonthlyPrice,
            NextBillingDate = billingCycle == "Yearly" ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.UserSubscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        return subscription;
    }

    public async Task<List<UserSubscription>> GetAllUserSubscriptionsAsync(int page = 1, int pageSize = 50)
    {
        return await _db.UserSubscriptions
            .Include(s => s.User)
            .Include(s => s.SubscriptionPlan)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<SubscriptionMetricsDto> GetSubscriptionMetricsAsync()
    {
        var total = await _db.UserSubscriptions.CountAsync();
        var active = await _db.UserSubscriptions.CountAsync(s => s.Status == "Active");
        var trial = await _db.UserSubscriptions.CountAsync(s => s.IsTrialActive);
        var cancelled = await _db.UserSubscriptions.CountAsync(s => s.Status == "Cancelled");
        var expired = await _db.UserSubscriptions.CountAsync(s => s.Status == "Expired");

        var monthlyRevenue = await _db.UserSubscriptions
            .Where(s => s.Status == "Active" && s.BillingCycle == "Monthly")
            .SumAsync(s => s.CurrentPrice);

        var yearlyRevenue = await _db.UserSubscriptions
            .Where(s => s.Status == "Active" && s.BillingCycle == "Yearly")
            .SumAsync(s => s.CurrentPrice);

        var mrr = monthlyRevenue + (yearlyRevenue / 12);

        return new SubscriptionMetricsDto
        {
            TotalSubscriptions = total,
            ActiveSubscriptions = active,
            TrialSubscriptions = trial,
            CancelledSubscriptions = cancelled,
            ExpiredSubscriptions = expired,
            MonthlyRecurringRevenue = mrr,
            AnnualRecurringRevenue = mrr * 12,
            ChurnRate = total > 0 ? (double)cancelled / total * 100 : 0,
            AverageRevenuePerUser = active > 0 ? (double)mrr / active : 0
        };
    }

    #endregion
}
