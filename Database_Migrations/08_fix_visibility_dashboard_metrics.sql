-- ============================================================================
-- Migration Script: Fix VisibilityDashboardMetrics Table
-- ============================================================================
-- This script adds missing columns to the VisibilityDashboardMetrics table
-- to match the actual C# entity class
-- ============================================================================

DO $$ 
BEGIN
    RAISE NOTICE 'Starting VisibilityDashboardMetrics table migration...';
    
    -- Overall Visibility columns
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'OverallVisibilityScore'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "OverallVisibilityScore" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added OverallVisibilityScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'VisibilityRank'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "VisibilityRank" INTEGER;
        RAISE NOTICE 'Added VisibilityRank column';
    END IF;
    
    -- VisibilityChange should already exist, but ensure it's there
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'VisibilityChange'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "VisibilityChange" DECIMAL(5,2);
        RAISE NOTICE 'Added VisibilityChange column';
    END IF;
    
    -- Prompt Performance columns
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'PromptsWithMentions'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "PromptsWithMentions" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added PromptsWithMentions column';
    END IF;
    
    -- Rename TotalMentions to MentionRate if needed
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'TotalMentions'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        DROP COLUMN "TotalMentions";
        RAISE NOTICE 'Removed TotalMentions column';
    END IF;
    
    -- Citation Metrics columns
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'TotalCitations'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "TotalCitations" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added TotalCitations column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'CitationRank'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "CitationRank" INTEGER;
        RAISE NOTICE 'Added CitationRank column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'UniqueDomainsCiting'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "UniqueDomainsCiting" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added UniqueDomainsCiting column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'OwnedDomainCitations'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "OwnedDomainCitations" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added OwnedDomainCitations column';
    END IF;
    
    -- Site Health columns
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'SiteAuditScore'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "SiteAuditScore" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added SiteAuditScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'CriticalIssues'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "CriticalIssues" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added CriticalIssues column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'WarningIssues'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "WarningIssues" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added WarningIssues column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'PassedChecks'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "PassedChecks" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added PassedChecks column';
    END IF;
    
    -- Opportunities columns
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'TotalOpportunities'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "TotalOpportunities" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added TotalOpportunities column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'HighPriorityOpportunities'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "HighPriorityOpportunities" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added HighPriorityOpportunities column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VisibilityDashboardMetrics' AND column_name = 'OpportunitiesActioned'
    ) THEN
        ALTER TABLE "VisibilityDashboardMetrics" 
        ADD COLUMN "OpportunitiesActioned" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added OpportunitiesActioned column';
    END IF;
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'VisibilityDashboardMetrics migration completed!';
    RAISE NOTICE '========================================';
END $$;
