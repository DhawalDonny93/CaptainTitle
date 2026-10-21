-- Migration: Add Opportunities Auditor System Tables
-- Date: 2026-02-08
-- Description: Creates tables for opportunity discovery, content gap analysis, competitor analysis, and impact tracking

-- ============================================
-- 1. Create or Update CommunityOpportunity Table
-- ============================================

-- First, create the table if it doesn't exist
CREATE TABLE IF NOT EXISTS "CommunityOpportunity" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "KeywordId" INTEGER,
    "Platform" VARCHAR(50) NOT NULL,
    "Title" VARCHAR(300) NOT NULL,
    "Url" TEXT NOT NULL,
    "Summary" TEXT,
    "Sentiment" VARCHAR(50),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'New',
    "DraftReply" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CommunityOpportunity_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Create base indexes if they don't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_ProjectId') THEN
        CREATE INDEX "IX_CommunityOpportunity_ProjectId" ON "CommunityOpportunity"("ProjectId");
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_Platform') THEN
        CREATE INDEX "IX_CommunityOpportunity_Platform" ON "CommunityOpportunity"("Platform");
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_Status') THEN
        CREATE INDEX "IX_CommunityOpportunity_Status" ON "CommunityOpportunity"("Status");
    END IF;
END $$;

-- Now add Phase 5 columns if they don't exist
DO $$ 
BEGIN
    -- Add new columns to existing CommunityOpportunity table if they don't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunity' AND column_name = 'Priority') THEN
        ALTER TABLE "CommunityOpportunity" ADD COLUMN "Priority" INTEGER DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunity' AND column_name = 'EngagementScore') THEN
        ALTER TABLE "CommunityOpportunity" ADD COLUMN "EngagementScore" INTEGER DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunity' AND column_name = 'Upvotes') THEN
        ALTER TABLE "CommunityOpportunity" ADD COLUMN "Upvotes" INTEGER DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunity' AND column_name = 'Comments') THEN
        ALTER TABLE "CommunityOpportunity" ADD COLUMN "Comments" INTEGER DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunity' AND column_name = 'Views') THEN
        ALTER TABLE "CommunityOpportunity" ADD COLUMN "Views" INTEGER DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunity' AND column_name = 'PostedAt') THEN
        ALTER TABLE "CommunityOpportunity" ADD COLUMN "PostedAt" TIMESTAMP;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunity' AND column_name = 'LastCheckedAt') THEN
        ALTER TABLE "CommunityOpportunity" ADD COLUMN "LastCheckedAt" TIMESTAMP;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunity' AND column_name = 'IsArchived') THEN
        ALTER TABLE "CommunityOpportunity" ADD COLUMN "IsArchived" BOOLEAN DEFAULT FALSE;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'CommunityOpportunity' AND column_name = 'AssignedToUserId') THEN
        ALTER TABLE "CommunityOpportunity" ADD COLUMN "AssignedToUserId" INTEGER;
    END IF;
END $$;

-- Add indexes to CommunityOpportunity if they don't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_Priority') THEN
        CREATE INDEX "IX_CommunityOpportunity_Priority" ON "CommunityOpportunity"("Priority");
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_EngagementScore') THEN
        CREATE INDEX "IX_CommunityOpportunity_EngagementScore" ON "CommunityOpportunity"("EngagementScore");
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CommunityOpportunity_IsArchived') THEN
        CREATE INDEX "IX_CommunityOpportunity_IsArchived" ON "CommunityOpportunity"("IsArchived");
    END IF;
END $$;

-- ============================================
-- 2. OpportunityScan Table
-- ============================================
CREATE TABLE "OpportunityScan" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "ScanType" VARCHAR(50) NOT NULL,
    "TargetUrl" TEXT,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "TotalOpportunities" INTEGER DEFAULT 0,
    "HighPriority" INTEGER DEFAULT 0,
    "MediumPriority" INTEGER DEFAULT 0,
    "LowPriority" INTEGER DEFAULT 0,
    "StartedAt" TIMESTAMP,
    "CompletedAt" TIMESTAMP,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_OpportunityScan_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for OpportunityScan
CREATE INDEX "IX_OpportunityScan_ProjectId" ON "OpportunityScan"("ProjectId");
CREATE INDEX "IX_OpportunityScan_Status" ON "OpportunityScan"("Status");
CREATE INDEX "IX_OpportunityScan_ScanType" ON "OpportunityScan"("ScanType");
CREATE INDEX "IX_OpportunityScan_CreatedAt" ON "OpportunityScan"("CreatedAt");

-- ============================================
-- 3. ContentGapOpportunity Table
-- ============================================
CREATE TABLE "ContentGapOpportunity" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "OpportunityScanId" INTEGER,
    "Topic" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "RecommendedContentType" VARCHAR(50),
    "EstimatedImpact" VARCHAR(20),
    "CompetitorCoverage" INTEGER DEFAULT 0,
    "SearchVolume" INTEGER DEFAULT 0,
    "Difficulty" INTEGER DEFAULT 0,
    "Status" VARCHAR(20) DEFAULT 'New',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_ContentGapOpportunity_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ContentGapOpportunity_Scan" FOREIGN KEY ("OpportunityScanId") 
        REFERENCES "OpportunityScan"("Id") ON DELETE SET NULL
);

-- Indexes for ContentGapOpportunity
CREATE INDEX "IX_ContentGapOpportunity_ProjectId" ON "ContentGapOpportunity"("ProjectId");
CREATE INDEX "IX_ContentGapOpportunity_Status" ON "ContentGapOpportunity"("Status");
CREATE INDEX "IX_ContentGapOpportunity_EstimatedImpact" ON "ContentGapOpportunity"("EstimatedImpact");

-- ============================================
-- 4. CompetitorAnalysis Table
-- ============================================
CREATE TABLE "CompetitorAnalysis" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "CompetitorDomain" VARCHAR(255) NOT NULL,
    "CompetitorName" VARCHAR(255),
    "VisibilityScore" INTEGER DEFAULT 0,
    "CitationShare" DECIMAL(5,2) DEFAULT 0,
    "ContentCount" INTEGER DEFAULT 0,
    "TopTopics" TEXT,
    "Strengths" TEXT,
    "Weaknesses" TEXT,
    "LastAnalyzedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CompetitorAnalysis_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for CompetitorAnalysis
CREATE INDEX "IX_CompetitorAnalysis_ProjectId" ON "CompetitorAnalysis"("ProjectId");
CREATE INDEX "IX_CompetitorAnalysis_Domain" ON "CompetitorAnalysis"("CompetitorDomain");
CREATE UNIQUE INDEX "IX_CompetitorAnalysis_Unique" ON "CompetitorAnalysis"("ProjectId", "CompetitorDomain");

-- ============================================
-- 5. OpportunityImpact Table
-- ============================================
CREATE TABLE "OpportunityImpact" (
    "Id" SERIAL PRIMARY KEY,
    "OpportunityType" VARCHAR(50) NOT NULL,
    "OpportunityId" INTEGER NOT NULL,
    "ImpactType" VARCHAR(50) NOT NULL,
    "EstimatedValue" DECIMAL(10,2),
    "ActualValue" DECIMAL(10,2),
    "MeasuredAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for OpportunityImpact
CREATE INDEX "IX_OpportunityImpact_OpportunityType" ON "OpportunityImpact"("OpportunityType");
CREATE INDEX "IX_OpportunityImpact_OpportunityId" ON "OpportunityImpact"("OpportunityId");

-- ============================================
-- Migration Complete
-- ============================================
-- Tables Created: 4 new tables
-- Columns Added: 9 to existing CommunityOpportunity table
-- Indexes Created: 16 new indexes
-- Foreign Keys: 4
