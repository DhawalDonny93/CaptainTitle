-- ============================================================================
-- Database Schema Fix Script
-- Date: 2026-02-09
-- Description: Fix missing tables and columns for AISEO application
-- ============================================================================

-- Start transaction
BEGIN;

-- ============================================================================
-- 1. Fix CommunityOpportunities table
-- ============================================================================

-- Add missing column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CommunityOpportunities' 
        AND column_name = 'AssignedToUserId'
    ) THEN
        ALTER TABLE "CommunityOpportunities" 
        ADD COLUMN "AssignedToUserId" integer NULL;
        RAISE NOTICE 'Added AssignedToUserId column to CommunityOpportunities';
    ELSE
        RAISE NOTICE 'AssignedToUserId column already exists in CommunityOpportunities';
    END IF;
END $$;

-- Add missing columns if they don't exist (Phase 5 additions)
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'Priority') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Priority" integer NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'EngagementScore') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "EngagementScore" integer NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'Upvotes') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Upvotes" integer NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'Comments') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Comments" integer NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'Views') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "Views" integer NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'PostedAt') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "PostedAt" timestamp with time zone NULL;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'LastCheckedAt') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "LastCheckedAt" timestamp with time zone NULL;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CommunityOpportunities' AND column_name = 'IsArchived') THEN
        ALTER TABLE "CommunityOpportunities" ADD COLUMN "IsArchived" boolean NOT NULL DEFAULT false;
    END IF;
END $$;

-- Create indexes for CommunityOpportunities
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Priority" ON "CommunityOpportunities" ("Priority");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_EngagementScore" ON "CommunityOpportunities" ("EngagementScore");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_IsArchived" ON "CommunityOpportunities" ("IsArchived");

-- ============================================================================
-- 2. Create SiteAuditRuns table
-- ============================================================================

CREATE TABLE IF NOT EXISTS "SiteAuditRuns" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" integer NOT NULL,
    "Url" text NOT NULL,
    "Status" character varying(20) NOT NULL DEFAULT 'Pending',
    "TotalChecks" integer NOT NULL DEFAULT 0,
    "PassedChecks" integer NOT NULL DEFAULT 0,
    "FailedChecks" integer NOT NULL DEFAULT 0,
    "WarningChecks" integer NOT NULL DEFAULT 0,
    "OverallScore" integer NOT NULL DEFAULT 0,
    "StartedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    "ErrorMessage" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditRuns_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

-- Create indexes for SiteAuditRuns
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_ProjectId" ON "SiteAuditRuns" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_Status" ON "SiteAuditRuns" ("Status");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_CreatedAt" ON "SiteAuditRuns" ("CreatedAt");

-- ============================================================================
-- 3. Create SiteAuditDetails table
-- ============================================================================

CREATE TABLE IF NOT EXISTS "SiteAuditDetails" (
    "Id" SERIAL PRIMARY KEY,
    "SiteAuditRunId" integer NOT NULL,
    "CheckName" character varying(255) NOT NULL,
    "CheckType" character varying(100) NOT NULL,
    "Status" character varying(20) NOT NULL DEFAULT 'Info',
    "Score" integer NOT NULL DEFAULT 0,
    "Message" text NULL,
    "DetailsJson" text NULL,
    "Recommendation" text NULL,
    "AutoFixAvailable" boolean NOT NULL DEFAULT false,
    "Priority" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditDetails_SiteAuditRuns_SiteAuditRunId" FOREIGN KEY ("SiteAuditRunId") 
        REFERENCES "SiteAuditRuns" ("Id") ON DELETE CASCADE
);

-- Create indexes for SiteAuditDetails
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_SiteAuditRunId" ON "SiteAuditDetails" ("SiteAuditRunId");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_CheckType" ON "SiteAuditDetails" ("CheckType");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_Status" ON "SiteAuditDetails" ("Status");

-- ============================================================================
-- 4. Add indexes for UserTasks table (if table exists)
-- ============================================================================

DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'UserTasks') THEN
        CREATE INDEX IF NOT EXISTS "IX_UserTasks_Priority" ON "UserTasks" ("Priority");
        CREATE INDEX IF NOT EXISTS "IX_UserTasks_IsCompleted" ON "UserTasks" ("IsCompleted");
        CREATE INDEX IF NOT EXISTS "IX_UserTasks_DueDate" ON "UserTasks" ("DueDate");
        RAISE NOTICE 'Created indexes for UserTasks table';
    ELSE
        RAISE NOTICE 'UserTasks table does not exist, skipping index creation';
    END IF;
END $$;

-- ============================================================================
-- Commit transaction
-- ============================================================================

COMMIT;

-- ============================================================================
-- Verification queries (optional - uncomment to run)
-- ============================================================================

-- Verify CommunityOpportunities columns
-- SELECT column_name, data_type, is_nullable
-- FROM information_schema.columns 
-- WHERE table_name = 'CommunityOpportunities'
-- ORDER BY ordinal_position;

-- Verify SiteAuditRuns table
-- SELECT column_name, data_type, is_nullable
-- FROM information_schema.columns 
-- WHERE table_name = 'SiteAuditRuns'
-- ORDER BY ordinal_position;

-- Verify SiteAuditDetails table
-- SELECT column_name, data_type, is_nullable
-- FROM information_schema.columns 
-- WHERE table_name = 'SiteAuditDetails'
-- ORDER BY ordinal_position;

-- Verify indexes
-- SELECT 
--     schemaname,
--     tablename,
--     indexname,
--     indexdef
-- FROM pg_indexes 
-- WHERE tablename IN ('CommunityOpportunities', 'SiteAuditRuns', 'SiteAuditDetails', 'UserTasks')
-- ORDER BY tablename, indexname;

-- ============================================================================
-- Script completed successfully
-- ============================================================================
