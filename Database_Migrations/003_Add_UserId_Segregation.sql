-- =====================================================
-- Migration: Add UserId columns for multi-user data segregation
-- Purpose: Ensure all multi-user tables have UserId for proper data isolation
-- Date: 2024
-- =====================================================

BEGIN;

-- =====================================================
-- 1. Add UserId to SiteAudits
-- =====================================================
ALTER TABLE "SiteAudits" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "SiteAudits" sa
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE sa."ProjectId" = p."Id"
AND sa."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "SiteAudits" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "SiteAudits"
ADD CONSTRAINT "FK_SiteAudits_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_SiteAudits_UserId" ON "SiteAudits"("UserId");
CREATE INDEX IF NOT EXISTS "IX_SiteAudits_UserId_ProjectId" ON "SiteAudits"("UserId", "ProjectId");

-- =====================================================
-- 2. Add UserId to SiteAuditRuns
-- =====================================================
ALTER TABLE "SiteAuditRuns" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "SiteAuditRuns" sar
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE sar."ProjectId" = p."Id"
AND sar."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "SiteAuditRuns" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "SiteAuditRuns"
ADD CONSTRAINT "FK_SiteAuditRuns_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_UserId" ON "SiteAuditRuns"("UserId");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_UserId_ProjectId" ON "SiteAuditRuns"("UserId", "ProjectId");

-- =====================================================
-- 3. Add UserId to SiteAuditIssues
-- =====================================================
ALTER TABLE "SiteAuditIssues" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from SiteAuditRuns table for existing records
UPDATE "SiteAuditIssues" sai
SET "UserId" = sar."UserId"
FROM "SiteAuditRuns" sar
WHERE sai."SiteAuditRunId" = sar."Id"
AND sai."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "SiteAuditIssues" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "SiteAuditIssues"
ADD CONSTRAINT "FK_SiteAuditIssues_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_SiteAuditIssues_UserId" ON "SiteAuditIssues"("UserId");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditIssues_UserId_IsCompleted" ON "SiteAuditIssues"("UserId", "IsCompleted");

-- =====================================================
-- 4. Add UserId to OpportunityScans
-- =====================================================
ALTER TABLE "OpportunityScans" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "OpportunityScans" os
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE os."ProjectId" = p."Id"
AND os."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "OpportunityScans" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "OpportunityScans"
ADD CONSTRAINT "FK_OpportunityScans_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_OpportunityScans_UserId" ON "OpportunityScans"("UserId");
CREATE INDEX IF NOT EXISTS "IX_OpportunityScans_UserId_ProjectId" ON "OpportunityScans"("UserId", "ProjectId");

-- =====================================================
-- 5. Add UserId to VisibilityOpportunities
-- =====================================================
ALTER TABLE "VisibilityOpportunities" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "VisibilityOpportunities" vo
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE vo."ProjectId" = p."Id"
AND vo."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "VisibilityOpportunities" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "VisibilityOpportunities"
ADD CONSTRAINT "FK_VisibilityOpportunities_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_VisibilityOpportunities_UserId" ON "VisibilityOpportunities"("UserId");
CREATE INDEX IF NOT EXISTS "IX_VisibilityOpportunities_UserId_Status" ON "VisibilityOpportunities"("UserId", "Status");

-- =====================================================
-- 6. Add UserId to ContentGapOpportunities
-- =====================================================
ALTER TABLE "ContentGapOpportunities" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "ContentGapOpportunities" cgo
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE cgo."ProjectId" = p."Id"
AND cgo."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "ContentGapOpportunities" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "ContentGapOpportunities"
ADD CONSTRAINT "FK_ContentGapOpportunities_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_ContentGapOpportunities_UserId" ON "ContentGapOpportunities"("UserId");
CREATE INDEX IF NOT EXISTS "IX_ContentGapOpportunities_UserId_Status" ON "ContentGapOpportunities"("UserId", "Status");

-- =====================================================
-- 7. Add UserId to SeoOpportunities
-- =====================================================
ALTER TABLE "SeoOpportunities" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "SeoOpportunities" so
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE so."ProjectId" = p."Id"
AND so."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "SeoOpportunities" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "SeoOpportunities"
ADD CONSTRAINT "FK_SeoOpportunities_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_SeoOpportunities_UserId" ON "SeoOpportunities"("UserId");
CREATE INDEX IF NOT EXISTS "IX_SeoOpportunities_UserId_Status" ON "SeoOpportunities"("UserId", "Status");

-- =====================================================
-- 8. Add UserId to Citations
-- =====================================================
ALTER TABLE "Citations" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "Citations" c
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE c."ProjectId" = p."Id"
AND c."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "Citations" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "Citations"
ADD CONSTRAINT "FK_Citations_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_Citations_UserId" ON "Citations"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Citations_UserId_ProjectId" ON "Citations"("UserId", "ProjectId");

-- =====================================================
-- 9. Add UserId to BrandMentions
-- =====================================================
ALTER TABLE "BrandMentions" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "BrandMentions" bm
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE bm."ProjectId" = p."Id"
AND bm."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "BrandMentions" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "BrandMentions"
ADD CONSTRAINT "FK_BrandMentions_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_BrandMentions_UserId" ON "BrandMentions"("UserId");
CREATE INDEX IF NOT EXISTS "IX_BrandMentions_UserId_ProjectId" ON "BrandMentions"("UserId", "ProjectId");

-- =====================================================
-- 10. Add UserId to ContentArticles
-- =====================================================
ALTER TABLE "ContentArticles" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "ContentArticles" ca
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE ca."ProjectId" = p."Id"
AND ca."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "ContentArticles" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "ContentArticles"
ADD CONSTRAINT "FK_ContentArticles_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_ContentArticles_UserId" ON "ContentArticles"("UserId");
CREATE INDEX IF NOT EXISTS "IX_ContentArticles_UserId_Status" ON "ContentArticles"("UserId", "Status");

-- =====================================================
-- 11. Add UserId to Keywords
-- =====================================================
ALTER TABLE "Keywords" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Populate UserId from Projects table for existing records
UPDATE "Keywords" k
SET "UserId" = p."UserId"
FROM "Projects" p
WHERE k."ProjectId" = p."Id"
AND k."UserId" IS NULL;

-- Make UserId NOT NULL after populating
ALTER TABLE "Keywords" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "Keywords"
ADD CONSTRAINT "FK_Keywords_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_Keywords_UserId" ON "Keywords"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Keywords_UserId_ProjectId" ON "Keywords"("UserId", "ProjectId");

-- =====================================================
-- 12. Rename CreatedByUserId to UserId in PromptTrackings for consistency
-- =====================================================
-- Note: PromptTrackings already has CreatedByUserId, we'll add UserId and copy the value
ALTER TABLE "PromptTrackings" 
ADD COLUMN IF NOT EXISTS "UserId" INTEGER;

-- Copy CreatedByUserId to UserId
UPDATE "PromptTrackings"
SET "UserId" = "CreatedByUserId"
WHERE "UserId" IS NULL;

-- Make UserId NOT NULL
ALTER TABLE "PromptTrackings" 
ALTER COLUMN "UserId" SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE "PromptTrackings"
ADD CONSTRAINT "FK_PromptTrackings_Users_UserId" 
FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IF NOT EXISTS "IX_PromptTrackings_UserId" ON "PromptTrackings"("UserId");
CREATE INDEX IF NOT EXISTS "IX_PromptTrackings_UserId_ProjectId" ON "PromptTrackings"("UserId", "ProjectId");

-- =====================================================
-- Summary of Changes
-- =====================================================
-- Tables updated with UserId column:
-- 1. SiteAudits
-- 2. SiteAuditRuns
-- 3. SiteAuditIssues
-- 4. OpportunityScans
-- 5. VisibilityOpportunities
-- 6. ContentGapOpportunities
-- 7. SeoOpportunities
-- 8. Citations
-- 9. BrandMentions
-- 10. ContentArticles
-- 11. Keywords
-- 12. PromptTrackings (added UserId alongside existing CreatedByUserId)
--
-- All tables now have:
-- - UserId column (NOT NULL)
-- - Foreign key to Users table with CASCADE delete
-- - Indexes for query performance
-- - Composite indexes for common query patterns

COMMIT;

-- =====================================================
-- Verification Queries (Run these after migration)
-- =====================================================
-- Verify all tables have UserId populated
-- SELECT 'SiteAudits' as table_name, COUNT(*) as total, COUNT("UserId") as with_userid FROM "SiteAudits"
-- UNION ALL
-- SELECT 'SiteAuditRuns', COUNT(*), COUNT("UserId") FROM "SiteAuditRuns"
-- UNION ALL
-- SELECT 'SiteAuditIssues', COUNT(*), COUNT("UserId") FROM "SiteAuditIssues"
-- UNION ALL
-- SELECT 'OpportunityScans', COUNT(*), COUNT("UserId") FROM "OpportunityScans"
-- UNION ALL
-- SELECT 'VisibilityOpportunities', COUNT(*), COUNT("UserId") FROM "VisibilityOpportunities"
-- UNION ALL
-- SELECT 'ContentGapOpportunities', COUNT(*), COUNT("UserId") FROM "ContentGapOpportunities"
-- UNION ALL
-- SELECT 'SeoOpportunities', COUNT(*), COUNT("UserId") FROM "SeoOpportunities"
-- UNION ALL
-- SELECT 'Citations', COUNT(*), COUNT("UserId") FROM "Citations"
-- UNION ALL
-- SELECT 'BrandMentions', COUNT(*), COUNT("UserId") FROM "BrandMentions"
-- UNION ALL
-- SELECT 'ContentArticles', COUNT(*), COUNT("UserId") FROM "ContentArticles"
-- UNION ALL
-- SELECT 'Keywords', COUNT(*), COUNT("UserId") FROM "Keywords"
-- UNION ALL
-- SELECT 'PromptTrackings', COUNT(*), COUNT("UserId") FROM "PromptTrackings";
