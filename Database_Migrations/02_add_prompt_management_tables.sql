-- Migration: Add Prompt Management Tables
-- Date: 2026-02-07
-- Description: Creates tables for prompt tracking, platform monitoring, visibility metrics, and research data

-- ============================================
-- 1. PromptTracking Table
-- ============================================
CREATE TABLE "PromptTracking" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "PromptText" TEXT NOT NULL,
    "Topic" VARCHAR(200),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Active',
    "CreatedByUserId" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "LastCheckedAt" TIMESTAMP,
    "AverageVisibilityScore" DOUBLE PRECISION,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PromptTracking_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PromptTracking_User" FOREIGN KEY ("CreatedByUserId") 
        REFERENCES "User"("Id") ON DELETE RESTRICT
);

-- Indexes for PromptTracking
CREATE INDEX "IX_PromptTracking_ProjectId" ON "PromptTracking"("ProjectId");
CREATE INDEX "IX_PromptTracking_CreatedByUserId" ON "PromptTracking"("CreatedByUserId");
CREATE INDEX "IX_PromptTracking_Status" ON "PromptTracking"("Status");
CREATE INDEX "IX_PromptTracking_IsActive" ON "PromptTracking"("IsActive");
CREATE INDEX "IX_PromptTracking_Topic" ON "PromptTracking"("Topic");

-- ============================================
-- 2. PromptPlatformTracking Table
-- ============================================
CREATE TABLE "PromptPlatformTracking" (
    "Id" SERIAL PRIMARY KEY,
    "PromptId" INTEGER NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "LastCheckDate" TIMESTAMP,
    "CurrentVisibilityScore" DOUBLE PRECISION,
    "BrandMentioned" BOOLEAN,
    "MentionPosition" INTEGER,
    "ResponseSummary" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PromptPlatformTracking_Prompt" FOREIGN KEY ("PromptId") 
        REFERENCES "PromptTracking"("Id") ON DELETE CASCADE
);

-- Indexes for PromptPlatformTracking
CREATE INDEX "IX_PromptPlatformTracking_PromptId" ON "PromptPlatformTracking"("PromptId");
CREATE INDEX "IX_PromptPlatformTracking_Platform" ON "PromptPlatformTracking"("Platform");
CREATE INDEX "IX_PromptPlatformTracking_IsEnabled" ON "PromptPlatformTracking"("IsEnabled");
CREATE UNIQUE INDEX "IX_PromptPlatformTracking_Unique" ON "PromptPlatformTracking"("PromptId", "Platform");

-- ============================================
-- 3. PromptVisibilityMetrics Table
-- ============================================
CREATE TABLE "PromptVisibilityMetrics" (
    "Id" SERIAL PRIMARY KEY,
    "PromptPlatformId" INTEGER NOT NULL,
    "CheckDate" TIMESTAMP NOT NULL,
    "VisibilityScore" DOUBLE PRECISION NOT NULL,
    "Position" INTEGER,
    "BrandMentioned" BOOLEAN NOT NULL,
    "Sentiment" VARCHAR(20) NOT NULL DEFAULT 'Neutral',
    "SentimentScore" DOUBLE PRECISION,
    "ResponseText" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PromptVisibilityMetrics_Platform" FOREIGN KEY ("PromptPlatformId") 
        REFERENCES "PromptPlatformTracking"("Id") ON DELETE CASCADE
);

-- Indexes for PromptVisibilityMetrics
CREATE INDEX "IX_PromptVisibilityMetrics_PromptPlatformId" ON "PromptVisibilityMetrics"("PromptPlatformId");
CREATE INDEX "IX_PromptVisibilityMetrics_CheckDate" ON "PromptVisibilityMetrics"("CheckDate");
CREATE INDEX "IX_PromptVisibilityMetrics_Combined" ON "PromptVisibilityMetrics"("PromptPlatformId", "CheckDate");

-- ============================================
-- 4. PromptResearchData Table
-- ============================================
CREATE TABLE "PromptResearchData" (
    "Id" SERIAL PRIMARY KEY,
    "PromptText" TEXT NOT NULL,
    "Topic" VARCHAR(200),
    "EstimatedVolume" INTEGER,
    "Difficulty" VARCHAR(20) NOT NULL DEFAULT 'Medium',
    "RelevanceScore" DOUBLE PRECISION,
    "Source" VARCHAR(50) NOT NULL DEFAULT 'Manual',
    "DiscoveredAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for PromptResearchData
CREATE INDEX "IX_PromptResearchData_Topic" ON "PromptResearchData"("Topic");
CREATE INDEX "IX_PromptResearchData_Source" ON "PromptResearchData"("Source");
CREATE INDEX "IX_PromptResearchData_DiscoveredAt" ON "PromptResearchData"("DiscoveredAt");

-- ============================================
-- Migration Complete
-- ============================================
-- Tables Created: 4
-- Indexes Created: 14
-- Foreign Keys: 3
