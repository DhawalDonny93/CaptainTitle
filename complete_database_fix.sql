-- ============================================================================
-- Complete Database Schema Fix
-- Date: 2026-02-09
-- Description: Fix all database issues - CommunityOpportunities, SiteAuditRuns, SiteAuditDetails
-- ============================================================================

BEGIN;

-- ============================================================================
-- PART 1: Fix CommunityOpportunities Table
-- ============================================================================

-- Step 1: Rename table from singular to plural (if needed)
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'CommunityOpportunity') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'CommunityOpportunities') THEN
            ALTER TABLE "CommunityOpportunity" RENAME TO "CommunityOpportunities";
            RAISE NOTICE 'Renamed CommunityOpportunity to CommunityOpportunities';
        ELSE
            RAISE WARNING 'Both CommunityOpportunity and CommunityOpportunities exist - manual intervention required';
        END IF;
    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'CommunityOpportunities') THEN
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
        RAISE NOTICE 'Created CommunityOpportunities table';
    END IF;
END $$;

-- Step 2: Add all required columns
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'Priority') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Priority" INTEGER NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'EngagementScore') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "EngagementScore" INTEGER NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'Upvotes') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Upvotes" INTEGER NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'Comments') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Comments" INTEGER NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'Views') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Views" INTEGER NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'PostedAt') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "PostedAt" TIMESTAMP WITH TIME ZONE NULL;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'LastCheckedAt') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "LastCheckedAt" TIMESTAMP WITH TIME ZONE NULL;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'IsArchived') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "IsArchived" BOOLEAN NOT NULL DEFAULT FALSE;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'AssignedToUserId') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "AssignedToUserId" INTEGER NULL;
    END IF;
END $$;

-- Step 3: Create indexes for CommunityOpportunities
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_ProjectId" ON "CommunityOpportunities"("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Platform" ON "CommunityOpportunities"("Platform");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Status" ON "CommunityOpportunities"("Status");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_CreatedAt" ON "CommunityOpportunities"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Priority" ON "CommunityOpportunities"("Priority");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_EngagementScore" ON "CommunityOpportunities"("EngagementScore");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_IsArchived" ON "CommunityOpportunities"("IsArchived");

-- Step 4: Drop old singular table indexes
DROP INDEX IF EXISTS "IX_CommunityOpportunity_ProjectId";
DROP INDEX IF EXISTS "IX_CommunityOpportunity_Platform";
DROP INDEX IF EXISTS "IX_CommunityOpportunity_Status";
DROP INDEX IF EXISTS "IX_CommunityOpportunity_Priority";
DROP INDEX IF EXISTS "IX_CommunityOpportunity_EngagementScore";
DROP INDEX IF EXISTS "IX_CommunityOpportunity_IsArchived";

-- ============================================================================
-- PART 2: Create SiteAuditRuns Table
-- ============================================================================

CREATE TABLE IF NOT EXISTS "SiteAuditRuns" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Url" TEXT NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "TotalChecks" INTEGER NOT NULL DEFAULT 0,
    "PassedChecks" INTEGER NOT NULL DEFAULT 0,
    "FailedChecks" INTEGER NOT NULL DEFAULT 0,
    "WarningChecks" INTEGER NOT NULL DEFAULT 0,
    "OverallScore" INTEGER NOT NULL DEFAULT 0,
    "StartedAt" TIMESTAMP WITH TIME ZONE NULL,
    "CompletedAt" TIMESTAMP WITH TIME ZONE NULL,
    "ErrorMessage" TEXT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditRuns_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

-- Create indexes for SiteAuditRuns
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_ProjectId" ON "SiteAuditRuns"("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_Status" ON "SiteAuditRuns"("Status");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_CreatedAt" ON "SiteAuditRuns"("CreatedAt");

-- ============================================================================
-- PART 3: Create SiteAuditDetails Table
-- ============================================================================

CREATE TABLE IF NOT EXISTS "SiteAuditDetails" (
    "Id" SERIAL PRIMARY KEY,
    "SiteAuditRunId" INTEGER NOT NULL,
    "CheckName" VARCHAR(255) NOT NULL,
    "CheckType" VARCHAR(100) NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Info',
    "Score" INTEGER NOT NULL DEFAULT 0,
    "Message" TEXT NULL,
    "DetailsJson" TEXT NULL,
    "Recommendation" TEXT NULL,
    "AutoFixAvailable" BOOLEAN NOT NULL DEFAULT FALSE,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditDetails_SiteAuditRuns_SiteAuditRunId" FOREIGN KEY ("SiteAuditRunId") 
        REFERENCES "SiteAuditRuns"("Id") ON DELETE CASCADE
);

-- Create indexes for SiteAuditDetails
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_SiteAuditRunId" ON "SiteAuditDetails"("SiteAuditRunId");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_CheckType" ON "SiteAuditDetails"("CheckType");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_Status" ON "SiteAuditDetails"("Status");

-- ============================================================================
-- PART 4: Add indexes for UserTasks (if table exists)
-- ============================================================================

DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'UserTasks') THEN
        CREATE INDEX IF NOT EXISTS "IX_UserTasks_Priority" ON "UserTasks"("Priority");
        CREATE INDEX IF NOT EXISTS "IX_UserTasks_IsCompleted" ON "UserTasks"("IsCompleted");
        CREATE INDEX IF NOT EXISTS "IX_UserTasks_DueDate" ON "UserTasks"("DueDate");
    END IF;
END $$;

COMMIT;

-- ============================================================================
-- Verification Queries
-- ============================================================================

-- Check CommunityOpportunities structure
SELECT 'CommunityOpportunities Columns:' as info;
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'CommunityOpportunities'
ORDER BY ordinal_position;

-- Check SiteAuditRuns structure
SELECT 'SiteAuditRuns Columns:' as info;
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'SiteAuditRuns'
ORDER BY ordinal_position;

-- Check SiteAuditDetails structure
SELECT 'SiteAuditDetails Columns:' as info;
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'SiteAuditDetails'
ORDER BY ordinal_position;

-- Check all indexes
SELECT 'All Indexes:' as info;
SELECT tablename, indexname
FROM pg_indexes 
WHERE tablename IN ('CommunityOpportunities', 'SiteAuditRuns', 'SiteAuditDetails', 'UserTasks')
ORDER BY tablename, indexname;
