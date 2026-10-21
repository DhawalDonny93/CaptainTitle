-- Migration: Add Citations & Mentions Tracking Tables
-- Date: 2026-02-07
-- Description: Creates tables for citation tracking, brand mentions, and aggregation metrics

-- ============================================
-- 1. Citation Table
-- ============================================
CREATE TABLE "Citation" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "SourceUrl" TEXT NOT NULL,
    "SourceDomain" VARCHAR(255) NOT NULL,
    "SourceTitle" VARCHAR(500) NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "PromptText" TEXT,
    "Position" INTEGER,
    "CitationType" VARCHAR(50) NOT NULL DEFAULT 'DirectLink',
    "Context" TEXT,
    "DiscoveredAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Citation_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for Citation
CREATE INDEX "IX_Citation_ProjectId" ON "Citation"("ProjectId");
CREATE INDEX "IX_Citation_SourceDomain" ON "Citation"("SourceDomain");
CREATE INDEX "IX_Citation_Platform" ON "Citation"("Platform");
CREATE INDEX "IX_Citation_DiscoveredAt" ON "Citation"("DiscoveredAt");
CREATE INDEX "IX_Citation_Combined" ON "Citation"("ProjectId", "SourceUrl", "Platform");

-- ============================================
-- 2. CitationMetric Table
-- ============================================
CREATE TABLE "CitationMetric" (
    "Id" SERIAL PRIMARY KEY,
    "CitationId" INTEGER NOT NULL,
    "CheckDate" TIMESTAMP NOT NULL,
    "Position" INTEGER NOT NULL,
    "IsVisible" BOOLEAN NOT NULL DEFAULT TRUE,
    "ResponseContext" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CitationMetric_Citation" FOREIGN KEY ("CitationId") 
        REFERENCES "Citation"("Id") ON DELETE CASCADE
);

-- Indexes for CitationMetric
CREATE INDEX "IX_CitationMetric_CitationId" ON "CitationMetric"("CitationId");
CREATE INDEX "IX_CitationMetric_CheckDate" ON "CitationMetric"("CheckDate");
CREATE INDEX "IX_CitationMetric_Combined" ON "CitationMetric"("CitationId", "CheckDate");

-- ============================================
-- 3. BrandMention Table
-- ============================================
CREATE TABLE "BrandMention" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "PromptText" TEXT NOT NULL,
    "IsMentioned" BOOLEAN NOT NULL,
    "Position" INTEGER,
    "MentionType" VARCHAR(50) NOT NULL DEFAULT 'Direct',
    "MentionContext" TEXT,
    "Sentiment" VARCHAR(20) NOT NULL DEFAULT 'Neutral',
    "SentimentScore" DOUBLE PRECISION,
    "ResponseText" TEXT,
    "CheckDate" TIMESTAMP NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_BrandMention_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for BrandMention
CREATE INDEX "IX_BrandMention_ProjectId" ON "BrandMention"("ProjectId");
CREATE INDEX "IX_BrandMention_Platform" ON "BrandMention"("Platform");
CREATE INDEX "IX_BrandMention_CheckDate" ON "BrandMention"("CheckDate");
CREATE INDEX "IX_BrandMention_IsMentioned" ON "BrandMention"("IsMentioned");
CREATE INDEX "IX_BrandMention_Combined" ON "BrandMention"("ProjectId", "Platform", "CheckDate");

-- ============================================
-- 4. CitationAggregation Table
-- ============================================
CREATE TABLE "CitationAggregation" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "SourceDomain" VARCHAR(255) NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "AggregationDate" DATE NOT NULL,
    "TotalCitations" INTEGER NOT NULL DEFAULT 0,
    "UniqueCitations" INTEGER NOT NULL DEFAULT 0,
    "AveragePosition" DOUBLE PRECISION NOT NULL DEFAULT 0,
    "TopPosition" INTEGER NOT NULL DEFAULT 999,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CitationAggregation_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for CitationAggregation
CREATE INDEX "IX_CitationAggregation_ProjectId" ON "CitationAggregation"("ProjectId");
CREATE INDEX "IX_CitationAggregation_SourceDomain" ON "CitationAggregation"("SourceDomain");
CREATE INDEX "IX_CitationAggregation_Platform" ON "CitationAggregation"("Platform");
CREATE INDEX "IX_CitationAggregation_AggregationDate" ON "CitationAggregation"("AggregationDate");
CREATE UNIQUE INDEX "IX_CitationAggregation_Unique" ON "CitationAggregation"("ProjectId", "SourceDomain", "Platform", "AggregationDate");

-- ============================================
-- Migration Complete
-- ============================================
-- Tables Created: 4
-- Indexes Created: 16
-- Foreign Keys: 4
