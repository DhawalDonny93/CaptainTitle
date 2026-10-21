-- =============================================
-- Subscription & Quota Management System
-- Migration: 14_subscription_quota_system.sql
-- =============================================

-- =============================================
-- 1. Subscription Plans Table
-- =============================================
CREATE TABLE IF NOT EXISTS SubscriptionPlans (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    DisplayName VARCHAR(200) NOT NULL,
    Description TEXT,
    MonthlyPrice DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    YearlyPrice DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    Currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    SortOrder INT NOT NULL DEFAULT 0,
    
    -- Feature Limits
    MaxProjects INT NOT NULL DEFAULT 1,
    MaxPromptsPerProject INT NOT NULL DEFAULT 10,
    MaxVisibilityChecksPerMonth INT NOT NULL DEFAULT 100,
    MaxSiteAuditsPerMonth INT NOT NULL DEFAULT 5,
    MaxContentArticles INT NOT NULL DEFAULT 10,
    MaxLlmRequestsPerMonth INT NOT NULL DEFAULT 1000,
    MaxLlmTokensPerMonth INT NOT NULL DEFAULT 100000,
    
    -- Feature Flags
    AllowCompetitorTracking BOOLEAN NOT NULL DEFAULT FALSE,
    AllowAdvancedAnalytics BOOLEAN NOT NULL DEFAULT FALSE,
    AllowApiAccess BOOLEAN NOT NULL DEFAULT FALSE,
    AllowWhiteLabel BOOLEAN NOT NULL DEFAULT FALSE,
    AllowPrioritySupport BOOLEAN NOT NULL DEFAULT FALSE,
    AllowCustomIntegrations BOOLEAN NOT NULL DEFAULT FALSE,
    
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_name (Name),
    INDEX idx_active (IsActive),
    INDEX idx_sort (SortOrder)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 2. User Subscriptions Table
-- =============================================
CREATE TABLE IF NOT EXISTS UserSubscriptions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    SubscriptionPlanId INT NOT NULL,
    Status VARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, Cancelled, Expired, PastDue
    BillingCycle VARCHAR(20) NOT NULL DEFAULT 'Monthly', -- Monthly, Yearly
    StartDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    EndDate DATETIME NULL,
    CancelledAt DATETIME NULL,
    TrialEndsAt DATETIME NULL,
    IsTrialActive BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Payment info
    PaymentProvider VARCHAR(50) NULL,
    PaymentProviderId VARCHAR(255) NULL,
    CurrentPrice DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    NextBillingDate DATETIME NULL,
    
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (SubscriptionPlanId) REFERENCES SubscriptionPlans(Id) ON DELETE RESTRICT,
    
    INDEX idx_user (UserId),
    INDEX idx_plan (SubscriptionPlanId),
    INDEX idx_status (Status),
    INDEX idx_billing_cycle (BillingCycle),
    INDEX idx_next_billing (NextBillingDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 3. Usage Quotas Table
-- =============================================
CREATE TABLE IF NOT EXISTS UsageQuotas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserSubscriptionId INT NOT NULL,
    QuotaType VARCHAR(100) NOT NULL, -- VisibilityChecks, LlmRequests, SiteAudits, etc.
    QuotaLimit INT NOT NULL DEFAULT 0,
    QuotaUsed INT NOT NULL DEFAULT 0,
    PeriodStart DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PeriodEnd DATETIME NOT NULL,
    IsResetMonthly BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (UserSubscriptionId) REFERENCES UserSubscriptions(Id) ON DELETE CASCADE,
    
    INDEX idx_subscription (UserSubscriptionId),
    INDEX idx_quota_type (QuotaType),
    INDEX idx_period (PeriodStart, PeriodEnd),
    UNIQUE KEY unique_subscription_quota (UserSubscriptionId, QuotaType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 4. Quota Usage Logs Table
-- =============================================
CREATE TABLE IF NOT EXISTS QuotaUsageLogs (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    ProjectId INT NULL,
    QuotaType VARCHAR(100) NOT NULL,
    AmountUsed INT NOT NULL DEFAULT 1,
    ResourceId VARCHAR(255) NULL,
    ResourceType VARCHAR(100) NULL,
    Metadata TEXT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE SET NULL,
    
    INDEX idx_user (UserId),
    INDEX idx_project (ProjectId),
    INDEX idx_quota_type (QuotaType),
    INDEX idx_created (CreatedAt),
    INDEX idx_resource (ResourceId, ResourceType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 5. Seed Default Subscription Plans
-- =============================================

INSERT INTO SubscriptionPlans (
    Name, DisplayName, Description, MonthlyPrice, YearlyPrice, Currency, IsActive, SortOrder,
    MaxProjects, MaxPromptsPerProject, MaxVisibilityChecksPerMonth, MaxSiteAuditsPerMonth,
    MaxContentArticles, MaxLlmRequestsPerMonth, MaxLlmTokensPerMonth,
    AllowCompetitorTracking, AllowAdvancedAnalytics, AllowApiAccess, AllowWhiteLabel,
    AllowPrioritySupport, AllowCustomIntegrations
) VALUES
-- Free Plan
(
    'Free', 'Free', 'Perfect for getting started with AI SEO',
    0.00, 0.00, 'USD', TRUE, 1,
    1, 5, 50, 2, 5, 500, 50000,
    FALSE, FALSE, FALSE, FALSE, FALSE, FALSE
),
-- Starter Plan
(
    'Starter', 'Starter', 'Great for small businesses and freelancers',
    29.00, 290.00, 'USD', TRUE, 2,
    3, 20, 500, 10, 50, 5000, 500000,
    TRUE, FALSE, FALSE, FALSE, FALSE, FALSE
),
-- Professional Plan
(
    'Professional', 'Professional', 'For growing businesses and agencies',
    99.00, 990.00, 'USD', TRUE, 3,
    10, 100, 2000, 50, 200, 20000, 2000000,
    TRUE, TRUE, TRUE, FALSE, TRUE, FALSE
),
-- Enterprise Plan
(
    'Enterprise', 'Enterprise', 'Custom solutions for large organizations',
    299.00, 2990.00, 'USD', TRUE, 4,
    -1, -1, -1, -1, -1, -1, -1,
    TRUE, TRUE, TRUE, TRUE, TRUE, TRUE
);

-- =============================================
-- 6. Create Free Subscriptions for Existing Users
-- =============================================

INSERT INTO UserSubscriptions (UserId, SubscriptionPlanId, Status, BillingCycle, StartDate, CurrentPrice)
SELECT 
    u.Id,
    (SELECT Id FROM SubscriptionPlans WHERE Name = 'Free' LIMIT 1),
    'Active',
    'Monthly',
    NOW(),
    0.00
FROM Users u
WHERE NOT EXISTS (
    SELECT 1 FROM UserSubscriptions us WHERE us.UserId = u.Id
);

-- =============================================
-- 7. Initialize Quotas for New Free Subscriptions
-- =============================================

INSERT INTO UsageQuotas (UserSubscriptionId, QuotaType, QuotaLimit, QuotaUsed, PeriodStart, PeriodEnd, IsResetMonthly)
SELECT 
    us.Id,
    'VisibilityChecks',
    sp.MaxVisibilityChecksPerMonth,
    0,
    NOW(),
    DATE_ADD(NOW(), INTERVAL 1 MONTH),
    TRUE
FROM UserSubscriptions us
JOIN SubscriptionPlans sp ON us.SubscriptionPlanId = sp.Id
WHERE NOT EXISTS (
    SELECT 1 FROM UsageQuotas uq 
    WHERE uq.UserSubscriptionId = us.Id AND uq.QuotaType = 'VisibilityChecks'
);

INSERT INTO UsageQuotas (UserSubscriptionId, QuotaType, QuotaLimit, QuotaUsed, PeriodStart, PeriodEnd, IsResetMonthly)
SELECT 
    us.Id,
    'SiteAudits',
    sp.MaxSiteAuditsPerMonth,
    0,
    NOW(),
    DATE_ADD(NOW(), INTERVAL 1 MONTH),
    TRUE
FROM UserSubscriptions us
JOIN SubscriptionPlans sp ON us.SubscriptionPlanId = sp.Id
WHERE NOT EXISTS (
    SELECT 1 FROM UsageQuotas uq 
    WHERE uq.UserSubscriptionId = us.Id AND uq.QuotaType = 'SiteAudits'
);

INSERT INTO UsageQuotas (UserSubscriptionId, QuotaType, QuotaLimit, QuotaUsed, PeriodStart, PeriodEnd, IsResetMonthly)
SELECT 
    us.Id,
    'LlmRequests',
    sp.MaxLlmRequestsPerMonth,
    0,
    NOW(),
    DATE_ADD(NOW(), INTERVAL 1 MONTH),
    TRUE
FROM UserSubscriptions us
JOIN SubscriptionPlans sp ON us.SubscriptionPlanId = sp.Id
WHERE NOT EXISTS (
    SELECT 1 FROM UsageQuotas uq 
    WHERE uq.UserSubscriptionId = us.Id AND uq.QuotaType = 'LlmRequests'
);

INSERT INTO UsageQuotas (UserSubscriptionId, QuotaType, QuotaLimit, QuotaUsed, PeriodStart, PeriodEnd, IsResetMonthly)
SELECT 
    us.Id,
    'LlmTokens',
    sp.MaxLlmTokensPerMonth,
    0,
    NOW(),
    DATE_ADD(NOW(), INTERVAL 1 MONTH),
    TRUE
FROM UserSubscriptions us
JOIN SubscriptionPlans sp ON us.SubscriptionPlanId = sp.Id
WHERE NOT EXISTS (
    SELECT 1 FROM UsageQuotas uq 
    WHERE uq.UserSubscriptionId = us.Id AND uq.QuotaType = 'LlmTokens'
);

-- =============================================
-- Migration Complete
-- =============================================
