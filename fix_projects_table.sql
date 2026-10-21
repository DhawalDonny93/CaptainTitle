-- Fix Project Table Schema
-- This script ensures the Projects table has the correct schema with uuid Id

BEGIN;

-- Drop the existing Projects table if it exists with wrong schema
DROP TABLE IF EXISTS "Projects" CASCADE;

-- Create Projects table with correct schema (uuid Id)
CREATE TABLE "Projects" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" integer NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Domain" character varying(255),
    "BrandVoice" text,
    "TargetAudience" text,
    "CmsCredentials" text,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp without time zone,
    CONSTRAINT "PK_Projects" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Projects_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Projects_UserId" ON "Projects" ("UserId");
CREATE INDEX "IX_Projects_Domain" ON "Projects" ("Domain");

-- Recreate dependent tables that reference Projects

-- Keywords table
CREATE TABLE IF NOT EXISTS "Keywords" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" uuid NOT NULL,
    "KeywordText" character varying(255) NOT NULL,
    "SearchVolume" integer,
    "Difficulty" integer,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Keywords_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Keywords_ProjectId" ON "Keywords" ("ProjectId");

-- CommunityOpportunities table
CREATE TABLE IF NOT EXISTS "CommunityOpportunities" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" uuid NOT NULL,
    "Platform" character varying(50) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Url" text NOT NULL,
    "Snippet" text,
    "Relevance" integer,
    "EngagementPotential" integer,
    "DiscoveredAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CommunityOpportunities_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_CommunityOpportunities_Url" UNIQUE ("Url")
);

CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_ProjectId" ON "CommunityOpportunities" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_CommunityOpportunities_Platform" ON "CommunityOpportunities" ("Platform");

-- SiteAudits table
CREATE TABLE IF NOT EXISTS "SiteAudits" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" uuid NOT NULL,
    "AuditDate" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "OverallScore" integer NOT NULL DEFAULT 0,
    "IssuesFound" integer NOT NULL DEFAULT 0,
    "ReportUrl" text,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAudits_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_SiteAudits_ProjectId" ON "SiteAudits" ("ProjectId");

-- UserTasks table
CREATE TABLE IF NOT EXISTS "UserTasks" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" uuid,
    "UserId" integer NOT NULL,
    "Title" character varying(255) NOT NULL,
    "Description" text,
    "Priority" character varying(20) NOT NULL DEFAULT 'Medium',
    "Status" character varying(20) NOT NULL DEFAULT 'Pending',
    "DueDate" timestamp without time zone,
    "CompletedAt" timestamp without time zone,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserTasks_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserTasks_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_UserTasks_ProjectId" ON "UserTasks" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_UserTasks_UserId" ON "UserTasks" ("UserId");

-- ContentArticles table (if not exists)
CREATE TABLE IF NOT EXISTS "ContentArticle" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ProjectId" uuid NOT NULL,
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
    "UpdatedAt" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_ContentArticle" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ContentArticle_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_ContentArticle_ProjectId" ON "ContentArticle" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_ContentArticle_Slug" ON "ContentArticle" ("Slug");
CREATE INDEX IF NOT EXISTS "IX_ContentArticle_Status" ON "ContentArticle" ("Status");

-- AiTrackingPrompt table (if not exists)
CREATE TABLE IF NOT EXISTS "AiTrackingPrompt" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ProjectId" uuid NOT NULL,
    "PromptText" text NOT NULL,
    "Category" character varying(100),
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_AiTrackingPrompt" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AiTrackingPrompt_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AiTrackingPrompt_ProjectId" ON "AiTrackingPrompt" ("ProjectId");

COMMIT;
