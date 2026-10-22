using System;
using System.Collections.Generic;

namespace AppBackendBasic.Core.Entities
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Free, Starter, Professional, Enterprise
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        
        // Feature Limits
        public int MaxProjects { get; set; } = 1;
        public int MaxPromptsPerProject { get; set; } = 10;
        public int MaxVisibilityChecksPerMonth { get; set; } = 100;
        public int MaxSiteAuditsPerMonth { get; set; } = 5;
        public int MaxContentArticles { get; set; } = 10;
        public int MaxLlmRequestsPerMonth { get; set; } = 1000;
        public int MaxLlmTokensPerMonth { get; set; } = 100000;
        
        // Feature Flags
        public bool AllowCompetitorTracking { get; set; } = false;
        public bool AllowAdvancedAnalytics { get; set; } = false;
        public bool AllowApiAccess { get; set; } = false;
        public bool AllowWhiteLabel { get; set; } = false;
        public bool AllowPrioritySupport { get; set; } = false;
        public bool AllowCustomIntegrations { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }

    public class UserSubscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubscriptionPlanId { get; set; }
        public string Status { get; set; } = "Active"; // Active, Cancelled, Expired, PastDue
        public string BillingCycle { get; set; } = "Monthly"; // Monthly, Yearly
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public bool IsTrialActive { get; set; } = false;
        
        // Payment info
        public string? PaymentProvider { get; set; } // Stripe, PayPal, etc.
        public string? PaymentProviderId { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime? NextBillingDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
        public virtual ICollection<UsageQuota> UsageQuotas { get; set; } = new List<UsageQuota>();
    }

    public class UsageQuota
    {
        public int Id { get; set; }
        public int UserSubscriptionId { get; set; }
        public string QuotaType { get; set; } = string.Empty; // VisibilityChecks, LlmRequests, SiteAudits, etc.
        public int QuotaLimit { get; set; }
        public int QuotaUsed { get; set; } = 0;
        public DateTime PeriodStart { get; set; } = DateTime.UtcNow;
        public DateTime PeriodEnd { get; set; }
        public bool IsResetMonthly { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual UserSubscription UserSubscription { get; set; } = null!;
    }

    public class QuotaUsageLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? ProjectId { get; set; }
        public string QuotaType { get; set; } = string.Empty;
        public int AmountUsed { get; set; } = 1;
        public string? ResourceId { get; set; } // ID of the resource that consumed quota
        public string? ResourceType { get; set; } // PromptCheck, SiteAudit, LlmRequest, etc.
        public string? Metadata { get; set; } // JSON with additional info
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
