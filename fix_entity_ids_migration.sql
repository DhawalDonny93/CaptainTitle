-- ============================================
-- AuraSEO Database Schema Migration
-- Purpose: Convert all Guid IDs to Integer IDs
-- ============================================
-- WARNING: This will DROP and RECREATE tables
-- Backup your data before running!
-- ============================================

BEGIN;

-- ============================================
-- STEP 1: Drop all dependent tables first
-- ============================================

DROP TABLE IF EXISTS "AiTrackingResult" CASCADE;
DROP TABLE IF EXISTS "AiTrackingPrompt" CASCADE;
DROP TABLE IF EXISTS "ContentArticle" CASCADE;
DROP TABLE IF EXISTS "UserTask" CASCADE;
DROP TABLE IF EXISTS "SiteAudit" CASCADE;
DROP TABLE IF EXISTS "CommunityOpportunities" CASCADE;
DROP TABLE IF EXISTS "Keywords" CASCADE;
DROP TABLE IF EXISTS "Notification" CASCADE;
DROP TABLE IF EXISTS "Project" CASCADE;

-- ============================================
-- STEP 2: Create Project table with INTEGER ID
-- ============================================

CREATE TABLE "Project" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" integer NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Domain" character varying(255),
    "BrandVoice" text,
    "TargetAudience" text,
    "CmsCredentials" text,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp without time zone,
    CONSTRAINT "FK_Project_User_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Project_UserId" ON "Project" ("UserId");
CREATE INDEX "IX_Project_Domain" ON "Project" ("Domain");

-- ============================================
-- STEP 3: Create ContentArticle table
-- ============================================

CREATE TABLE "ContentArticle" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" integer NOT NULL,
    "Title" character varying(255) NOT NULL,
    "Slug" character varying(255),
    "ContentHtml" text,
    "ContentMarkdown" text,
    "MetaTitle" character varying(255),
    "MetaDescription" text,
    "FeaturedImageUrl" text,
    "Status" character varying(50) NOT NULL DEFAULT 'Draft',
    "ScheduledPublishAt" timestamp without time zone,
    "PublishedAt" timestamp without time zone,
    "WordCount" integer NOT NULL DEFAULT 0,
    "AiModelUsed" character varying(100),
    "GenerationPrompt" text,
    "SeoScore" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp without time zone,
    CONSTRAINT "FK_ContentArticle_Project_ProjectId" 
        FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ContentArticle_ProjectId" ON "ContentArticle" ("ProjectId");
CREATE INDEX "IX_ContentArticle_Slug" ON "ContentArticle" ("Slug");
CREATE INDEX "IX_ContentArticle_Status" ON "ContentArticle" ("Status");

-- ============================================
-- STEP 6: Recreate Keywords table (if needed)
-- ============================================

CREATE TABLE IF NOT EXISTS "Keywords" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" integer NOT NULL,
    "KeywordText" character varying(255) NOT NULL,
    "SearchVolume" integer,
    "Difficulty" integer,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Keywords_Project_ProjectId" 
        FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Keywords_ProjectId" ON "Keywords" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_Keywords_KeywordText" ON "Keywords" ("KeywordText");

-- ============================================
-- STEP 4: Create AiTrackingPrompt table
-- ============================================

CREATE TABLE "AiTrackingPrompt" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" integer NOT NULL,
    "PromptText" text NOT NULL,
    "KeywordId" integer,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp without time zone,
    CONSTRAINT "FK_AiTrackingPrompt_Project_ProjectId" 
        FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AiTrackingPrompt_Keywords_KeywordId"
        FOREIGN KEY ("KeywordId") REFERENCES "Keywords" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_AiTrackingPrompt_ProjectId" ON "AiTrackingPrompt" ("ProjectId");
CREATE INDEX "IX_AiTrackingPrompt_KeywordId" ON "AiTrackingPrompt" ("KeywordId");

-- ============================================
-- STEP 5: Create AiTrackingResult table
-- ============================================

CREATE TABLE "AiTrackingResult" (
    "Id" SERIAL PRIMARY KEY,
    "PromptId" integer NOT NULL,
    "AiEngine" character varying(50) NOT NULL,
    "CountryCode" character varying(10),
    "ResponseText" text,
    "BrandMentioned" boolean NOT NULL DEFAULT FALSE,
    "MentionPosition" integer,
    "VisibilityScore" integer NOT NULL DEFAULT 0,
    "Sentiment" character varying(20),
    "SentimentScore" double precision,
    "ExecutedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_AiTrackingResult_AiTrackingPrompt_PromptId" 
        FOREIGN KEY ("PromptId") REFERENCES "AiTrackingPrompt" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AiTrackingResult_PromptId" ON "AiTrackingResult" ("PromptId");
CREATE INDEX "IX_AiTrackingResult_ExecutedAt" ON "AiTrackingResult" ("ExecutedAt");
CREATE INDEX "IX_AiTrackingResult_AiEngine" ON "AiTrackingResult" ("AiEngine");

-- ============================================
-- STEP 7: Create CommunityOpportunities table
-- ============================================

CREATE TABLE IF NOT EXISTS "CommunityOpportunities" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" integer NOT NULL,
    "Platform" character varying(50) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Url" text NOT NULL,
    "Snippet" text,
    "Relevance" integer,
    "EngagementPotential" integer,
    "DiscoveredAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CommunityOpportunities_Project_ProjectId" 
        FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_CommunityOpportunities_Url" UNIQUE ("Url")
);

CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_ProjectId" ON "CommunityOpportunities" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Platform" ON "CommunityOpportunities" ("Platform");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_DiscoveredAt" ON "CommunityOpportunities" ("DiscoveredAt");

-- ============================================
-- STEP 8: Create SiteAudit table
-- ============================================

CREATE TABLE IF NOT EXISTS "SiteAudit" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" integer NOT NULL,
    "AuditDate" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "OverallScore" integer NOT NULL DEFAULT 0,
    "IssuesFound" integer NOT NULL DEFAULT 0,
    "ReportUrl" text,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAudit_Project_ProjectId" 
        FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_SiteAudit_ProjectId" ON "SiteAudit" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_SiteAudit_AuditDate" ON "SiteAudit" ("AuditDate");

-- ============================================
-- STEP 9: Create UserTask table
-- ============================================

CREATE TABLE IF NOT EXISTS "UserTask" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" integer,
    "UserId" integer NOT NULL,
    "Title" character varying(255) NOT NULL,
    "Description" text,
    "Priority" character varying(20) NOT NULL DEFAULT 'Medium',
    "Status" character varying(20) NOT NULL DEFAULT 'Pending',
    "DueDate" timestamp without time zone,
    "CompletedAt" timestamp without time zone,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserTask_Project_ProjectId" 
        FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserTask_User_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_UserTask_ProjectId" ON "UserTask" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_UserTask_UserId" ON "UserTask" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserTask_Status" ON "UserTask" ("Status");

-- ============================================
-- STEP 10: Create Notification table
-- ============================================

CREATE TABLE IF NOT EXISTS "Notification" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" integer NOT NULL,
    "Title" character varying(255) NOT NULL,
    "Message" text NOT NULL,
    "Type" character varying(50) NOT NULL DEFAULT 'Info',
    "IsRead" boolean NOT NULL DEFAULT FALSE,
    "RelatedEntityType" character varying(100),
    "RelatedEntityId" integer,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Notification_User_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Notification_UserId" ON "Notification" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Notification_IsRead" ON "Notification" ("IsRead");
CREATE INDEX IF NOT EXISTS "IX_Notification_CreatedAt" ON "Notification" ("CreatedAt");

-- ============================================
-- COMMIT TRANSACTION
-- ============================================

COMMIT;

-- ============================================
-- VERIFICATION QUERIES
-- ============================================
-- Run these after migration to verify:
-- SELECT table_name, column_name, data_type 
-- FROM information_schema.columns 
-- WHERE table_name IN ('Project', 'ContentArticle', 'AiTrackingPrompt', 'AiTrackingResult')
-- AND column_name = 'Id'
-- ORDER BY table_name;
