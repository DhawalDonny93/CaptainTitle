-- ============================================================================
-- DATABASE TABLE REMOVAL MIGRATION SCRIPT
-- ============================================================================
-- Purpose: Remove unused tables (Analytics, Product, Receipt)
-- 
-- Tables to Remove:
-- 1. Analytics tables: AnalyticsCohort, AnalyticsEvent, AnalyticsFunnel, AnalyticsExport
-- 2. Product table
-- 3. Receipt table
--
-- IMPORTANT: BACKUP YOUR DATABASE BEFORE RUNNING THIS SCRIPT!
-- ============================================================================

BEGIN;

-- ============================================================================
-- PHASE 1: Drop Analytics Tables (if they exist)
-- ============================================================================

-- Drop AnalyticsExport table
DROP TABLE IF EXISTS "AnalyticsExport" CASCADE;
RAISE NOTICE 'AnalyticsExport table dropped (if existed)';

-- Drop AnalyticsFunnel table
DROP TABLE IF EXISTS "AnalyticsFunnel" CASCADE;
RAISE NOTICE 'AnalyticsFunnel table dropped (if existed)';

-- Drop AnalyticsEvent table
DROP TABLE IF EXISTS "AnalyticsEvent" CASCADE;
RAISE NOTICE 'AnalyticsEvent table dropped (if existed)';

-- Drop AnalyticsCohort table
DROP TABLE IF EXISTS "AnalyticsCohort" CASCADE;
RAISE NOTICE 'AnalyticsCohort table dropped (if existed)';

-- ============================================================================
-- PHASE 2: Drop Product Table
-- ============================================================================

-- Drop Product table (replaced by RevenueCat)
DROP TABLE IF EXISTS "Product" CASCADE;
RAISE NOTICE 'Product table dropped (if existed)';

-- ============================================================================
-- PHASE 3: Drop Receipt Table
-- ============================================================================

-- Drop Receipt table (replaced by RevenueCat webhooks)
DROP TABLE IF EXISTS "Receipt" CASCADE;
RAISE NOTICE 'Receipt table dropped (if existed)';

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- Verify Analytics tables are dropped
SELECT 
    'AnalyticsCohort' as table_name,
    EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_name = 'AnalyticsCohort'
    ) as still_exists
UNION ALL
SELECT 
    'AnalyticsEvent',
    EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_name = 'AnalyticsEvent'
    )
UNION ALL
SELECT 
    'AnalyticsFunnel',
    EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_name = 'AnalyticsFunnel'
    )
UNION ALL
SELECT 
    'AnalyticsExport',
    EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_name = 'AnalyticsExport'
    )
UNION ALL
SELECT 
    'Product',
    EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_name = 'Product'
    )
UNION ALL
SELECT 
    'Receipt',
    EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_name = 'Receipt'
    );

-- Count remaining tables
SELECT COUNT(*) as remaining_table_count 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_type = 'BASE TABLE';

COMMIT;

-- ============================================================================
-- ROLLBACK INSTRUCTIONS (if needed)
-- ============================================================================
-- If you need to rollback this migration:
-- 1. Restore from your database backup
-- 2. Or manually recreate the dropped tables from the previous migration
-- 
-- IMPORTANT: Always test migrations on a development/staging database first!
-- ============================================================================

-- ============================================================================
-- NOTES
-- ============================================================================
-- Tables NOT removed (actively used):
-- - DeviceRegistration, DeviceToken: Used by push notifications, biometric auth
-- - ParentalConsent: Used by age verification service (COPPA compliance)
-- 
-- These tables have active services and controllers depending on them.
-- ============================================================================
