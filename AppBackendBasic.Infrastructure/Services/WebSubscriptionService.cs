using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;

namespace AppBackendBasic.Infrastructure.Services;

/// <summary>
/// Simplified subscription service for web-only use (no mobile/RevenueCat)
/// </summary>
public class WebSubscriptionService : IWebSubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebSubscriptionService> _logger;

    public WebSubscriptionService(
        ApplicationDbContext context,
        ILogger<WebSubscriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserSubscription?> GetActiveSubscriptionAsync(int userId)
    {
        return await _context.UserSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Include(s => s.UsageQuotas)
            .Where(s => s.UserId == userId && s.Status == "Active")
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId)
    {
        return await _context.UserSubscriptions
            .AnyAsync(s => s.UserId == userId && s.Status == "Active");
    }

    public async Task<UserSubscription> AssignFreePlanAsync(int userId)
    {
        var freePlan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.MonthlyPrice == 0);

        if (freePlan == null)
        {
            throw new InvalidOperationException("Free plan not found");
        }

        var userSubscription = new UserSubscription
        {
            UserId = userId,
            SubscriptionPlanId = freePlan.Id,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            BillingCycle = "Monthly"
        };

        await _context.UserSubscriptions.AddAsync(userSubscription);
        await _context.SaveChangesAsync();

        await InitializeQuotasAsync(userSubscription.Id, freePlan);

        _logger.LogInformation("Free plan assigned to user {UserId}", userId);

        return userSubscription;
    }

    public async Task<UserSubscription> AssignPaidPlanAsync(
        int userId, 
        int planId, 
        string billingCycle, 
        string stripeSubscriptionId,
        string stripeCustomerId)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
        {
            throw new InvalidOperationException($"Plan {planId} not found");
        }

        var userSubscription = new UserSubscription
        {
            UserId = userId,
            SubscriptionPlanId = planId,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            EndDate = billingCycle == "Yearly" 
                ? DateTime.UtcNow.AddYears(1) 
                : DateTime.UtcNow.AddMonths(1),
            BillingCycle = billingCycle,
            PaymentProvider = "Stripe",
            PaymentProviderId = stripeSubscriptionId
        };

        await _context.UserSubscriptions.AddAsync(userSubscription);
        await _context.SaveChangesAsync();

        await InitializeQuotasAsync(userSubscription.Id, plan);

        _logger.LogInformation("Paid plan {PlanId} assigned to user {UserId}", planId, userId);

        return userSubscription;
    }

    public async Task<List<SubscriptionPlan>> GetAllPlansAsync()
    {
        return await _context.SubscriptionPlans
            .OrderBy(p => p.MonthlyPrice)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan?> GetPlanByIdAsync(int planId)
    {
        return await _context.SubscriptionPlans.FindAsync(planId);
    }

    public async Task InitializeQuotasAsync(int userSubscriptionId, SubscriptionPlan plan)
    {
        var quotas = new List<UsageQuota>
        {
            new UsageQuota
            {
                UserSubscriptionId = userSubscriptionId,
                QuotaType = "VisibilityChecks",
                QuotaLimit = plan.MaxVisibilityChecksPerMonth,
                QuotaUsed = 0,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1)
            },
            new UsageQuota
            {
                UserSubscriptionId = userSubscriptionId,
                QuotaType = "SiteAudits",
                QuotaLimit = plan.MaxSiteAuditsPerMonth,
                QuotaUsed = 0,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1)
            },
            new UsageQuota
            {
                UserSubscriptionId = userSubscriptionId,
                QuotaType = "LlmRequests",
                QuotaLimit = plan.MaxLlmRequestsPerMonth,
                QuotaUsed = 0,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1)
            },
            new UsageQuota
            {
                UserSubscriptionId = userSubscriptionId,
                QuotaType = "LlmTokens",
                QuotaLimit = plan.MaxLlmTokensPerMonth,
                QuotaUsed = 0,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1)
            }
        };

        await _context.UsageQuotas.AddRangeAsync(quotas);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Quotas initialized for subscription {SubscriptionId}", userSubscriptionId);
    }

    public async Task<Dictionary<string, QuotaStatusDto>> GetAllQuotasAsync(int userId)
    {
        var subscription = await GetActiveSubscriptionAsync(userId);
        if (subscription == null)
        {
            return new Dictionary<string, QuotaStatusDto>();
        }

        var quotas = await _context.UsageQuotas
            .Where(q => q.UserSubscriptionId == subscription.Id)
            .ToListAsync();

        return quotas.ToDictionary(
            q => q.QuotaType,
            q => new QuotaStatusDto
            {
                QuotaType = q.QuotaType,
                Limit = q.QuotaLimit,
                Used = q.QuotaUsed,
                Remaining = q.QuotaLimit == -1 ? -1 : q.QuotaLimit - q.QuotaUsed
            }
        );
    }

    public async Task<bool> CheckQuotaAvailableAsync(int userId, string quotaType, int amount = 1)
    {
        var subscription = await GetActiveSubscriptionAsync(userId);
        if (subscription == null)
        {
            return false;
        }

        var quota = await _context.UsageQuotas
            .FirstOrDefaultAsync(q => q.UserSubscriptionId == subscription.Id && q.QuotaType == quotaType);

        if (quota == null)
        {
            return false;
        }

        // Unlimited quota
        if (quota.QuotaLimit == -1)
        {
            return true;
        }

        return (quota.QuotaUsed + amount) <= quota.QuotaLimit;
    }

    public async Task<bool> ConsumeQuotaAsync(int userId, string quotaType, int amount = 1, string? resourceId = null)
    {
        var subscription = await GetActiveSubscriptionAsync(userId);
        if (subscription == null)
        {
            _logger.LogWarning("No active subscription for user {UserId}", userId);
            return false;
        }

        var quota = await _context.UsageQuotas
            .FirstOrDefaultAsync(q => q.UserSubscriptionId == subscription.Id && q.QuotaType == quotaType);

        if (quota == null)
        {
            _logger.LogWarning("Quota type {QuotaType} not found for user {UserId}", quotaType, userId);
            return false;
        }

        // Unlimited quota
        if (quota.QuotaLimit == -1)
        {
            // Still log usage for unlimited plans
            await LogQuotaUsageAsync(subscription.Id, quotaType, amount, resourceId);
            return true;
        }

        // Check if quota available
        if ((quota.QuotaUsed + amount) > quota.QuotaLimit)
        {
            _logger.LogWarning("Quota exceeded for user {UserId}, type {QuotaType}", userId, quotaType);
            return false;
        }

        // Consume quota
        quota.QuotaUsed += amount;
        await _context.SaveChangesAsync();

        // Log usage
        await LogQuotaUsageAsync(subscription.Id, quotaType, amount, resourceId);

        _logger.LogInformation("Consumed {Amount} {QuotaType} for user {UserId}", amount, quotaType, userId);

        return true;
    }

    private async Task LogQuotaUsageAsync(int userSubscriptionId, string quotaType, int amount, string? resourceId)
    {
        // Get userId from subscription
        var subscription = await _context.UserSubscriptions.FindAsync(userSubscriptionId);
        if (subscription == null) return;

        var log = new QuotaUsageLog
        {
            UserId = subscription.UserId,
            QuotaType = quotaType,
            AmountUsed = amount,
            ResourceId = resourceId
        };

        await _context.QuotaUsageLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task ResetMonthlyQuotasAsync(int userId)
    {
        var subscription = await GetActiveSubscriptionAsync(userId);
        if (subscription == null)
        {
            return;
        }

        var quotas = await _context.UsageQuotas
            .Where(q => q.UserSubscriptionId == subscription.Id)
            .ToListAsync();

        foreach (var quota in quotas)
        {
            quota.QuotaUsed = 0;
            quota.PeriodStart = DateTime.UtcNow;
            quota.PeriodEnd = DateTime.UtcNow.AddMonths(1);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Monthly quotas reset for user {UserId}", userId);
    }

    public async Task<bool> HasFeatureAccessAsync(int userId, string featureName)
    {
        var plan = await GetUserSubscriptionPlanAsync(userId);
        if (plan == null)
        {
            return false;
        }

        return featureName switch
        {
            "CompetitorTracking" => plan.AllowCompetitorTracking,
            "AdvancedAnalytics" => plan.AllowAdvancedAnalytics,
            "ApiAccess" => plan.AllowApiAccess,
            "WhiteLabel" => plan.AllowWhiteLabel,
            "PrioritySupport" => plan.AllowPrioritySupport,
            _ => false
        };
    }

    public async Task<SubscriptionPlan?> GetUserSubscriptionPlanAsync(int userId)
    {
        var subscription = await GetActiveSubscriptionAsync(userId);
        return subscription?.SubscriptionPlan;
    }

    // Admin Methods
    public async Task<SubscriptionPlan> CreatePlanAsync(SubscriptionPlan plan)
    {
        await _context.SubscriptionPlans.AddAsync(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<SubscriptionPlan> UpdatePlanAsync(int planId, SubscriptionPlan plan)
    {
        var existingPlan = await _context.SubscriptionPlans.FindAsync(planId);
        if (existingPlan == null)
        {
            throw new InvalidOperationException($"Plan {planId} not found");
        }

        existingPlan.DisplayName = plan.DisplayName;
        existingPlan.Description = plan.Description;
        existingPlan.MonthlyPrice = plan.MonthlyPrice;
        existingPlan.YearlyPrice = plan.YearlyPrice;
        existingPlan.MaxProjects = plan.MaxProjects;
        existingPlan.MaxPromptsPerProject = plan.MaxPromptsPerProject;
        existingPlan.MaxVisibilityChecksPerMonth = plan.MaxVisibilityChecksPerMonth;
        existingPlan.MaxSiteAuditsPerMonth = plan.MaxSiteAuditsPerMonth;
        existingPlan.MaxContentArticles = plan.MaxContentArticles;
        existingPlan.MaxLlmRequestsPerMonth = plan.MaxLlmRequestsPerMonth;
        existingPlan.AllowCompetitorTracking = plan.AllowCompetitorTracking;
        existingPlan.AllowAdvancedAnalytics = plan.AllowAdvancedAnalytics;
        existingPlan.AllowApiAccess = plan.AllowApiAccess;
        existingPlan.AllowWhiteLabel = plan.AllowWhiteLabel;
        existingPlan.AllowPrioritySupport = plan.AllowPrioritySupport;

        await _context.SaveChangesAsync();
        return existingPlan;
    }

    public async Task<bool> DeletePlanAsync(int planId)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
        {
            return false;
        }

        // Check if any users have this plan
        var hasUsers = await _context.UserSubscriptions
            .AnyAsync(s => s.SubscriptionPlanId == planId && s.Status == "Active");

        if (hasUsers)
        {
            throw new InvalidOperationException("Cannot delete plan with active subscriptions");
        }

        _context.SubscriptionPlans.Remove(plan);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserSubscription> AssignPlanToUserAsync(int userId, int planId, string billingCycle)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
        {
            throw new InvalidOperationException($"Plan {planId} not found");
        }

        // Cancel existing subscriptions
        var existingSubscriptions = await _context.UserSubscriptions
            .Where(s => s.UserId == userId && s.Status == "Active")
            .ToListAsync();

        foreach (var sub in existingSubscriptions)
        {
            sub.Status = "Cancelled";
            sub.EndDate = DateTime.UtcNow;
        }

        // Create new subscription
        var userSubscription = new UserSubscription
        {
            UserId = userId,
            SubscriptionPlanId = planId,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            BillingCycle = billingCycle
        };

        await _context.UserSubscriptions.AddAsync(userSubscription);
        await _context.SaveChangesAsync();

        await InitializeQuotasAsync(userSubscription.Id, plan);

        _logger.LogInformation("Admin assigned plan {PlanId} to user {UserId}", planId, userId);

        return userSubscription;
    }

    public async Task<List<UserSubscription>> GetAllUserSubscriptionsAsync(int page = 1, int pageSize = 50)
    {
        return await _context.UserSubscriptions
            .Include(s => s.User)
            .Include(s => s.SubscriptionPlan)
            .OrderByDescending(s => s.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<SubscriptionMetricsDto> GetSubscriptionMetricsAsync()
    {
        var totalSubscriptions = await _context.UserSubscriptions.CountAsync();
        var activeSubscriptions = await _context.UserSubscriptions
            .CountAsync(s => s.Status == "Active");
        
        var subscriptionsByPlan = await _context.UserSubscriptions
            .Where(s => s.Status == "Active")
            .GroupBy(s => s.SubscriptionPlan.DisplayName)
            .Select(g => new { Plan = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalRevenue = await _context.UserSubscriptions
            .Where(s => s.Status == "Active" && s.SubscriptionPlan.MonthlyPrice > 0)
            .SumAsync(s => s.BillingCycle == "Yearly" 
                ? s.SubscriptionPlan.YearlyPrice 
                : s.SubscriptionPlan.MonthlyPrice);

        return new SubscriptionMetricsDto
        {
            TotalSubscriptions = totalSubscriptions,
            ActiveSubscriptions = activeSubscriptions,
            SubscriptionsByPlan = subscriptionsByPlan.ToDictionary(x => x.Plan, x => x.Count),
            MonthlyRecurringRevenue = totalRevenue
        };
    }
}
