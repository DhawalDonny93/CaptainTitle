-- Migration: Enhance Citation and Mention Tracking
-- Date: 2026-02-23
-- Description: Add fields for citation count, content type, last seen date, and ensure mention count exists

-- Add new columns to Citations table
ALTER TABLE "Citations" 
ADD COLUMN IF NOT EXISTS "ContentType" TEXT NOT NULL DEFAULT 'Other',
ADD COLUMN IF NOT EXISTS "CitationCount" INTEGER NOT NULL DEFAULT 1,
ADD COLUMN IF NOT EXISTS "LastSeenAt" TIMESTAMP NULL;

-- Add MentionCount to BrandMentions if it doesn't exist (should already exist from previous migration)
ALTER TABLE "BrandMentions" 
ADD COLUMN IF NOT EXISTS "MentionCount" INTEGER NOT NULL DEFAULT 0;

-- Create index for better query performance on citation frequency
CREATE INDEX IF NOT EXISTS "IX_Citations_ProjectId_Platform_SourceDomain" 
ON "Citations" ("ProjectId", "Platform", "SourceDomain");

-- Create index for better query performance on brand mentions
CREATE INDEX IF NOT EXISTS "IX_BrandMentions_ProjectId_Platform_CheckDate" 
ON "BrandMentions" ("ProjectId", "Platform", "CheckDate");

-- Create index for competitor mentions
CREATE INDEX IF NOT EXISTS "IX_BrandMentions_ProjectId_IsCompetitor_CheckDate" 
ON "BrandMentions" ("ProjectId", "IsCompetitor", "CheckDate");

-- Update existing records to set LastSeenAt to DiscoveredAt
UPDATE "Citations" 
SET "LastSeenAt" = "DiscoveredAt" 
WHERE "LastSeenAt" IS NULL;

COMMENT ON COLUMN "Citations"."ContentType" IS 'Content type classification: Editorial, Review, Institutional, Competitor, Social, Technical, Other';
COMMENT ON COLUMN "Citations"."CitationCount" IS 'Number of times this source has been cited across all prompts';
COMMENT ON COLUMN "Citations"."LastSeenAt" IS 'Last time this citation appeared in a response';
COMMENT ON COLUMN "BrandMentions"."MentionCount" IS 'Number of times the brand was mentioned in the response';
