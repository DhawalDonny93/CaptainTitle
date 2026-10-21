using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

public interface ISubscriptionService
{
    // Existing methods
    Task<SubscriptionStatusDto> GetStatusAsync(int userId);
    Task<SubscriptionResponseDto> RegisterAsync(int userId, RegisterSubscriptionDto request);
    Task<SubscriptionResponseDto> CancelAsync(int userId, int subscriptionId);
    
    // RevenueCat specific methods
    Task ProcessRevenueCatWebhookAsync(RevenueCatWebhookPayload payload);
    Task<SubscriptionResponseDto> ProcessSubscriptionPurchaseAsync(RevenueCatEvent eventData);
    Task<SubscriptionResponseDto> ProcessSubscriptionCancellationAsync(RevenueCatEvent eventData);
    Task<SubscriptionResponseDto> ProcessSubscriptionExpirationAsync(RevenueCatEvent eventData);
    Task<bool> SyncSubscriptionStatusAsync(int userId, string revenueCatId, Dictionary<string, object>? subscriptionData);
    Task<int?> GetOrCreateUserIdFromRevenueCatIdAsync(string revenueCatId);
    Task<bool> LinkUserToRevenueCatIdAsync(int userId, string revenueCatId);
    
    // Free subscription method
    Task<SubscriptionResponseDto> CreateFreeSubscriptionAsync(int userId, FreeSubscriptionRequestDto request);
    
    // Enhanced subscription methods
    Task<EnhancedSubscriptionStatusDto> GetEnhancedStatusAsync(int userId);
    Task<SubscriptionPlansResponseDto> GetSubscriptionPlansAsync();
    Task<List<SubscriptionTransactionDto>> GetUserTransactionsAsync(int userId);
    Task<SubscriptionResponseDto> CancelWithReasonAsync(int userId, string? reason);
    Task<SubscriptionResponseDto> SyncRevenueCatPurchaseAsync(int userId, SyncRevenueCatRequestDto request);
    
    // Quota Management Methods
    Task<QuotaStatusDto> GetQuotaStatusAsync(int userId, string quotaType);
    Task<Dictionary<string, QuotaStatusDto>> GetAllQuotasAsync(int userId);
    Task<bool> CheckQuotaAvailableAsync(int userId, string quotaType, int amount = 1);
    Task<bool> ConsumeQuotaAsync(int userId, string quotaType, int amount = 1, string? resourceId = null, string? resourceType = null, int? projectId = null);
    Task ResetMonthlyQuotasAsync(int userId);
    Task<List<QuotaUsageLog>> GetQuotaUsageHistoryAsync(int userId, string? quotaType = null, DateTime? startDate = null, DateTime? endDate = null);
    
    // Feature Access Control
    Task<bool> HasFeatureAccessAsync(int userId, string featureName);
    Task<FeatureLimitsDto> GetFeatureLimitsAsync(int userId);
    Task<SubscriptionPlan?> GetUserSubscriptionPlanAsync(int userId);
    
    // Admin Methods
    Task<List<SubscriptionPlan>> GetAllPlansAsync();
    Task<SubscriptionPlan> CreatePlanAsync(SubscriptionPlan plan);
    Task<SubscriptionPlan> UpdatePlanAsync(int planId, SubscriptionPlan plan);
    Task<bool> DeletePlanAsync(int planId);
    Task<UserSubscription> AssignPlanToUserAsync(int userId, int planId, string billingCycle = "Monthly");
    Task<List<UserSubscription>> GetAllUserSubscriptionsAsync(int page = 1, int pageSize = 50);
    Task<SubscriptionMetricsDto> GetSubscriptionMetricsAsync();
}
