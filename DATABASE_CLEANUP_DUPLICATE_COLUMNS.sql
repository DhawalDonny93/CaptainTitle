-- ============================================================================
-- Database Cleanup: Remove Duplicate UserId/CreatedByUserId Columns
-- ============================================================================
-- Purpose: Remove CreatedByUserId columns from tables that already have UserId
-- Reason: UserId is sufficient for tracking ownership; CreatedByUserId is redundant
-- ============================================================================

-- ANALYSIS RESULTS:
-- ✅ LlmPromptTemplates: Has CreatedByUserId but NO UserId (CORRECT - admin templates)
-- ❌ PromptTrackings: Had BOTH UserId and CreatedByUserId (DUPLICATE - FIXED)
-- ✅ All other entities: Only have UserId (CORRECT)

-- ============================================================================
-- STEP 1: Check if CreatedByUserId column exists in PromptTrackings
-- ============================================================================
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' 
AND table_name = 'PromptTrackings'
AND column_name = 'CreatedByUserId';

-- ============================================================================
-- STEP 2: Drop CreatedByUserId column from PromptTrackings
-- ============================================================================
-- This column was redundant since UserId already tracks the owner
ALTER TABLE "PromptTrackings" 
DROP COLUMN IF EXISTS "CreatedByUserId";

-- ============================================================================
-- STEP 3: Verify the column has been removed
-- ============================================================================
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' 
AND table_name = 'PromptTrackings'
ORDER BY ordinal_position;

-- ============================================================================
-- STEP 4: Check for any other tables with both UserId and CreatedByUserId
-- ============================================================================
SELECT 
    t1.table_name,
    COUNT(*) as column_count
FROM information_schema.columns t1
WHERE t1.table_schema = 'public'
AND t1.column_name IN ('UserId', 'CreatedByUserId')
GROUP BY t1.table_name
HAVING COUNT(*) > 1
ORDER BY t1.table_name;

-- Expected result: No rows (all duplicates removed)

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- List all tables with UserId column
SELECT table_name, column_name
FROM information_schema.columns
WHERE table_schema = 'public' 
AND column_name = 'UserId'
ORDER BY table_name;

-- List all tables with CreatedByUserId column
SELECT table_name, column_name
FROM information_schema.columns
WHERE table_schema = 'public' 
AND column_name = 'CreatedByUserId'
ORDER BY table_name;

-- Expected: Only LlmPromptTemplates should have CreatedByUserId (admin templates)

-- ============================================================================
-- ROLLBACK (if needed)
-- ============================================================================
-- If you need to restore the column (NOT RECOMMENDED):
-- ALTER TABLE "PromptTrackings" ADD COLUMN "CreatedByUserId" INTEGER;
-- UPDATE "PromptTrackings" SET "CreatedByUserId" = "UserId";

-- ============================================================================
-- SUMMARY
-- ============================================================================
-- Tables Modified:
-- 1. PromptTrackings - Removed CreatedByUserId column
--
-- Tables with CreatedByUserId (KEPT):
-- 1. LlmPromptTemplates - Admin-created templates (no UserId, correct)
--
-- All other tables use UserId only (correct pattern)
-- ============================================================================
