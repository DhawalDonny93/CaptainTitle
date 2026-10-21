-- Migration: AddAiSeoEntities
-- Date: 2026-01-05
-- Description: Adds Project, ContentArticle, AiTrackingPrompt, and AiTrackingResult tables.

BEGIN;

-- Create Project Table
CREATE TABLE "Project" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Domain" character varying(255),
    "BrandVoice" text,
    "TargetAudience" text,
    "CmsCredentials" text,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp without time zone,
    CONSTRAINT "PK_Project" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Project_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Project_UserId" ON "Project" ("UserId");
CREATE INDEX "IX_Project_Domain" ON "Project" ("Domain");

-- Create ContentArticle Table
CREATE TABLE "ContentArticle" (
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
    CONSTRAINT "FK_ContentArticle_Project_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ContentArticle_ProjectId" ON "ContentArticle" ("ProjectId");
CREATE INDEX "IX_ContentArticle_Slug" ON "ContentArticle" ("Slug");
CREATE INDEX "IX_ContentArticle_Status" ON "ContentArticle" ("Status");

-- Create AiTrackingPrompt Table
CREATE TABLE "AiTrackingPrompt" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ProjectId" uuid NOT NULL,
    "PromptText" text NOT NULL,
    "Category" character varying(100),
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_AiTrackingPrompt" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AiTrackingPrompt_Project_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AiTrackingPrompt_ProjectId" ON "AiTrackingPrompt" ("ProjectId");

-- Create AiTrackingResult Table
CREATE TABLE "AiTrackingResult" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "PromptId" uuid NOT NULL,
    "AiEngine" character varying(50) NOT NULL,
    "CountryCode" character varying(10),
    "ResponseText" text,
    "BrandMentioned" boolean NOT NULL DEFAULT FALSE,
    "MentionPosition" integer,
    "SentimentScore" double precision,
    "ExecutedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_AiTrackingResult" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AiTrackingResult_AiTrackingPrompt_PromptId" FOREIGN KEY ("PromptId") REFERENCES "AiTrackingPrompt" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AiTrackingResult_PromptId" ON "AiTrackingResult" ("PromptId");
CREATE INDEX "IX_AiTrackingResult_ExecutedAt" ON "AiTrackingResult" ("ExecutedAt");

COMMIT;
