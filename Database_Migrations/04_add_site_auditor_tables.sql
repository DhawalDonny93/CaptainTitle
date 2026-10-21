-- Migration: Add Site Auditor System Tables
-- Date: 2026-02-08
-- Description: Creates tables for comprehensive site auditing including LLMs.txt, Schema, DOM clarity, content structure, and technical SEO checks

-- ============================================
-- 1. SiteAuditRun Table
-- ============================================
CREATE TABLE "SiteAuditRun" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Url" TEXT NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "TotalChecks" INTEGER DEFAULT 0,
    "PassedChecks" INTEGER DEFAULT 0,
    "FailedChecks" INTEGER DEFAULT 0,
    "WarningChecks" INTEGER DEFAULT 0,
    "OverallScore" INTEGER DEFAULT 0,
    "StartedAt" TIMESTAMP,
    "CompletedAt" TIMESTAMP,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditRun_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for SiteAuditRun
CREATE INDEX "IX_SiteAuditRun_ProjectId" ON "SiteAuditRun"("ProjectId");
CREATE INDEX "IX_SiteAuditRun_Status" ON "SiteAuditRun"("Status");
CREATE INDEX "IX_SiteAuditRun_CreatedAt" ON "SiteAuditRun"("CreatedAt");

-- ============================================
-- 2. SiteAuditDetail Table
-- ============================================
CREATE TABLE "SiteAuditDetail" (
    "Id" SERIAL PRIMARY KEY,
    "SiteAuditRunId" INTEGER NOT NULL,
    "CheckName" VARCHAR(100) NOT NULL,
    "CheckType" VARCHAR(50) NOT NULL,
    "Status" VARCHAR(20) NOT NULL,
    "Score" INTEGER DEFAULT 0,
    "Message" TEXT,
    "Details" JSONB,
    "Recommendation" TEXT,
    "AutoFixAvailable" BOOLEAN DEFAULT FALSE,
    "Priority" INTEGER DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditDetail_Run" FOREIGN KEY ("SiteAuditRunId") 
        REFERENCES "SiteAuditRun"("Id") ON DELETE CASCADE
);

-- Indexes for SiteAuditDetail
CREATE INDEX "IX_SiteAuditDetail_RunId" ON "SiteAuditDetail"("SiteAuditRunId");
CREATE INDEX "IX_SiteAuditDetail_CheckType" ON "SiteAuditDetail"("CheckType");
CREATE INDEX "IX_SiteAuditDetail_Status" ON "SiteAuditDetail"("Status");

-- ============================================
-- 3. SiteAuditHistory Table
-- ============================================
CREATE TABLE "SiteAuditHistory" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "CheckDate" TIMESTAMP NOT NULL,
    "OverallScore" INTEGER,
    "LlmsTxtScore" INTEGER,
    "SchemaScore" INTEGER,
    "DomClarityScore" INTEGER,
    "ContentScore" INTEGER,
    "TechnicalScore" INTEGER,
    "TotalIssues" INTEGER DEFAULT 0,
    "CriticalIssues" INTEGER DEFAULT 0,
    "WarningIssues" INTEGER DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditHistory_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE
);

-- Indexes for SiteAuditHistory
CREATE INDEX "IX_SiteAuditHistory_ProjectId" ON "SiteAuditHistory"("ProjectId");
CREATE INDEX "IX_SiteAuditHistory_CheckDate" ON "SiteAuditHistory"("CheckDate");

-- ============================================
-- 4. Update Existing SiteAudit Table (if needed)
-- ============================================
-- Add new columns to existing SiteAudit table if they don't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'SiteAudit' AND column_name = 'Url') THEN
        ALTER TABLE "SiteAudit" ADD COLUMN "Url" TEXT;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'SiteAudit' AND column_name = 'AuditCategory') THEN
        ALTER TABLE "SiteAudit" ADD COLUMN "AuditCategory" VARCHAR(50);
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'SiteAudit' AND column_name = 'Severity') THEN
        ALTER TABLE "SiteAudit" ADD COLUMN "Severity" VARCHAR(20);
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'SiteAudit' AND column_name = 'Recommendation') THEN
        ALTER TABLE "SiteAudit" ADD COLUMN "Recommendation" TEXT;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'SiteAudit' AND column_name = 'AutoFixAvailable') THEN
        ALTER TABLE "SiteAudit" ADD COLUMN "AutoFixAvailable" BOOLEAN DEFAULT FALSE;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'SiteAudit' AND column_name = 'FixedAt') THEN
        ALTER TABLE "SiteAudit" ADD COLUMN "FixedAt" TIMESTAMP;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'SiteAudit' AND column_name = 'RecheckAt') THEN
        ALTER TABLE "SiteAudit" ADD COLUMN "RecheckAt" TIMESTAMP;
    END IF;
END $$;

-- Add indexes to SiteAudit if they don't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SiteAudit_ProjectId') THEN
        CREATE INDEX "IX_SiteAudit_ProjectId" ON "SiteAudit"("ProjectId");
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SiteAudit_AuditType') THEN
        CREATE INDEX "IX_SiteAudit_AuditType" ON "SiteAudit"("AuditType");
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SiteAudit_Severity') THEN
        CREATE INDEX "IX_SiteAudit_Severity" ON "SiteAudit"("Severity");
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SiteAudit_CheckDate') THEN
        CREATE INDEX "IX_SiteAudit_CheckDate" ON "SiteAudit"("CheckDate");
    END IF;
END $$;

-- ============================================
-- Migration Complete
-- ============================================
-- Tables Created: 3 new tables
-- Columns Added: 7 to existing SiteAudit table
-- Indexes Created: 10 new indexes
-- Foreign Keys: 3
