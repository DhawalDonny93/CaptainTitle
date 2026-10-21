-- =====================================================
-- Site Audit Issues and Opportunities Enhancement
-- =====================================================
-- This migration adds tables to store audit issues and opportunities
-- with completion status tracking

-- 1. Create SiteAuditIssues table to store individual audit findings
CREATE TABLE IF NOT EXISTS "SiteAuditIssues" (
    "Id" SERIAL PRIMARY KEY,
    "SiteAuditRunId" INTEGER NOT NULL,
    "Title" VARCHAR(500) NOT NULL,
    "Description" TEXT NOT NULL,
    "Severity" VARCHAR(50) NOT NULL, -- Critical, High, Medium, Low
    "Category" VARCHAR(100) NOT NULL, -- Content, AEO, Technical
    "Type" VARCHAR(100) NOT NULL, -- Specific issue type (e.g., Schema, Meta, Links)
    "AffectedPages" INTEGER DEFAULT 0,
    "IsCompleted" BOOLEAN DEFAULT FALSE,
    "CompletedAt" TIMESTAMP NULL,
    "CompletedBy" INTEGER NULL, -- UserId who marked it complete
    "Recommendation" TEXT NULL,
    "AutoFixAvailable" BOOLEAN DEFAULT FALSE,
    "Priority" INTEGER DEFAULT 3, -- 1-5, higher is more important
    "EstimatedImpactScore" INTEGER DEFAULT 0, -- 0-100
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_SiteAuditIssues_SiteAuditRun" 
        FOREIGN KEY ("SiteAuditRunId") 
        REFERENCES "SiteAuditRuns"("Id") 
        ON DELETE CASCADE,
    
    CONSTRAINT "FK_SiteAuditIssues_CompletedBy" 
        FOREIGN KEY ("CompletedBy") 
        REFERENCES "Users"("Id") 
        ON DELETE SET NULL
);

-- 2. Create indexes for better query performance
CREATE INDEX IF NOT EXISTS "IX_SiteAuditIssues_SiteAuditRunId" 
    ON "SiteAuditIssues"("SiteAuditRunId");

CREATE INDEX IF NOT EXISTS "IX_SiteAuditIssues_Severity" 
    ON "SiteAuditIssues"("Severity");

CREATE INDEX IF NOT EXISTS "IX_SiteAuditIssues_IsCompleted" 
    ON "SiteAuditIssues"("IsCompleted");

CREATE INDEX IF NOT EXISTS "IX_SiteAuditIssues_Category" 
    ON "SiteAuditIssues"("Category");

-- 3. Add columns to SiteAuditRuns for better tracking
ALTER TABLE "SiteAuditRuns" 
    ADD COLUMN IF NOT EXISTS "PerformanceScore" INTEGER DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "SeoScore" INTEGER DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "AeoScore" INTEGER DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ContentScore" INTEGER DEFAULT 0;

-- 4. Create OpportunityTasks table to link opportunities to tasks
CREATE TABLE IF NOT EXISTS "OpportunityTasks" (
    "Id" SERIAL PRIMARY KEY,
    "OpportunityId" INTEGER NOT NULL,
    "OpportunitySource" VARCHAR(50) NOT NULL, -- Visibility, ContentGap, SEO
    "TaskId" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_OpportunityTasks_Task" 
        FOREIGN KEY ("TaskId") 
        REFERENCES "Tasks"("Id") 
        ON DELETE CASCADE,
    
    CONSTRAINT "UQ_OpportunityTasks_OpportunityTask" 
        UNIQUE ("OpportunityId", "OpportunitySource", "TaskId")
);

-- 5. Create indexes for OpportunityTasks
CREATE INDEX IF NOT EXISTS "IX_OpportunityTasks_OpportunityId" 
    ON "OpportunityTasks"("OpportunityId", "OpportunitySource");

CREATE INDEX IF NOT EXISTS "IX_OpportunityTasks_TaskId" 
    ON "OpportunityTasks"("TaskId");

-- 6. Add completion tracking to existing opportunity tables
ALTER TABLE "VisibilityOpportunities" 
    ADD COLUMN IF NOT EXISTS "IsCompleted" BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS "CompletedAt" TIMESTAMP NULL;

ALTER TABLE "ContentGapOpportunities" 
    ADD COLUMN IF NOT EXISTS "IsCompleted" BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS "CompletedAt" TIMESTAMP NULL;

ALTER TABLE "SeoOpportunities" 
    ADD COLUMN IF NOT EXISTS "IsCompleted" BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS "CompletedAt" TIMESTAMP NULL;

-- 7. Create function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 8. Create trigger for SiteAuditIssues
DROP TRIGGER IF EXISTS update_site_audit_issues_updated_at ON "SiteAuditIssues";
CREATE TRIGGER update_site_audit_issues_updated_at
    BEFORE UPDATE ON "SiteAuditIssues"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 9. Create view for audit issues summary
CREATE OR REPLACE VIEW "VW_AuditIssuesSummary" AS
SELECT 
    sar."ProjectId",
    sar."Id" AS "SiteAuditRunId",
    COUNT(sai."Id") AS "TotalIssues",
    COUNT(CASE WHEN sai."Severity" = 'Critical' THEN 1 END) AS "CriticalIssues",
    COUNT(CASE WHEN sai."Severity" = 'High' THEN 1 END) AS "HighIssues",
    COUNT(CASE WHEN sai."Severity" = 'Medium' THEN 1 END) AS "MediumIssues",
    COUNT(CASE WHEN sai."Severity" = 'Low' THEN 1 END) AS "LowIssues",
    COUNT(CASE WHEN sai."IsCompleted" = TRUE THEN 1 END) AS "CompletedIssues",
    COUNT(CASE WHEN sai."IsCompleted" = FALSE THEN 1 END) AS "PendingIssues",
    COUNT(CASE WHEN sai."Category" = 'Content' THEN 1 END) AS "ContentIssues",
    COUNT(CASE WHEN sai."Category" = 'AEO' THEN 1 END) AS "AeoIssues",
    COUNT(CASE WHEN sai."Category" = 'Technical' THEN 1 END) AS "TechnicalIssues",
    sar."CreatedAt" AS "AuditDate"
FROM "SiteAuditRuns" sar
LEFT JOIN "SiteAuditIssues" sai ON sar."Id" = sai."SiteAuditRunId"
GROUP BY sar."ProjectId", sar."Id", sar."CreatedAt";

COMMENT ON TABLE "SiteAuditIssues" IS 'Stores individual audit issues found during site audits with completion tracking';
COMMENT ON TABLE "OpportunityTasks" IS 'Links opportunities to tasks for tracking implementation';
COMMENT ON VIEW "VW_AuditIssuesSummary" IS 'Provides summary statistics for audit issues by project and audit run';
