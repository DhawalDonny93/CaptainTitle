using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Simplified subscription service for web-only use (no mobile/RevenueCat)
/// </summary>
public interface IWebSubscriptionService
{
    // Web Subscription Management
    Task<UserSubscription?> GetActiveSubscriptionAsync(int userId);
    Task<UserSubscription> AssignFreePlanAsync(int userId);
    Task<UserSubscription> AssignPaidPlanAsync(int userId, int planId, string billingCycle, string stripeSubscriptionId, string stripeCustomerId);
    Task<bool> HasActiveSubscriptionAsync(int userId);
    
    // Plan Management
    Task<List<SubscriptionPlan>> GetAllPlansAsync();
    Task<SubscriptionPlan?> GetPlanByIdAsync(int planId);
    
    // Quota Management
    Task InitializeQuotasAsync(int userSubscriptionId, SubscriptionPlan plan);
    Task<Dictionary<string, QuotaStatusDto>> GetAllQuotasAsync(int userId);
    Task<bool> CheckQuotaAvailableAsync(int userId, string quotaType, int amount = 1);
    Task<bool> ConsumeQuotaAsync(int userId, string quotaType, int amount = 1, string? resourceId = null);
    Task ResetMonthlyQuotasAsync(int userId);
    
    // Feature Access
    Task<bool> HasFeatureAccessAsync(int userId, string featureName);
    Task<SubscriptionPlan?> GetUserSubscriptionPlanAsync(int userId);
    
    // Admin Methods
    Task<SubscriptionPlan> CreatePlanAsync(SubscriptionPlan plan);
    Task<SubscriptionPlan> UpdatePlanAsync(int planId, SubscriptionPlan plan);
    Task<bool> DeletePlanAsync(int planId);
    Task<UserSubscription> AssignPlanToUserAsync(int userId, int planId, string billingCycle);
    Task<List<UserSubscription>> GetAllUserSubscriptionsAsync(int page = 1, int pageSize = 50);
    Task<SubscriptionMetricsDto> GetSubscriptionMetricsAsync();
}
