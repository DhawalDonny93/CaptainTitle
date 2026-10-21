-- =====================================================
-- Database Cleanup: Remove Mobile/RevenueCat Tables
-- Web-Only Subscription System
-- PostgreSQL Script
-- =====================================================

-- This script removes old mobile/RevenueCat subscription tables
-- and keeps only the web-based subscription system

-- =====================================================
-- BACKUP REMINDER
-- =====================================================
-- IMPORTANT: Before running this script, backup your database!
-- pg_dump -U your_user -d your_database > backup_before_cleanup.sql

-- =====================================================
-- Step 1: Drop Old Mobile/RevenueCat Tables
-- =====================================================

-- Drop SubscriptionTransactions table (mobile transactions)
DROP TABLE IF EXISTS "SubscriptionTransactions" CASCADE;

-- Drop Subscriptions table (mobile/RevenueCat subscriptions)
DROP TABLE IF EXISTS "Subscriptions" CASCADE;

-- =====================================================
-- Step 2: Verify Web Subscription Tables Exist
-- =====================================================

-- These tables should already exist from migration 14
-- If not, run 14_subscription_quota_system_postgres.sql first

-- Verify SubscriptionPlans table
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'SubscriptionPlans') THEN
        RAISE EXCEPTION 'SubscriptionPlans table does not exist. Run migration 14 first.';
    END IF;
END $$;

-- Verify UserSubscriptions table
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'UserSubscriptions') THEN
        RAISE EXCEPTION 'UserSubscriptions table does not exist. Run migration 14 first.';
    END IF;
END $$;

-- Verify UsageQuotas table
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'UsageQuotas') THEN
        RAISE EXCEPTION 'UsageQuotas table does not exist. Run migration 14 first.';
    END IF;
END $$;

-- Verify QuotaUsageLogs table
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'QuotaUsageLogs') THEN
        RAISE EXCEPTION 'QuotaUsageLogs table does not exist. Run migration 14 first.';
    END IF;
END $$;

-- =====================================================
-- Step 3: Clean Up Any Mobile-Related Columns (Optional)
-- =====================================================

-- If Users table has mobile-specific columns, you can remove them here
-- Example (uncomment if needed):
-- ALTER TABLE "Users" DROP COLUMN IF EXISTS "RevenueCatId";
-- ALTER TABLE "Users" DROP COLUMN IF EXISTS "AppleUserId";
-- ALTER TABLE "Users" DROP COLUMN IF EXISTS "GooglePlayUserId";

-- =====================================================
-- Step 4: Verify Data Integrity
-- =====================================================

-- Check that all active UserSubscriptions have valid plans
DO $$
DECLARE
    orphaned_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO orphaned_count
    FROM "UserSubscriptions" us
    LEFT JOIN "SubscriptionPlans" sp ON us."SubscriptionPlanId" = sp."Id"
    WHERE us."Status" = 'Active' AND sp."Id" IS NULL;
    
    IF orphaned_count > 0 THEN
        RAISE WARNING 'Found % active subscriptions without valid plans', orphaned_count;
    END IF;
END $$;

-- Check that all UserSubscriptions have quotas initialized
DO $$
DECLARE
    missing_quotas_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO missing_quotas_count
    FROM "UserSubscriptions" us
    LEFT JOIN "UsageQuotas" uq ON us."Id" = uq."UserSubscriptionId"
    WHERE us."Status" = 'Active' AND uq."Id" IS NULL;
    
    IF missing_quotas_count > 0 THEN
        RAISE WARNING 'Found % active subscriptions without quotas', missing_quotas_count;
    END IF;
END $$;

-- =====================================================
-- Step 5: Summary Report
-- =====================================================

-- Show current subscription statistics
SELECT 
    'Web Subscription System Summary' AS report_section,
    (SELECT COUNT(*) FROM "SubscriptionPlans") AS total_plans,
    (SELECT COUNT(*) FROM "UserSubscriptions") AS total_subscriptions,
    (SELECT COUNT(*) FROM "UserSubscriptions" WHERE "Status" = 'Active') AS active_subscriptions,
    (SELECT COUNT(*) FROM "UsageQuotas") AS total_quotas,
    (SELECT COUNT(*) FROM "QuotaUsageLogs") AS total_usage_logs;

-- Show subscriptions by plan
SELECT 
    sp."DisplayName" AS plan_name,
    COUNT(us."Id") AS subscription_count,
    COUNT(CASE WHEN us."Status" = 'Active' THEN 1 END) AS active_count
FROM "SubscriptionPlans" sp
LEFT JOIN "UserSubscriptions" us ON sp."Id" = us."SubscriptionPlanId"
GROUP BY sp."Id", sp."DisplayName"
ORDER BY sp."MonthlyPrice";

-- =====================================================
-- Cleanup Complete
-- =====================================================

-- The database now uses only the web-based subscription system:
-- ✅ SubscriptionPlans - Plan definitions
-- ✅ UserSubscriptions - User plan assignments
-- ✅ UsageQuotas - Quota tracking
-- ✅ QuotaUsageLogs - Usage history
--
-- ❌ Subscriptions - REMOVED (mobile/RevenueCat)
-- ❌ SubscriptionTransactions - REMOVED (mobile transactions)

RAISE NOTICE 'Database cleanup complete! Mobile/RevenueCat tables removed.';
