-- ============================================================================
-- Migration 12: Fix Entity Schema Mismatches
-- Purpose: Align database schema with updated C# entity classes
-- Date: 2024
-- ============================================================================

-- Add missing columns to Projects table for new features
ALTER TABLE "Projects" 
ADD COLUMN IF NOT EXISTS "WebsiteUrl" VARCHAR(255),
ADD COLUMN IF NOT EXISTS "Industry" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "CompetitorDomainsJson" TEXT,
ADD COLUMN IF NOT EXISTS "EnableAutoCrawl" BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "CrawlMaxPages" INTEGER,
ADD COLUMN IF NOT EXISTS "CrawlMaxDepth" INTEGER,
ADD COLUMN IF NOT EXISTS "EnableOpportunityDetection" BOOLEAN DEFAULT TRUE,
ADD COLUMN IF NOT EXISTS "EnableVisibilityAlerts" BOOLEAN DEFAULT TRUE,
ADD COLUMN IF NOT EXISTS "VisibilityCheckFrequency" VARCHAR(50) DEFAULT 'daily';

-- Update WebsiteUrl from Domain for existing projects
UPDATE "Projects" SET "WebsiteUrl" = "Domain" WHERE "WebsiteUrl" IS NULL AND "Domain" IS NOT NULL;

-- Add missing columns to PromptResearchData table
ALTER TABLE "PromptResearchData"
ADD COLUMN IF NOT EXISTS "ProjectId" INTEGER REFERENCES "Projects"("Id") ON DELETE CASCADE,
ADD COLUMN IF NOT EXISTS "Category" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "Status" VARCHAR(50) DEFAULT 'New',
ADD COLUMN IF NOT EXISTS "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;

-- Update RelevanceScore type if needed (from double to int)
-- Note: This may cause data loss if values are not integers
-- ALTER TABLE "PromptResearchData" ALTER COLUMN "RelevanceScore" TYPE INTEGER USING "RelevanceScore"::INTEGER;

-- Update EstimatedVolume type from int to varchar
ALTER TABLE "PromptResearchData" ALTER COLUMN "EstimatedVolume" TYPE VARCHAR(50);

-- Ensure VisibilityAlerts table matches migration schema
-- Remove Title column if it exists (not in migration schema)
ALTER TABLE "VisibilityAlerts" DROP COLUMN IF EXISTS "Title";
ALTER TABLE "VisibilityAlerts" DROP COLUMN IF EXISTS "MetricType";
ALTER TABLE "VisibilityAlerts" DROP COLUMN IF EXISTS "OldValue";
ALTER TABLE "VisibilityAlerts" DROP COLUMN IF EXISTS "NewValue";
ALTER TABLE "VisibilityAlerts" DROP COLUMN IF EXISTS "ChangePercentage";
ALTER TABLE "VisibilityAlerts" DROP COLUMN IF EXISTS "IsDismissed";

-- Add missing columns to VisibilityAlerts
ALTER TABLE "VisibilityAlerts"
ADD COLUMN IF NOT EXISTS "PromptText" TEXT,
ADD COLUMN IF NOT EXISTS "Platform" VARCHAR(50),
ADD COLUMN IF NOT EXISTS "DetailsJson" TEXT,
ADD COLUMN IF NOT EXISTS "ReadAt" TIMESTAMP WITH TIME ZONE;

-- Ensure VisibilityTrends table has correct schema
-- This should already be correct from migration 11, but verify
ALTER TABLE "VisibilityTrends"
ADD COLUMN IF NOT EXISTS "Platform" VARCHAR(50),
ADD COLUMN IF NOT EXISTS "Topic" VARCHAR(200),
ADD COLUMN IF NOT EXISTS "TotalPromptsChecked" INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS "PromptsWithMention" INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS "AverageVisibilityScore" DECIMAL(5,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS "AveragePosition" DECIMAL(5,2),
ADD COLUMN IF NOT EXISTS "PositiveMentions" INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS "NeutralMentions" INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS "NegativeMentions" INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS "CompetitorMentions" INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS "ShareOfVoice" DECIMAL(5,2);

-- Create indexes for new columns
CREATE INDEX IF NOT EXISTS "idx_projects_website_url" ON "Projects"("WebsiteUrl");
CREATE INDEX IF NOT EXISTS "idx_projects_industry" ON "Projects"("Industry");
CREATE INDEX IF NOT EXISTS "idx_prompt_research_project" ON "PromptResearchData"("ProjectId");
CREATE INDEX IF NOT EXISTS "idx_prompt_research_status" ON "PromptResearchData"("Status");
CREATE INDEX IF NOT EXISTS "idx_visibility_alerts_platform" ON "VisibilityAlerts"("Platform");

-- Add foreign key constraint for PromptResearchData if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'fk_prompt_research_project' 
        AND table_name = 'PromptResearchData'
    ) THEN
        ALTER TABLE "PromptResearchData"
        ADD CONSTRAINT "fk_prompt_research_project"
        FOREIGN KEY ("ProjectId") REFERENCES "Projects"("Id") ON DELETE CASCADE;
    END IF;
END $$;

-- Update existing data to set default values
UPDATE "PromptResearchData" SET "Status" = 'New' WHERE "Status" IS NULL;
UPDATE "Projects" SET "EnableOpportunityDetection" = TRUE WHERE "EnableOpportunityDetection" IS NULL;
UPDATE "Projects" SET "EnableVisibilityAlerts" = TRUE WHERE "EnableVisibilityAlerts" IS NULL;

-- ============================================================================
-- NOTES FOR MANUAL REVIEW:
-- ============================================================================
-- 1. CitationMetric entity is designed for individual citation tracking,
--    not for aggregated daily metrics. If you need aggregation, consider
--    creating a new table: CitationDailyAggregates
--
-- 2. VisibilityOpportunity.Priority is stored as VARCHAR (High/Medium/Low)
--    not as INTEGER. Services have been updated to match this.
--
-- 3. ContentGapOpportunity.EstimatedImpact is VARCHAR (High/Medium/Low)
--    and CompetitorCoverage is INTEGER, matching COMPLETE_AISEO_SCHEMA.sql
--
-- 4. Background jobs have been simplified to work with actual entity schemas.
--    CitationAggregationJob now only logs metrics instead of storing them.
--
-- 5. All navigation properties have been fixed:
--    - PromptPlatformTracking.Prompt (not PromptTracking)
--    - PromptPlatformTracking.PromptId (not PromptTrackingId)
--
-- ============================================================================

COMMIT;
