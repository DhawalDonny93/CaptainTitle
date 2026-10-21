-- ============================================================================
-- Fix CommunityOpportunities Table Schema
-- Date: 2026-02-09
-- Description: Consolidate CommunityOpportunity table naming and ensure all columns exist
-- ============================================================================

BEGIN;

-- ============================================================================
-- Step 1: Rename table from singular to plural (if needed)
-- ============================================================================

DO $$ 
BEGIN
    -- Check if old singular table exists
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'CommunityOpportunity') THEN
        -- Check if new plural table doesn't exist
        IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'CommunityOpportunities') THEN
            -- Rename the table
            ALTER TABLE "CommunityOpportunity" RENAME TO "CommunityOpportunities";
            RAISE NOTICE 'Renamed table from CommunityOpportunity to CommunityOpportunities';
        ELSE
            RAISE NOTICE 'Both tables exist - manual intervention required';
        END IF;
    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'CommunityOpportunities') THEN
        -- Neither table exists, create the plural version
        CREATE TABLE "CommunityOpportunities" (
            "Id" SERIAL PRIMARY KEY,
            "ProjectId" INTEGER NOT NULL,
            "KeywordId" INTEGER,
            "Platform" VARCHAR(50) NOT NULL,
            "Title" VARCHAR(300) NOT NULL,
            "Url" TEXT NOT NULL,
            "Summary" TEXT,
            "Sentiment" VARCHAR(50),
            "Status" VARCHAR(20) NOT NULL DEFAULT 'New',
            "DraftReply" TEXT,
            "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT "FK_CommunityOpportunities_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
                REFERENCES "Projects"("Id") ON DELETE CASCADE
        );
        RAISE NOTICE 'Created new CommunityOpportunities table';
    ELSE
        RAISE NOTICE 'CommunityOpportunities table already exists';
    END IF;
END $$;

-- ============================================================================
-- Step 2: Ensure all required columns exist
-- ============================================================================

DO $$ 
BEGIN
    -- Priority column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunities' AND column_name = 'Priority') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Priority" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added Priority column';
    END IF;
    
    -- EngagementScore column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunities' AND column_name = 'EngagementScore') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "EngagementScore" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added EngagementScore column';
    END IF;
    
    -- Upvotes column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunities' AND column_name = 'Upvotes') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Upvotes" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added Upvotes column';
    END IF;
    
    -- Comments column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunities' AND column_name = 'Comments') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Comments" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added Comments column';
    END IF;
    
    -- Views column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunities' AND column_name = 'Views') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Views" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added Views column';
    END IF;
    
    -- PostedAt column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunities' AND column_name = 'PostedAt') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "PostedAt" TIMESTAMP WITH TIME ZONE NULL;
        RAISE NOTICE 'Added PostedAt column';
    END IF;
    
    -- LastCheckedAt column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunities' AND column_name = 'LastCheckedAt') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "LastCheckedAt" TIMESTAMP WITH TIME ZONE NULL;
        RAISE NOTICE 'Added LastCheckedAt column';
    END IF;
    
    -- IsArchived column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunities' AND column_name = 'IsArchived') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "IsArchived" BOOLEAN NOT NULL DEFAULT FALSE;
        RAISE NOTICE 'Added IsArchived column';
    END IF;
    
    -- AssignedToUserId column (THIS IS THE MISSING COLUMN FROM THE ERROR)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunities' AND column_name = 'AssignedToUserId') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "AssignedToUserId" INTEGER NULL;
        RAISE NOTICE 'Added AssignedToUserId column';
    END IF;
END $$;

-- ============================================================================
-- Step 3: Create all required indexes
-- ============================================================================

-- Base indexes
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_ProjectId" ON "CommunityOpportunities"("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Platform" ON "CommunityOpportunities"("Platform");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Status" ON "CommunityOpportunities"("Status");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_CreatedAt" ON "CommunityOpportunities"("CreatedAt");

-- Phase 5 indexes
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Priority" ON "CommunityOpportunities"("Priority");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_EngagementScore" ON "CommunityOpportunities"("EngagementScore");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_IsArchived" ON "CommunityOpportunities"("IsArchived");

-- ============================================================================
-- Step 4: Drop old indexes from singular table name (if they exist)
-- ============================================================================

DO $$
BEGIN
    -- Drop old singular table indexes if they exist
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_ProjectId') THEN
        DROP INDEX "IX_CommunityOpportunity_ProjectId";
    END IF;
    
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_Platform') THEN
        DROP INDEX "IX_CommunityOpportunity_Platform";
    END IF;
    
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_Status') THEN
        DROP INDEX "IX_CommunityOpportunity_Status";
    END IF;
    
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_Priority') THEN
        DROP INDEX "IX_CommunityOpportunity_Priority";
    END IF;
    
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_EngagementScore') THEN
        DROP INDEX "IX_CommunityOpportunity_EngagementScore";
    END IF;
    
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_IsArchived') THEN
        DROP INDEX "IX_CommunityOpportunity_IsArchived";
    END IF;
END $$;

COMMIT;

-- ============================================================================
-- Verification Query
-- ============================================================================

SELECT 
    'CommunityOpportunities Table Structure' as info,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'CommunityOpportunities'
ORDER BY ordinal_position;
