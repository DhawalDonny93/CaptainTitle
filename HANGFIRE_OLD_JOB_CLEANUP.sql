-- ============================================================================
-- Hangfire Old Job Cleanup Script
-- ============================================================================
-- Purpose: Remove old/obsolete recurring jobs from Hangfire that reference
--          deleted or refactored services causing TypeLoadException errors
-- ============================================================================

-- Check for jobs referencing IVisibilityCheckService (old service)
SELECT 
    id,
    invocationdata,
    createdat
FROM hangfire.job
WHERE invocationdata::text LIKE '%IVisibilityCheckService%'
ORDER BY createdat DESC
LIMIT 10;

-- Remove old recurring job entries for IVisibilityCheckService
-- This service was merged into IPromptTrackingService
DELETE FROM hangfire.set
WHERE key = 'recurring-jobs' 
AND value::text LIKE '%IVisibilityCheckService%';

-- Alternative: Remove by specific job name if known
-- DELETE FROM hangfire.set
-- WHERE key = 'recurring-jobs' 
-- AND value::text LIKE '%check-visibility%';

-- Verify cleanup - List all recurring jobs
SELECT 
    key,
    value,
    score
FROM hangfire.set
WHERE key = 'recurring-jobs'
ORDER BY score;

-- ============================================================================
-- IMPORTANT NOTES:
-- ============================================================================
-- 1. This error occurs when Hangfire tries to deserialize a job that 
--    references a type that no longer exists in the codebase
-- 2. The IVisibilityCheckService was likely refactored/merged into 
--    IPromptTrackingService
-- 3. After running this script, restart the application
-- 4. The new job "ai-visibility-citation-check" using IPromptTrackingService
--    should work correctly
-- ============================================================================

-- To manually remove a specific recurring job via Hangfire API:
-- RecurringJob.RemoveIfExists("old-job-id");
