-- =============================================
-- Subscription & Quota Management System
-- Migration: 14_subscription_quota_system_postgres.sql
-- PostgreSQL Version
-- =============================================

-- =============================================
-- 1. Subscription Plans Table
-- =============================================
CREATE TABLE IF NOT EXISTS "SubscriptionPlans" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "DisplayName" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "MonthlyPrice" NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    "YearlyPrice" NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    "Currency" VARCHAR(10) NOT NULL DEFAULT 'USD',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "SortOrder" INTEGER NOT NULL DEFAULT 0,
    
    -- Feature Limits
    "MaxProjects" INTEGER NOT NULL DEFAULT 1,
    "MaxPromptsPerProject" INTEGER NOT NULL DEFAULT 10,
    "MaxVisibilityChecksPerMonth" INTEGER NOT NULL DEFAULT 100,
    "MaxSiteAuditsPerMonth" INTEGER NOT NULL DEFAULT 5,
    "MaxContentArticles" INTEGER NOT NULL DEFAULT 10,
    "MaxLlmRequestsPerMonth" INTEGER NOT NULL DEFAULT 1000,
    "MaxLlmTokensPerMonth" INTEGER NOT NULL DEFAULT 100000,
    
    -- Feature Flags
    "AllowCompetitorTracking" BOOLEAN NOT NULL DEFAULT FALSE,
    "AllowAdvancedAnalytics" BOOLEAN NOT NULL DEFAULT FALSE,
    "AllowApiAccess" BOOLEAN NOT NULL DEFAULT FALSE,
    "AllowWhiteLabel" BOOLEAN NOT NULL DEFAULT FALSE,
    "AllowPrioritySupport" BOOLEAN NOT NULL DEFAULT FALSE,
    "AllowCustomIntegrations" BOOLEAN NOT NULL DEFAULT FALSE,
    
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "idx_subscriptionplans_name" ON "SubscriptionPlans" ("Name");
CREATE INDEX IF NOT EXISTS "idx_subscriptionplans_active" ON "SubscriptionPlans" ("IsActive");
CREATE INDEX IF NOT EXISTS "idx_subscriptionplans_sort" ON "SubscriptionPlans" ("SortOrder");

-- Create trigger for UpdatedAt
CREATE OR REPLACE FUNCTION update_subscriptionplans_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_subscriptionplans_updated_at
    BEFORE UPDATE ON "SubscriptionPlans"
    FOR EACH ROW
    EXECUTE FUNCTION update_subscriptionplans_updated_at();

-- =============================================
-- 2. User Subscriptions Table
-- =============================================
CREATE TABLE IF NOT EXISTS "UserSubscriptions" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "SubscriptionPlanId" INTEGER NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, Cancelled, Expired, PastDue
    "BillingCycle" VARCHAR(20) NOT NULL DEFAULT 'Monthly', -- Monthly, Yearly
    "StartDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "EndDate" TIMESTAMP NULL,
    "CancelledAt" TIMESTAMP NULL,
    "TrialEndsAt" TIMESTAMP NULL,
    "IsTrialActive" BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Payment info
    "PaymentProvider" VARCHAR(50) NULL,
    "PaymentProviderId" VARCHAR(255) NULL,
    "CurrentPrice" NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    "NextBillingDate" TIMESTAMP NULL,
    
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT "fk_usersubscriptions_user" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "fk_usersubscriptions_plan" FOREIGN KEY ("SubscriptionPlanId") REFERENCES "SubscriptionPlans"("Id") ON DELETE RESTRICT
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "idx_usersubscriptions_user" ON "UserSubscriptions" ("UserId");
CREATE INDEX IF NOT EXISTS "idx_usersubscriptions_plan" ON "UserSubscriptions" ("SubscriptionPlanId");
CREATE INDEX IF NOT EXISTS "idx_usersubscriptions_status" ON "UserSubscriptions" ("Status");
CREATE INDEX IF NOT EXISTS "idx_usersubscriptions_billing_cycle" ON "UserSubscriptions" ("BillingCycle");
CREATE INDEX IF NOT EXISTS "idx_usersubscriptions_next_billing" ON "UserSubscriptions" ("NextBillingDate");

-- Create trigger for UpdatedAt
CREATE OR REPLACE FUNCTION update_usersubscriptions_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_usersubscriptions_updated_at
    BEFORE UPDATE ON "UserSubscriptions"
    FOR EACH ROW
    EXECUTE FUNCTION update_usersubscriptions_updated_at();

-- =============================================
-- 3. Usage Quotas Table
-- =============================================
CREATE TABLE IF NOT EXISTS "UsageQuotas" (
    "Id" SERIAL PRIMARY KEY,
    "UserSubscriptionId" INTEGER NOT NULL,
    "QuotaType" VARCHAR(100) NOT NULL, -- VisibilityChecks, LlmRequests, SiteAudits, etc.
    "QuotaLimit" INTEGER NOT NULL DEFAULT 0,
    "QuotaUsed" INTEGER NOT NULL DEFAULT 0,
    "PeriodStart" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "PeriodEnd" TIMESTAMP NOT NULL,
    "IsResetMonthly" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT "fk_usagequotas_subscription" FOREIGN KEY ("UserSubscriptionId") REFERENCES "UserSubscriptions"("Id") ON DELETE CASCADE,
    CONSTRAINT "unique_subscription_quota" UNIQUE ("UserSubscriptionId", "QuotaType")
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "idx_usagequotas_subscription" ON "UsageQuotas" ("UserSubscriptionId");
CREATE INDEX IF NOT EXISTS "idx_usagequotas_quota_type" ON "UsageQuotas" ("QuotaType");
CREATE INDEX IF NOT EXISTS "idx_usagequotas_period" ON "UsageQuotas" ("PeriodStart", "PeriodEnd");

-- Create trigger for UpdatedAt
CREATE OR REPLACE FUNCTION update_usagequotas_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_usagequotas_updated_at
    BEFORE UPDATE ON "UsageQuotas"
    FOR EACH ROW
    EXECUTE FUNCTION update_usagequotas_updated_at();

-- =============================================
-- 4. Quota Usage Logs Table
-- =============================================
CREATE TABLE IF NOT EXISTS "QuotaUsageLogs" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "ProjectId" INTEGER NULL,
    "QuotaType" VARCHAR(100) NOT NULL,
    "AmountUsed" INTEGER NOT NULL DEFAULT 1,
    "ResourceId" VARCHAR(255) NULL,
    "ResourceType" VARCHAR(100) NULL,
    "Metadata" TEXT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT "fk_quotausagelogs_user" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "fk_quotausagelogs_project" FOREIGN KEY ("ProjectId") REFERENCES "Projects"("Id") ON DELETE SET NULL
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "idx_quotausagelogs_user" ON "QuotaUsageLogs" ("UserId");
CREATE INDEX IF NOT EXISTS "idx_quotausagelogs_project" ON "QuotaUsageLogs" ("ProjectId");
CREATE INDEX IF NOT EXISTS "idx_quotausagelogs_quota_type" ON "QuotaUsageLogs" ("QuotaType");
CREATE INDEX IF NOT EXISTS "idx_quotausagelogs_created" ON "QuotaUsageLogs" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "idx_quotausagelogs_resource" ON "QuotaUsageLogs" ("ResourceId", "ResourceType");

-- =============================================
-- 5. Seed Default Subscription Plans
-- =============================================

INSERT INTO "SubscriptionPlans" (
    "Name", "DisplayName", "Description", "MonthlyPrice", "YearlyPrice", "Currency", "IsActive", "SortOrder",
    "MaxProjects", "MaxPromptsPerProject", "MaxVisibilityChecksPerMonth", "MaxSiteAuditsPerMonth",
    "MaxContentArticles", "MaxLlmRequestsPerMonth", "MaxLlmTokensPerMonth",
    "AllowCompetitorTracking", "AllowAdvancedAnalytics", "AllowApiAccess", "AllowWhiteLabel",
    "AllowPrioritySupport", "AllowCustomIntegrations"
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

INSERT INTO "UserSubscriptions" ("UserId", "SubscriptionPlanId", "Status", "BillingCycle", "StartDate", "CurrentPrice")
SELECT 
    u."Id",
    (SELECT "Id" FROM "SubscriptionPlans" WHERE "Name" = 'Free' LIMIT 1),
    'Active',
    'Monthly',
    CURRENT_TIMESTAMP,
    0.00
FROM "Users" u
WHERE NOT EXISTS (
    SELECT 1 FROM "UserSubscriptions" us WHERE us."UserId" = u."Id"
);

-- =============================================
-- 7. Initialize Quotas for New Free Subscriptions
-- =============================================

-- VisibilityChecks Quota
INSERT INTO "UsageQuotas" ("UserSubscriptionId", "QuotaType", "QuotaLimit", "QuotaUsed", "PeriodStart", "PeriodEnd", "IsResetMonthly")
SELECT 
    us."Id",
    'VisibilityChecks',
    sp."MaxVisibilityChecksPerMonth",
    0,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP + INTERVAL '1 month',
    TRUE
FROM "UserSubscriptions" us
JOIN "SubscriptionPlans" sp ON us."SubscriptionPlanId" = sp."Id"
WHERE NOT EXISTS (
    SELECT 1 FROM "UsageQuotas" uq 
    WHERE uq."UserSubscriptionId" = us."Id" AND uq."QuotaType" = 'VisibilityChecks'
);

-- SiteAudits Quota
INSERT INTO "UsageQuotas" ("UserSubscriptionId", "QuotaType", "QuotaLimit", "QuotaUsed", "PeriodStart", "PeriodEnd", "IsResetMonthly")
SELECT 
    us."Id",
    'SiteAudits',
    sp."MaxSiteAuditsPerMonth",
    0,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP + INTERVAL '1 month',
    TRUE
FROM "UserSubscriptions" us
JOIN "SubscriptionPlans" sp ON us."SubscriptionPlanId" = sp."Id"
WHERE NOT EXISTS (
    SELECT 1 FROM "UsageQuotas" uq 
    WHERE uq."UserSubscriptionId" = us."Id" AND uq."QuotaType" = 'SiteAudits'
);

-- LlmRequests Quota
INSERT INTO "UsageQuotas" ("UserSubscriptionId", "QuotaType", "QuotaLimit", "QuotaUsed", "PeriodStart", "PeriodEnd", "IsResetMonthly")
SELECT 
    us."Id",
    'LlmRequests',
    sp."MaxLlmRequestsPerMonth",
    0,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP + INTERVAL '1 month',
    TRUE
FROM "UserSubscriptions" us
JOIN "SubscriptionPlans" sp ON us."SubscriptionPlanId" = sp."Id"
WHERE NOT EXISTS (
    SELECT 1 FROM "UsageQuotas" uq 
    WHERE uq."UserSubscriptionId" = us."Id" AND uq."QuotaType" = 'LlmRequests'
);

-- LlmTokens Quota
INSERT INTO "UsageQuotas" ("UserSubscriptionId", "QuotaType", "QuotaLimit", "QuotaUsed", "PeriodStart", "PeriodEnd", "IsResetMonthly")
SELECT 
    us."Id",
    'LlmTokens',
    sp."MaxLlmTokensPerMonth",
    0,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP + INTERVAL '1 month',
    TRUE
FROM "UserSubscriptions" us
JOIN "SubscriptionPlans" sp ON us."SubscriptionPlanId" = sp."Id"
WHERE NOT EXISTS (
    SELECT 1 FROM "UsageQuotas" uq 
    WHERE uq."UserSubscriptionId" = us."Id" AND uq."QuotaType" = 'LlmTokens'
);

-- =============================================
-- 8. Create Helper Functions (Optional but Recommended)
-- =============================================

-- Function to check if user has quota available
CREATE OR REPLACE FUNCTION check_user_quota(
    p_user_id INTEGER,
    p_quota_type VARCHAR(100),
    p_amount INTEGER DEFAULT 1
)
RETURNS BOOLEAN AS $$
DECLARE
    v_available INTEGER;
BEGIN
    SELECT (uq."QuotaLimit" - uq."QuotaUsed")
    INTO v_available
    FROM "UserSubscriptions" us
    JOIN "UsageQuotas" uq ON us."Id" = uq."UserSubscriptionId"
    WHERE us."UserId" = p_user_id
        AND us."Status" = 'Active'
        AND uq."QuotaType" = p_quota_type
    LIMIT 1;
    
    RETURN COALESCE(v_available, 0) >= p_amount;
END;
$$ LANGUAGE plpgsql;

-- Function to consume quota
CREATE OR REPLACE FUNCTION consume_user_quota(
    p_user_id INTEGER,
    p_quota_type VARCHAR(100),
    p_amount INTEGER DEFAULT 1,
    p_resource_id VARCHAR(255) DEFAULT NULL,
    p_resource_type VARCHAR(100) DEFAULT NULL,
    p_project_id INTEGER DEFAULT NULL
)
RETURNS BOOLEAN AS $$
DECLARE
    v_subscription_id INTEGER;
    v_available INTEGER;
BEGIN
    -- Get active subscription and check quota
    SELECT us."Id", (uq."QuotaLimit" - uq."QuotaUsed")
    INTO v_subscription_id, v_available
    FROM "UserSubscriptions" us
    JOIN "UsageQuotas" uq ON us."Id" = uq."UserSubscriptionId"
    WHERE us."UserId" = p_user_id
        AND us."Status" = 'Active'
        AND uq."QuotaType" = p_quota_type
    LIMIT 1;
    
    -- Check if quota is available
    IF v_available IS NULL OR v_available < p_amount THEN
        RETURN FALSE;
    END IF;
    
    -- Update quota
    UPDATE "UsageQuotas"
    SET "QuotaUsed" = "QuotaUsed" + p_amount,
        "UpdatedAt" = CURRENT_TIMESTAMP
    WHERE "UserSubscriptionId" = v_subscription_id
        AND "QuotaType" = p_quota_type;
    
    -- Log usage
    INSERT INTO "QuotaUsageLogs" (
        "UserId", "ProjectId", "QuotaType", "AmountUsed",
        "ResourceId", "ResourceType", "CreatedAt"
    ) VALUES (
        p_user_id, p_project_id, p_quota_type, p_amount,
        p_resource_id, p_resource_type, CURRENT_TIMESTAMP
    );
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- Function to reset monthly quotas
CREATE OR REPLACE FUNCTION reset_monthly_quotas()
RETURNS INTEGER AS $$
DECLARE
    v_reset_count INTEGER;
BEGIN
    UPDATE "UsageQuotas"
    SET "QuotaUsed" = 0,
        "PeriodStart" = CURRENT_TIMESTAMP,
        "PeriodEnd" = CURRENT_TIMESTAMP + INTERVAL '1 month',
        "UpdatedAt" = CURRENT_TIMESTAMP
    WHERE "IsResetMonthly" = TRUE
        AND "PeriodEnd" < CURRENT_TIMESTAMP;
    
    GET DIAGNOSTICS v_reset_count = ROW_COUNT;
    RETURN v_reset_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- 9. Create Scheduled Job for Monthly Quota Reset (Optional)
-- =============================================

-- Note: This requires pg_cron extension
-- To enable: CREATE EXTENSION IF NOT EXISTS pg_cron;
-- 
-- Schedule monthly quota reset (runs daily at 2 AM)
-- SELECT cron.schedule('reset-monthly-quotas', '0 2 * * *', 'SELECT reset_monthly_quotas();');

-- =============================================
-- Migration Complete
-- =============================================

-- Verify installation
DO $$
DECLARE
    v_plans_count INTEGER;
    v_subscriptions_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_plans_count FROM "SubscriptionPlans";
    SELECT COUNT(*) INTO v_subscriptions_count FROM "UserSubscriptions";
    
    RAISE NOTICE 'Subscription system installed successfully!';
    RAISE NOTICE 'Subscription Plans: %', v_plans_count;
    RAISE NOTICE 'User Subscriptions: %', v_subscriptions_count;
END $$;
