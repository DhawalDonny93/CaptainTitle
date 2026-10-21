-- Migration: Add Visibility Dashboard System Tables
-- Date: 2026-02-08
-- Description: Creates tables for dashboard metrics, goals, alerts, and competitive intelligence

-- ============================================
-- 1. VisibilityDashboardMetrics Table
-- ============================================
CREATE TABLE "VisibilityDashboardMetrics" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "MetricDate" DATE NOT NULL,
    "Period" VARCHAR(20) NOT NULL DEFAULT 'daily',
    
    -- Overall Visibility
    "OverallVisibilityScore" INTEGER DEFAULT 0,
    "VisibilityRank" INTEGER,
    "VisibilityChange" DECIMAL(5,2),
    
    -- Prompt Performance
    "TotalPrompts" INTEGER DEFAULT 0,
    "ActivePrompts" INTEGER DEFAULT 0,
    "AveragePromptScore" DECIMAL(5,2) DEFAULT 0,
    "PromptsWithMentions" INTEGER DEFAULT 0,
    "MentionRate" DECIMAL(5,2) DEFAULT 0,
    
    -- Citation Metrics
    "TotalCitations" INTEGER DEFAULT 0,
    "CitationShare" DECIMAL(5,2) DEFAULT 0,
    "CitationRank" INTEGER,
    "UniqueDomainsCiting" INTEGER DEFAULT 0,
    "OwnedDomainCitations" INTEGER DEFAULT 0,
    
    -- Site Health
    "SiteAuditScore" INTEGER DEFAULT 0,
    "CriticalIssues" INTEGER DEFAULT 0,
    "WarningIssues" INTEGER DEFAULT 0,
    "PassedChecks" INTEGER DEFAULT 0,
    
    -- Opportunities
    "TotalOpportunities" INTEGER DEFAULT 0,
    "HighPriorityOpportunities" INTEGER DEFAULT 0,
    "OpportunitiesActioned" INTEGER DEFAULT 0,
    
    -- Platform Breakdown (JSON)
    "PlatformMetrics" JSONB,
    
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_VisibilityDashboardMetrics_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for VisibilityDashboardMetrics
CREATE INDEX "IX_VisibilityDashboardMetrics_ProjectId" ON "VisibilityDashboardMetrics"("ProjectId");
CREATE INDEX "IX_VisibilityDashboardMetrics_MetricDate" ON "VisibilityDashboardMetrics"("MetricDate");
CREATE INDEX "IX_VisibilityDashboardMetrics_Period" ON "VisibilityDashboardMetrics"("Period");
CREATE UNIQUE INDEX "IX_VisibilityDashboardMetrics_Unique" ON "VisibilityDashboardMetrics"("ProjectId", "MetricDate", "Period");

-- ============================================
-- 2. VisibilityGoal Table
-- ============================================
CREATE TABLE "VisibilityGoal" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "GoalType" VARCHAR(50) NOT NULL,
    "TargetValue" DECIMAL(10,2) NOT NULL,
    "CurrentValue" DECIMAL(10,2) DEFAULT 0,
    "StartDate" DATE NOT NULL,
    "TargetDate" DATE NOT NULL,
    "Status" VARCHAR(20) DEFAULT 'Active',
    "Progress" DECIMAL(5,2) DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_VisibilityGoal_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for VisibilityGoal
CREATE INDEX "IX_VisibilityGoal_ProjectId" ON "VisibilityGoal"("ProjectId");
CREATE INDEX "IX_VisibilityGoal_Status" ON "VisibilityGoal"("Status");
CREATE INDEX "IX_VisibilityGoal_TargetDate" ON "VisibilityGoal"("TargetDate");

-- ============================================
-- 3. VisibilityAlert Table
-- ============================================
CREATE TABLE "VisibilityAlert" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "AlertType" VARCHAR(50) NOT NULL,
    "Severity" VARCHAR(20) NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Message" TEXT NOT NULL,
    "MetricType" VARCHAR(50),
    "OldValue" DECIMAL(10,2),
    "NewValue" DECIMAL(10,2),
    "ChangePercentage" DECIMAL(5,2),
    "IsRead" BOOLEAN DEFAULT FALSE,
    "IsDismissed" BOOLEAN DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_VisibilityAlert_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for VisibilityAlert
CREATE INDEX "IX_VisibilityAlert_ProjectId" ON "VisibilityAlert"("ProjectId");
CREATE INDEX "IX_VisibilityAlert_IsRead" ON "VisibilityAlert"("IsRead");
CREATE INDEX "IX_VisibilityAlert_Severity" ON "VisibilityAlert"("Severity");
CREATE INDEX "IX_VisibilityAlert_CreatedAt" ON "VisibilityAlert"("CreatedAt");

-- ============================================
-- 4. CompetitiveIntelligence Table
-- ============================================
CREATE TABLE "CompetitiveIntelligence" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "CompetitorDomain" VARCHAR(255) NOT NULL,
    "MetricDate" DATE NOT NULL,
    "VisibilityScore" INTEGER DEFAULT 0,
    "CitationShare" DECIMAL(5,2) DEFAULT 0,
    "Rank" INTEGER,
    "PromptsTracked" INTEGER DEFAULT 0,
    "AverageMentionPosition" DECIMAL(4,2),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CompetitiveIntelligence_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for CompetitiveIntelligence
CREATE INDEX "IX_CompetitiveIntelligence_ProjectId" ON "CompetitiveIntelligence"("ProjectId");
CREATE INDEX "IX_CompetitiveIntelligence_MetricDate" ON "CompetitiveIntelligence"("MetricDate");
CREATE INDEX "IX_CompetitiveIntelligence_Domain" ON "CompetitiveIntelligence"("CompetitorDomain");

-- ============================================
-- Migration Complete
-- ============================================
-- Tables Created: 4 new tables
-- Indexes Created: 15 new indexes
-- Foreign Keys: 4
