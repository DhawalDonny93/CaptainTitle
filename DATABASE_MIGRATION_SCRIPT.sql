-- ============================================================================
-- DATABASE CONSOLIDATION MIGRATION SCRIPT
-- ============================================================================
-- Purpose: Consolidate redundant tables to simplify database schema
-- 
-- Changes:
-- 1. Merge LlmUsageLog into LlmRequest (LlmUsageLog removed)
-- 2. Merge AiTrackingPrompt into PromptTracking (AiTrackingPrompt removed)
--
-- IMPORTANT: BACKUP YOUR DATABASE BEFORE RUNNING THIS SCRIPT!
-- ============================================================================

BEGIN;

-- ============================================================================
-- PHASE 1: Add missing fields to PromptTracking from AiTrackingPrompt
-- ============================================================================

-- Add new columns to PromptTracking table
ALTER TABLE "PromptTrackings" 
ADD COLUMN IF NOT EXISTS "CompetitorUrlsJson" TEXT,
ADD COLUMN IF NOT EXISTS "TrackingFrequency" VARCHAR(20),
ADD COLUMN IF NOT EXISTS "KeywordId" INTEGER;

-- Add foreign key constraint for KeywordId
ALTER TABLE "PromptTrackings"
ADD CONSTRAINT "FK_PromptTrackings_Keywords_KeywordId" 
FOREIGN KEY ("KeywordId") REFERENCES "Keywords"("Id") ON DELETE SET NULL;

-- Create index on KeywordId for better performance
CREATE INDEX IF NOT EXISTS "IX_PromptTrackings_KeywordId" ON "PromptTrackings"("KeywordId");

-- ============================================================================
-- PHASE 2: Migrate data from AiTrackingPrompts to PromptTrackings
-- ============================================================================

-- Check if AiTrackingPrompts table exists before migration
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'AiTrackingPrompts') THEN
        
        -- Migrate data from AiTrackingPrompts to PromptTrackings
        INSERT INTO "PromptTrackings" (
            "ProjectId",
            "PromptText",
            "Topic",
            "Status",
            "IsCompetitor",
            "CreatedByUserId",
            "IsActive",
            "LastCheckedAt",
            "CompetitorUrlsJson",
            "TrackingFrequency",
            "KeywordId",
            "CreatedAt",
            "UpdatedAt"
        )
        SELECT 
            "ProjectId",
            "PromptText",
            "Topic",
            "Status",
            false as "IsCompetitor", -- Default value
            (SELECT "UserId" FROM "Projects" WHERE "Id" = "AiTrackingPrompts"."ProjectId" LIMIT 1) as "CreatedByUserId",
            "IsActive",
            "LastCheckedAt",
            "CompetitorUrlsJson",
            "TrackingFrequency",
            "KeywordId",
            "CreatedAt",
            COALESCE("UpdatedAt", "CreatedAt") as "UpdatedAt"
        FROM "AiTrackingPrompts"
        WHERE NOT EXISTS (
            -- Avoid duplicates if script is run multiple times
            SELECT 1 FROM "PromptTrackings" pt 
            WHERE pt."PromptText" = "AiTrackingPrompts"."PromptText" 
            AND pt."ProjectId" = "AiTrackingPrompts"."ProjectId"
        );

        RAISE NOTICE 'Data migrated from AiTrackingPrompts to PromptTrackings';
    ELSE
        RAISE NOTICE 'AiTrackingPrompts table does not exist, skipping migration';
    END IF;
END $$;

-- ============================================================================
-- PHASE 3: Update AiTrackingResults to reference PromptTrackings
-- ============================================================================

-- Update foreign key in AiTrackingResults
-- Note: This assumes PromptId in AiTrackingResults corresponds to migrated prompts
-- If there are orphaned records, they will be handled by the constraint

DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'AiTrackingResults') THEN
        -- Drop old foreign key constraint if it exists
        ALTER TABLE "AiTrackingResults" 
        DROP CONSTRAINT IF EXISTS "FK_AiTrackingResults_AiTrackingPrompts_PromptId";
        
        -- Add new foreign key constraint to PromptTrackings
        ALTER TABLE "AiTrackingResults"
        ADD CONSTRAINT "FK_AiTrackingResults_PromptTrackings_PromptId"
        FOREIGN KEY ("PromptId") REFERENCES "PromptTrackings"("Id") ON DELETE CASCADE;
        
        RAISE NOTICE 'AiTrackingResults foreign key updated to reference PromptTrackings';
    END IF;
END $$;

-- ============================================================================
-- PHASE 4: Drop AiTrackingPrompts table
-- ============================================================================

-- Drop the AiTrackingPrompts table (data already migrated)
DROP TABLE IF EXISTS "AiTrackingPrompts" CASCADE;

RAISE NOTICE 'AiTrackingPrompts table dropped';

-- ============================================================================
-- PHASE 5: Drop LlmUsageLogs table (replaced by LlmRequests)
-- ============================================================================

-- LlmUsageLogs is completely replaced by LlmRequests
-- No data migration needed as LlmRequests is already being used
DROP TABLE IF EXISTS "LlmUsageLogs" CASCADE;

RAISE NOTICE 'LlmUsageLogs table dropped';

-- ============================================================================
-- PHASE 6: Update Keywords table navigation property
-- ============================================================================

-- Update any references in Keywords table if needed
-- (This is handled at the application level through Entity Framework)

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- Verify PromptTrackings has the new columns
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'PromptTrackings' 
AND column_name IN ('CompetitorUrlsJson', 'TrackingFrequency', 'KeywordId')
ORDER BY column_name;

-- Count records in PromptTrackings
SELECT COUNT(*) as "PromptTrackings_Count" FROM "PromptTrackings";

-- Count records in AiTrackingResults
SELECT COUNT(*) as "AiTrackingResults_Count" FROM "AiTrackingResults";

-- Verify AiTrackingPrompts is dropped
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_name = 'AiTrackingPrompts'
) as "AiTrackingPrompts_Exists";

-- Verify LlmUsageLogs is dropped
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_name = 'LlmUsageLogs'
) as "LlmUsageLogs_Exists";

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
