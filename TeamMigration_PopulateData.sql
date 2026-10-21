-- ============================================================================
-- AISEO Team Implementation - Data Migration Script
-- This script populates Team data and migrates existing user data to teams
-- RUN THIS AFTER TeamMigration_CreateSchema.sql
-- ============================================================================

BEGIN TRANSACTION;

-- Step 1: Create a personal team for each existing user
-- This creates one team per user initially (they can add more users later)
INSERT INTO "Team" ("Name", "Description", "IsActive", "CreatedAt")
SELECT
    COALESCE("FirstName", '') || ' ' || COALESCE("LastName", 'User') || '''s Team' as "Name",
    'Personal team for ' || COALESCE("Email", 'User'),
    true,
    CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "User"
WHERE NOT EXISTS (
    -- Check if we haven't already created teams (for idempotency)
    SELECT 1 FROM "Team"
)
ON CONFLICT DO NOTHING;

-- Step 2: Create UserTeamMapping for each user to their personal team
-- Match users to their personal teams
INSERT INTO "UserTeamMapping" ("UserId", "TeamId", "Role", "JoinedAt", "IsActive", "CreatedAt")
SELECT
    u."Id" as "UserId",
    t."Id" as "TeamId",
    'Admin' as "Role", -- All existing users are admins of their personal teams
    CURRENT_TIMESTAMP AT TIME ZONE 'UTC' as "JoinedAt",
    true as "IsActive",
    CURRENT_TIMESTAMP AT TIME ZONE 'UTC' as "CreatedAt"
FROM "User" u
INNER JOIN "Team" t ON
    t."Name" = (COALESCE(u."FirstName", '') || ' ' || COALESCE(u."LastName", 'User') || '''s Team')
WHERE NOT EXISTS (
    -- Prevent duplicate entries (for idempotency)
    SELECT 1 FROM "UserTeamMapping"
    WHERE "UserId" = u."Id" AND "TeamId" = t."Id"
)
ON CONFLICT DO NOTHING;

-- Step 3: Update all existing project records to set TeamId from user's team
-- For each project, find the user's team and update the TeamId
UPDATE "Projects" p
SET "TeamId" = utc."TeamId"
FROM (
    SELECT u."Id" as "UserId", t."Id" as "TeamId"
    FROM "User" u
    INNER JOIN "Team" t ON
        t."Name" = (COALESCE(u."FirstName", '') || ' ' || COALESCE(u."LastName", 'User') || '''s Team')
) utc
WHERE p."UserId" = utc."UserId" AND p."TeamId" = 0;

-- Step 4: Update PromptTrackings - set TeamId from project's team
UPDATE "PromptTrackings" pt
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE pt."ProjectId" = p."Id" AND pt."TeamId" = 0;

-- Step 5: Update PromptPlatformTrackings - set TeamId from prompt's team
UPDATE "PromptPlatformTrackings" ppt
SET "TeamId" = pt."TeamId"
FROM "PromptTrackings" pt
WHERE ppt."PromptId" = pt."Id" AND ppt."TeamId" = 0;

-- Step 6: Update PromptVisibilityMetrics - set TeamId from prompt platform's team
UPDATE "PromptVisibilityMetrics" pvm
SET "TeamId" = ppt."TeamId"
FROM "PromptPlatformTrackings" ppt
WHERE pvm."PromptPlatformId" = ppt."Id" AND pvm."TeamId" = 0;

-- Step 7: Update Citations - set TeamId from project's team
UPDATE "Citations" c
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE c."ProjectId" = p."Id" AND c."TeamId" = 0;

-- Step 8: Update CitationMetrics - set TeamId from citation's team
UPDATE "CitationMetrics" cm
SET "TeamId" = c."TeamId"
FROM "Citations" c
WHERE cm."CitationId" = c."Id" AND cm."TeamId" = 0;

-- Step 9: Update BrandMentions - set TeamId from project's team
UPDATE "BrandMentions" bm
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE bm."ProjectId" = p."Id" AND bm."TeamId" = 0;

-- Step 10: Update ContentArticles - set TeamId from project's team
UPDATE "ContentArticles" ca
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE ca."ProjectId" = p."Id" AND ca."TeamId" = 0;

-- Step 11: Update SiteAuditRuns - set TeamId from project's team
UPDATE "SiteAuditRuns" sar
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE sar."ProjectId" = p."Id" AND sar."TeamId" = 0;

-- Step 12: Update SiteAuditIssues - set TeamId from SiteAuditRun's team
UPDATE "SiteAuditIssues" sai
SET "TeamId" = sar."TeamId"
FROM "SiteAuditRuns" sar
WHERE sai."SiteAuditRunId" = sar."Id" AND sai."TeamId" = 0;

-- Step 13: Update SiteAuditDetails - set TeamId from SiteAuditRun's team
UPDATE "SiteAuditDetails" sad
SET "TeamId" = sar."TeamId"
FROM "SiteAuditRuns" sar
WHERE sad."SiteAuditRunId" = sar."Id" AND sad."TeamId" = 0;

-- Step 14: Update SeoOpportunities - set TeamId from project's team
UPDATE "SeoOpportunities" so
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE so."ProjectId" = p."Id" AND so."TeamId" = 0;

-- Step 15: Update ContentGapOpportunities - set TeamId from project's team
UPDATE "ContentGapOpportunities" cgo
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE cgo."ProjectId" = p."Id" AND cgo."TeamId" = 0;

-- Step 16: Update VisibilityOpportunities - set TeamId from project's team
UPDATE "VisibilityOpportunities" vo
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE vo."ProjectId" = p."Id" AND vo."TeamId" = 0;

-- Step 17: Update OpportunityScans - set TeamId from project's team
UPDATE "OpportunityScans" os
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE os."ProjectId" = p."Id" AND os."TeamId" = 0;

-- Step 18: Update BrandProfiles - set TeamId from project's team
UPDATE "BrandProfiles" bp
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE bp."ProjectId" = p."Id" AND bp."TeamId" = 0;

-- Step 19: Update Competitors - set TeamId from project's team
UPDATE "Competitors" c
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE c."ProjectId" = p."Id" AND c."TeamId" = 0;

-- Step 20: Update CommunityOpportunities - set TeamId from project's team
UPDATE "CommunityOpportunities" co
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE co."ProjectId" = p."Id" AND co."TeamId" = 0;

-- Step 21: Update VisibilityDashboardMetrics - set TeamId from project's team
UPDATE "VisibilityDashboardMetrics" vdm
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE vdm."ProjectId" = p."Id" AND vdm."TeamId" = 0;

-- Step 22: Update VisibilityAlerts - set TeamId from project's team
UPDATE "VisibilityAlerts" va
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE va."ProjectId" = p."Id" AND va."TeamId" = 0;

-- Step 23: Update VisibilityTrends - set TeamId from project's team
UPDATE "VisibilityTrends" vt
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE vt."ProjectId" = p."Id" AND vt."TeamId" = 0;

-- Step 24: Update VisibilityGoals - set TeamId from project's team
UPDATE "VisibilityGoals" vg
SET "TeamId" = p."TeamId"
FROM "Projects" p
WHERE vg."ProjectId" = p."Id" AND vg."TeamId" = 0;

COMMIT;

-- ============================================================================
-- Data migration complete!
--
-- Summary of changes:
-- 1. Created one personal team per existing user
-- 2. Created UserTeamMapping entries for each user to their personal team
-- 3. Updated all data records to have TeamId set from their associated project's team
--
-- Next steps:
-- 1. Build and deploy the code changes
-- 2. Update your authentication to include TeamId in JWT claims
-- 3. Test team functionality with the migrated data
-- ============================================================================

-- Verify migration results (optional - run these SELECT statements to verify data):
-- SELECT COUNT(*) as "TotalTeams" FROM "Team";
-- SELECT COUNT(*) as "TotalUserTeamMappings" FROM "UserTeamMapping";
-- SELECT COUNT(*) as "ProjectsWithoutTeamId" FROM "Projects" WHERE "TeamId" = 0;
-- SELECT COUNT(*) as "PromptsWithoutTeamId" FROM "PromptTrackings" WHERE "TeamId" = 0;
