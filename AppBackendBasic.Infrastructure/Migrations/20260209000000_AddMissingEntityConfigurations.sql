-- Migration: AddMissingEntityConfigurations
-- Date: 2026-02-09
-- Description: Add missing tables and columns for CommunityOpportunity, SiteAuditRun, SiteAuditDetail entities

-- Add missing column to CommunityOpportunities table
ALTER TABLE "CommunityOpportunities" 
ADD COLUMN IF NOT EXISTS "AssignedToUserId" integer NULL;

-- Create SiteAuditRuns table
CREATE TABLE IF NOT EXISTS "SiteAuditRuns" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" integer NOT NULL,
    "Url" text NOT NULL,
    "Status" character varying(20) NOT NULL,
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

-- Create SiteAuditDetails table
CREATE TABLE IF NOT EXISTS "SiteAuditDetails" (
    "Id" SERIAL PRIMARY KEY,
    "SiteAuditRunId" integer NOT NULL,
    "CheckName" character varying(255) NOT NULL,
    "CheckType" character varying(100) NOT NULL,
    "Status" character varying(20) NOT NULL,
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

-- Create indexes for SiteAuditRuns
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_ProjectId" ON "SiteAuditRuns" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_Status" ON "SiteAuditRuns" ("Status");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditRuns_CreatedAt" ON "SiteAuditRuns" ("CreatedAt");

-- Create indexes for SiteAuditDetails
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_SiteAuditRunId" ON "SiteAuditDetails" ("SiteAuditRunId");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_CheckType" ON "SiteAuditDetails" ("CheckType");
CREATE INDEX IF NOT EXISTS "IX_SiteAuditDetails_Status" ON "SiteAuditDetails" ("Status");

-- Create indexes for CommunityOpportunities
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Priority" ON "CommunityOpportunities" ("Priority");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_EngagementScore" ON "CommunityOpportunities" ("EngagementScore");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_IsArchived" ON "CommunityOpportunities" ("IsArchived");

-- Create indexes for UserTasks
CREATE INDEX IF NOT EXISTS "IX_UserTasks_Priority" ON "UserTasks" ("Priority");
CREATE INDEX IF NOT EXISTS "IX_UserTasks_IsCompleted" ON "UserTasks" ("IsCompleted");
CREATE INDEX IF NOT EXISTS "IX_UserTasks_DueDate" ON "UserTasks" ("DueDate");

-- Insert migration record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260209000000_AddMissingEntityConfigurations', '8.0.0')
ON CONFLICT DO NOTHING;
