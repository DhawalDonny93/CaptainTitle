-- ============================================================================
-- AISEO Team Implementation - Schema Creation Script
-- This script creates the Team infrastructure and adds TeamId columns
-- Run this script BEFORE running the data migration script
-- ============================================================================

-- Step 1: Create Team table
CREATE TABLE IF NOT EXISTS "Team" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "Description" VARCHAR(1000) NULL,
    "StripeCustomerId" VARCHAR(255) NULL,
    "IsActive" BOOLEAN DEFAULT true NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP AT TIME ZONE 'UTC' NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

-- Create index on Name for searching
CREATE INDEX IF NOT EXISTS "IX_Team_Name" ON "Team"("Name");
CREATE INDEX IF NOT EXISTS "IX_Team_IsActive" ON "Team"("IsActive");
CREATE INDEX IF NOT EXISTS "IX_Team_StripeCustomerId" ON "Team"("StripeCustomerId") WHERE "StripeCustomerId" IS NOT NULL;

-- Step 2: Create UserTeamMapping table
CREATE TABLE IF NOT EXISTS "UserTeamMapping" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "TeamId" INTEGER NOT NULL,
    "Role" VARCHAR(50) DEFAULT 'Member' NOT NULL, -- Admin, Manager, Member
    "JoinedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP AT TIME ZONE 'UTC' NOT NULL,
    "InvitedBy" INTEGER NULL,
    "IsActive" BOOLEAN DEFAULT true NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP AT TIME ZONE 'UTC' NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "FK_UserTeamMapping_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserTeamMapping_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE
);

-- Create unique index to prevent duplicate user-team associations
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserTeamMapping_UserId_TeamId" ON "UserTeamMapping"("UserId", "TeamId");
CREATE INDEX IF NOT EXISTS "IX_UserTeamMapping_TeamId" ON "UserTeamMapping"("TeamId");
CREATE INDEX IF NOT EXISTS "IX_UserTeamMapping_Role" ON "UserTeamMapping"("Role");
CREATE INDEX IF NOT EXISTS "IX_UserTeamMapping_IsActive" ON "UserTeamMapping"("IsActive");

-- Step 3: Create PromptEditHistory table
CREATE TABLE IF NOT EXISTS "PromptEditHistory" (
    "Id" SERIAL PRIMARY KEY,
    "PromptTrackingId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "FieldName" VARCHAR(100) NOT NULL,
    "OldValue" TEXT NULL,
    "NewValue" TEXT NULL,
    "ChangeType" VARCHAR(50) NOT NULL, -- Created, Updated, Deleted, StatusChanged
    "Reason" VARCHAR(500) NULL,
    "IpAddress" VARCHAR(45) NULL,
    "ChangedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP AT TIME ZONE 'UTC' NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP AT TIME ZONE 'UTC' NOT NULL,
    CONSTRAINT "FK_PromptEditHistory_PromptTracking" FOREIGN KEY ("PromptTrackingId") REFERENCES "PromptTrackings"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PromptEditHistory_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS "IX_PromptEditHistory_PromptTrackingId" ON "PromptEditHistory"("PromptTrackingId");
CREATE INDEX IF NOT EXISTS "IX_PromptEditHistory_UserId" ON "PromptEditHistory"("UserId");
CREATE INDEX IF NOT EXISTS "IX_PromptEditHistory_ChangedAt" ON "PromptEditHistory"("ChangedAt");
CREATE INDEX IF NOT EXISTS "IX_PromptEditHistory_ChangeType" ON "PromptEditHistory"("ChangeType");

-- Step 4: Add TeamId columns to existing tables
-- Project table
ALTER TABLE "Projects" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "Projects" ADD CONSTRAINT "FK_Projects_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_Projects_TeamId" ON "Projects"("TeamId");

-- PromptTrackings table
ALTER TABLE "PromptTrackings" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "PromptTrackings" ADD CONSTRAINT "FK_PromptTrackings_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_PromptTrackings_TeamId" ON "PromptTrackings"("TeamId");

-- PromptPlatformTrackings table
ALTER TABLE "PromptPlatformTrackings" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "PromptPlatformTrackings" ADD CONSTRAINT "FK_PromptPlatformTrackings_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_PromptPlatformTrackings_TeamId" ON "PromptPlatformTrackings"("TeamId");

-- PromptVisibilityMetrics table
ALTER TABLE "PromptVisibilityMetrics" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "PromptVisibilityMetrics" ADD CONSTRAINT "FK_PromptVisibilityMetrics_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_PromptVisibilityMetrics_TeamId" ON "PromptVisibilityMetrics"("TeamId");

-- Citations table
ALTER TABLE "Citations" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "Citations" ADD CONSTRAINT "FK_Citations_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_Citations_TeamId" ON "Citations"("TeamId");

-- BrandMentions table
ALTER TABLE "BrandMentions" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "BrandMentions" ADD CONSTRAINT "FK_BrandMentions_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_BrandMentions_TeamId" ON "BrandMentions"("TeamId");

-- ContentArticles table
ALTER TABLE "ContentArticles" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "ContentArticles" ADD CONSTRAINT "FK_ContentArticles_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_ContentArticles_TeamId" ON "ContentArticles"("TeamId");

-- SiteAuditRuns table
ALTER TABLE "SiteAuditRuns" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "SiteAuditRuns" ADD CONSTRAINT "FK_SiteAuditRuns_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_TeamId" ON "SiteAuditRuns"("TeamId");

-- SiteAuditIssues table
ALTER TABLE "SiteAuditIssues" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "SiteAuditIssues" ADD CONSTRAINT "FK_SiteAuditIssues_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_SiteAuditIssues_TeamId" ON "SiteAuditIssues"("TeamId");

-- SiteAuditDetails table
ALTER TABLE "SiteAuditDetails" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "SiteAuditDetails" ADD CONSTRAINT "FK_SiteAuditDetails_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_TeamId" ON "SiteAuditDetails"("TeamId");

-- CitationMetrics table
ALTER TABLE "CitationMetrics" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "CitationMetrics" ADD CONSTRAINT "FK_CitationMetrics_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_CitationMetrics_TeamId" ON "CitationMetrics"("TeamId");

-- SeoOpportunities table
ALTER TABLE "SeoOpportunities" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "SeoOpportunities" ADD CONSTRAINT "FK_SeoOpportunities_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_SeoOpportunities_TeamId" ON "SeoOpportunities"("TeamId");

-- ContentGapOpportunities table
ALTER TABLE "ContentGapOpportunities" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "ContentGapOpportunities" ADD CONSTRAINT "FK_ContentGapOpportunities_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_ContentGapOpportunities_TeamId" ON "ContentGapOpportunities"("TeamId");

-- VisibilityOpportunities table
ALTER TABLE "VisibilityOpportunities" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "VisibilityOpportunities" ADD CONSTRAINT "FK_VisibilityOpportunities_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_VisibilityOpportunities_TeamId" ON "VisibilityOpportunities"("TeamId");

-- OpportunityScans table
ALTER TABLE "OpportunityScans" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "OpportunityScans" ADD CONSTRAINT "FK_OpportunityScans_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_OpportunityScans_TeamId" ON "OpportunityScans"("TeamId");

-- BrandProfiles table
ALTER TABLE "BrandProfiles" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "BrandProfiles" ADD CONSTRAINT "FK_BrandProfiles_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_BrandProfiles_TeamId" ON "BrandProfiles"("TeamId");

-- Competitors table
ALTER TABLE "Competitors" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "Competitors" ADD CONSTRAINT "FK_Competitors_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_Competitors_TeamId" ON "Competitors"("TeamId");

-- CommunityOpportunities table
ALTER TABLE "CommunityOpportunities" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "CommunityOpportunities" ADD CONSTRAINT "FK_CommunityOpportunities_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_TeamId" ON "CommunityOpportunities"("TeamId");

-- VisibilityDashboardMetrics table
ALTER TABLE "VisibilityDashboardMetrics" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "VisibilityDashboardMetrics" ADD CONSTRAINT "FK_VisibilityDashboardMetrics_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_VisibilityDashboardMetrics_TeamId" ON "VisibilityDashboardMetrics"("TeamId");

-- VisibilityAlerts table
ALTER TABLE "VisibilityAlerts" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "VisibilityAlerts" ADD CONSTRAINT "FK_VisibilityAlerts_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_VisibilityAlerts_TeamId" ON "VisibilityAlerts"("TeamId");

-- VisibilityTrends table
ALTER TABLE "VisibilityTrends" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "VisibilityTrends" ADD CONSTRAINT "FK_VisibilityTrends_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_VisibilityTrends_TeamId" ON "VisibilityTrends"("TeamId");

-- VisibilityGoals table
ALTER TABLE "VisibilityGoals" ADD COLUMN IF NOT EXISTS "TeamId" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "VisibilityGoals" ADD CONSTRAINT "FK_VisibilityGoals_Team" FOREIGN KEY ("TeamId") REFERENCES "Team"("Id") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS "IX_VisibilityGoals_TeamId" ON "VisibilityGoals"("TeamId");

-- ============================================================================
-- Schema creation complete!
-- Next step: Run TeamMigration_PopulateData.sql to migrate existing data
-- ============================================================================
