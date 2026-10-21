-- ============================================================================
-- Migration Script: Align Database Schema with Entity Classes
-- ============================================================================
-- This script updates existing tables to match the actual C# entity classes
-- Run this ONLY if you have an existing database that was created from the
-- old COMPLETE_AISEO_SCHEMA.sql file
-- ============================================================================

-- ============================================================================
-- 1. PromptVisibilityMetrics - Rename column
-- ============================================================================
DO $$ 
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'PromptVisibilityMetrics' 
        AND column_name = 'PlatformTrackingId'
    ) THEN
        -- Rename column
        ALTER TABLE "PromptVisibilityMetrics" 
        RENAME COLUMN "PlatformTrackingId" TO "PromptPlatformId";
        
        -- Drop old indexes
        DROP INDEX IF EXISTS "IX_PromptVisibilityMetrics_PlatformTrackingId";
        DROP INDEX IF EXISTS "IX_PromptVisibilityMetrics_PlatformTrackingId_CheckDate";
        
        -- Create new indexes
        CREATE INDEX "IX_PromptVisibilityMetrics_PromptPlatformId" 
            ON "PromptVisibilityMetrics"("PromptPlatformId");
        CREATE INDEX "IX_PromptVisibilityMetrics_PromptPlatformId_CheckDate" 
            ON "PromptVisibilityMetrics"("PromptPlatformId", "CheckDate");
        
        -- Update foreign key constraint
        ALTER TABLE "PromptVisibilityMetrics" 
        DROP CONSTRAINT IF EXISTS "FK_PromptVisibilityMetrics_PromptPlatformTrackings_PlatformTrackingId";
        
        ALTER TABLE "PromptVisibilityMetrics" 
        ADD CONSTRAINT "FK_PromptVisibilityMetrics_PromptPlatformTrackings_PromptPlatformId" 
        FOREIGN KEY ("PromptPlatformId") REFERENCES "PromptPlatformTrackings"("Id") ON DELETE CASCADE;
        
        RAISE NOTICE 'PromptVisibilityMetrics: Renamed PlatformTrackingId to PromptPlatformId';
    END IF;
    
    -- Add missing columns
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'PromptVisibilityMetrics' AND column_name = 'Position'
    ) THEN
        ALTER TABLE "PromptVisibilityMetrics" ADD COLUMN "Position" INTEGER;
        RAISE NOTICE 'PromptVisibilityMetrics: Added Position column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'PromptVisibilityMetrics' AND column_name = 'SentimentScore'
    ) THEN
        ALTER TABLE "PromptVisibilityMetrics" ADD COLUMN "SentimentScore" DOUBLE PRECISION;
        RAISE NOTICE 'PromptVisibilityMetrics: Added SentimentScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'PromptVisibilityMetrics' AND column_name = 'ResponseText'
    ) THEN
        ALTER TABLE "PromptVisibilityMetrics" ADD COLUMN "ResponseText" TEXT;
        RAISE NOTICE 'PromptVisibilityMetrics: Added ResponseText column';
    END IF;
END $$;

-- ============================================================================
-- 2. PromptResearchDatas - Remove ProjectId/Status, Add Source
-- ============================================================================
DO $$ 
BEGIN
    -- Add Source column if missing
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'PromptResearchDatas' AND column_name = 'Source'
    ) THEN
        ALTER TABLE "PromptResearchDatas" 
        ADD COLUMN "Source" VARCHAR(50) NOT NULL DEFAULT 'Manual';
        
        CREATE INDEX "IX_PromptResearchDatas_Source" ON "PromptResearchDatas"("Source");
        RAISE NOTICE 'PromptResearchDatas: Added Source column';
    END IF;
    
    -- Drop ProjectId foreign key and column if exists
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'PromptResearchDatas' AND column_name = 'ProjectId'
    ) THEN
        ALTER TABLE "PromptResearchDatas" 
        DROP CONSTRAINT IF EXISTS "FK_PromptResearchDatas_Projects_ProjectId";
        
        DROP INDEX IF EXISTS "IX_PromptResearchDatas_ProjectId";
        
        ALTER TABLE "PromptResearchDatas" DROP COLUMN "ProjectId";
        RAISE NOTICE 'PromptResearchDatas: Removed ProjectId column';
    END IF;
    
    -- Drop Status column if exists
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'PromptResearchDatas' AND column_name = 'Status'
    ) THEN
        DROP INDEX IF EXISTS "IX_PromptResearchDatas_Status";
        ALTER TABLE "PromptResearchDatas" DROP COLUMN "Status";
        RAISE NOTICE 'PromptResearchDatas: Removed Status column';
    END IF;
END $$;

-- ============================================================================
-- 3. Citations - Add Platform, SourceTitle, PromptText, Position
-- ============================================================================
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Citations' AND column_name = 'Platform'
    ) THEN
        ALTER TABLE "Citations" ADD COLUMN "Platform" VARCHAR(50) NOT NULL DEFAULT 'ChatGPT';
        CREATE INDEX "IX_Citations_Platform" ON "Citations"("Platform");
        RAISE NOTICE 'Citations: Added Platform column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Citations' AND column_name = 'SourceTitle'
    ) THEN
        ALTER TABLE "Citations" ADD COLUMN "SourceTitle" VARCHAR(500) NOT NULL DEFAULT '';
        RAISE NOTICE 'Citations: Added SourceTitle column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Citations' AND column_name = 'PromptText'
    ) THEN
        ALTER TABLE "Citations" ADD COLUMN "PromptText" TEXT;
        RAISE NOTICE 'Citations: Added PromptText column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Citations' AND column_name = 'Position'
    ) THEN
        ALTER TABLE "Citations" ADD COLUMN "Position" INTEGER;
        RAISE NOTICE 'Citations: Added Position column';
    END IF;
    
    -- Remove Sentiment and IsVerified if they exist
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Citations' AND column_name = 'Sentiment'
    ) THEN
        ALTER TABLE "Citations" DROP COLUMN "Sentiment";
        RAISE NOTICE 'Citations: Removed Sentiment column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Citations' AND column_name = 'IsVerified'
    ) THEN
        ALTER TABLE "Citations" DROP COLUMN "IsVerified";
        RAISE NOTICE 'Citations: Removed IsVerified column';
    END IF;
    
    -- Create composite index
    DROP INDEX IF EXISTS "IX_Citations_ProjectId_SourceUrl_Platform";
    CREATE INDEX "IX_Citations_ProjectId_SourceUrl_Platform" 
        ON "Citations"("ProjectId", "SourceUrl", "Platform");
END $$;

-- ============================================================================
-- 4. CitationMetrics - Change from project aggregates to per-citation tracking
-- ============================================================================
DO $$ 
BEGIN
    -- Check if this is the old structure (has ProjectId and MetricDate)
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationMetrics' AND column_name = 'ProjectId'
    ) THEN
        RAISE NOTICE 'CitationMetrics: Old structure detected. Manual migration required!';
        RAISE NOTICE 'CitationMetrics: The table structure has fundamentally changed.';
        RAISE NOTICE 'CitationMetrics: Consider backing up data and recreating the table.';
        -- This is a breaking change - data migration would need custom logic
    ELSE
        -- Add columns for new structure if missing
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'CitationMetrics' AND column_name = 'CitationId'
        ) THEN
            ALTER TABLE "CitationMetrics" ADD COLUMN "CitationId" INTEGER NOT NULL;
            ALTER TABLE "CitationMetrics" 
            ADD CONSTRAINT "FK_CitationMetrics_Citations_CitationId" 
            FOREIGN KEY ("CitationId") REFERENCES "Citations"("Id") ON DELETE CASCADE;
            CREATE INDEX "IX_CitationMetrics_CitationId" ON "CitationMetrics"("CitationId");
        END IF;
    END IF;
END $$;

-- ============================================================================
-- 5. BrandMentions - Change to tracking-focused model
-- ============================================================================
DO $$ 
BEGIN
    -- Add tracking-focused columns
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'PromptText'
    ) THEN
        ALTER TABLE "BrandMentions" ADD COLUMN "PromptText" TEXT NOT NULL DEFAULT '';
        RAISE NOTICE 'BrandMentions: Added PromptText column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'IsMentioned'
    ) THEN
        ALTER TABLE "BrandMentions" ADD COLUMN "IsMentioned" BOOLEAN NOT NULL DEFAULT FALSE;
        CREATE INDEX "IX_BrandMentions_IsMentioned" ON "BrandMentions"("IsMentioned");
        RAISE NOTICE 'BrandMentions: Added IsMentioned column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'Position'
    ) THEN
        ALTER TABLE "BrandMentions" ADD COLUMN "Position" INTEGER;
        RAISE NOTICE 'BrandMentions: Added Position column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'MentionType'
    ) THEN
        ALTER TABLE "BrandMentions" ADD COLUMN "MentionType" VARCHAR(50) NOT NULL DEFAULT 'Direct';
        RAISE NOTICE 'BrandMentions: Added MentionType column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'MentionContext'
    ) THEN
        ALTER TABLE "BrandMentions" ADD COLUMN "MentionContext" TEXT;
        RAISE NOTICE 'BrandMentions: Added MentionContext column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'SentimentScore'
    ) THEN
        ALTER TABLE "BrandMentions" ADD COLUMN "SentimentScore" DOUBLE PRECISION;
        RAISE NOTICE 'BrandMentions: Added SentimentScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'ResponseText'
    ) THEN
        ALTER TABLE "BrandMentions" ADD COLUMN "ResponseText" TEXT;
        RAISE NOTICE 'BrandMentions: Added ResponseText column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'CheckDate'
    ) THEN
        ALTER TABLE "BrandMentions" ADD COLUMN "CheckDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
        CREATE INDEX "IX_BrandMentions_CheckDate" ON "BrandMentions"("CheckDate");
        RAISE NOTICE 'BrandMentions: Added CheckDate column';
    END IF;
    
    -- Remove old columns
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'MentionText'
    ) THEN
        ALTER TABLE "BrandMentions" DROP COLUMN "MentionText";
        RAISE NOTICE 'BrandMentions: Removed MentionText column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'SourceUrl'
    ) THEN
        ALTER TABLE "BrandMentions" DROP COLUMN "SourceUrl";
        RAISE NOTICE 'BrandMentions: Removed SourceUrl column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'Reach'
    ) THEN
        ALTER TABLE "BrandMentions" DROP COLUMN "Reach";
        RAISE NOTICE 'BrandMentions: Removed Reach column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'Engagement'
    ) THEN
        ALTER TABLE "BrandMentions" DROP COLUMN "Engagement";
        RAISE NOTICE 'BrandMentions: Removed Engagement column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'BrandMentions' AND column_name = 'MentionedAt'
    ) THEN
        DROP INDEX IF EXISTS "IX_BrandMentions_MentionedAt";
        ALTER TABLE "BrandMentions" DROP COLUMN "MentionedAt";
        RAISE NOTICE 'BrandMentions: Removed MentionedAt column';
    END IF;
    
    -- Create composite index
    DROP INDEX IF EXISTS "IX_BrandMentions_ProjectId_Platform_CheckDate";
    CREATE INDEX "IX_BrandMentions_ProjectId_Platform_CheckDate" 
        ON "BrandMentions"("ProjectId", "Platform", "CheckDate");
END $$;

-- ============================================================================
-- 6. CitationAggregations - Change to domain/platform model
-- ============================================================================
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'SourceDomain'
    ) THEN
        ALTER TABLE "CitationAggregations" ADD COLUMN "SourceDomain" VARCHAR(255) NOT NULL DEFAULT '';
        CREATE INDEX "IX_CitationAggregations_SourceDomain" ON "CitationAggregations"("SourceDomain");
        RAISE NOTICE 'CitationAggregations: Added SourceDomain column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'Platform'
    ) THEN
        ALTER TABLE "CitationAggregations" ADD COLUMN "Platform" VARCHAR(50) NOT NULL DEFAULT 'ChatGPT';
        CREATE INDEX "IX_CitationAggregations_Platform" ON "CitationAggregations"("Platform");
        RAISE NOTICE 'CitationAggregations: Added Platform column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'AggregationDate'
    ) THEN
        ALTER TABLE "CitationAggregations" ADD COLUMN "AggregationDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
        CREATE INDEX "IX_CitationAggregations_AggregationDate" ON "CitationAggregations"("AggregationDate");
        RAISE NOTICE 'CitationAggregations: Added AggregationDate column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'UniqueCitations'
    ) THEN
        ALTER TABLE "CitationAggregations" ADD COLUMN "UniqueCitations" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'CitationAggregations: Added UniqueCitations column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'AveragePosition'
    ) THEN
        ALTER TABLE "CitationAggregations" ADD COLUMN "AveragePosition" DOUBLE PRECISION NOT NULL DEFAULT 0;
        RAISE NOTICE 'CitationAggregations: Added AveragePosition column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'TopPosition'
    ) THEN
        ALTER TABLE "CitationAggregations" ADD COLUMN "TopPosition" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'CitationAggregations: Added TopPosition column';
    END IF;
    
    -- Remove old columns
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'Period'
    ) THEN
        DROP INDEX IF EXISTS "IX_CitationAggregations_Period";
        ALTER TABLE "CitationAggregations" DROP COLUMN "Period";
        RAISE NOTICE 'CitationAggregations: Removed Period column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'StartDate'
    ) THEN
        DROP INDEX IF EXISTS "IX_CitationAggregations_StartDate";
        ALTER TABLE "CitationAggregations" DROP COLUMN "StartDate";
        RAISE NOTICE 'CitationAggregations: Removed StartDate column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'EndDate'
    ) THEN
        ALTER TABLE "CitationAggregations" DROP COLUMN "EndDate";
        RAISE NOTICE 'CitationAggregations: Removed EndDate column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CitationAggregations' AND column_name = 'TopDomains'
    ) THEN
        ALTER TABLE "CitationAggregations" DROP COLUMN "TopDomains";
        RAISE NOTICE 'CitationAggregations: Removed TopDomains column';
    END IF;
    
    -- Create composite unique index
    DROP INDEX IF EXISTS "IX_CitationAggregations_ProjectId_SourceDomain_Platform_AggregationDate";
    CREATE UNIQUE INDEX "IX_CitationAggregations_ProjectId_SourceDomain_Platform_AggregationDate" 
        ON "CitationAggregations"("ProjectId", "SourceDomain", "Platform", "AggregationDate");
END $$;

-- ============================================================================
-- 7. SiteAuditHistories - Add detailed score breakdown
-- ============================================================================
DO $$ 
BEGIN
    -- Rename AuditDate to CheckDate
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'AuditDate'
    ) THEN
        ALTER TABLE "SiteAuditHistories" RENAME COLUMN "AuditDate" TO "CheckDate";
        DROP INDEX IF EXISTS "IX_SiteAuditHistories_AuditDate";
        CREATE INDEX "IX_SiteAuditHistories_CheckDate" ON "SiteAuditHistories"("CheckDate");
        RAISE NOTICE 'SiteAuditHistories: Renamed AuditDate to CheckDate';
    END IF;
    
    -- Add detailed score columns
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'LlmsTxtScore'
    ) THEN
        ALTER TABLE "SiteAuditHistories" ADD COLUMN "LlmsTxtScore" INTEGER;
        RAISE NOTICE 'SiteAuditHistories: Added LlmsTxtScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'SchemaScore'
    ) THEN
        ALTER TABLE "SiteAuditHistories" ADD COLUMN "SchemaScore" INTEGER;
        RAISE NOTICE 'SiteAuditHistories: Added SchemaScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'DomClarityScore'
    ) THEN
        ALTER TABLE "SiteAuditHistories" ADD COLUMN "DomClarityScore" INTEGER;
        RAISE NOTICE 'SiteAuditHistories: Added DomClarityScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'ContentScore'
    ) THEN
        ALTER TABLE "SiteAuditHistories" ADD COLUMN "ContentScore" INTEGER;
        RAISE NOTICE 'SiteAuditHistories: Added ContentScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'TechnicalScore'
    ) THEN
        ALTER TABLE "SiteAuditHistories" ADD COLUMN "TechnicalScore" INTEGER;
        RAISE NOTICE 'SiteAuditHistories: Added TechnicalScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'TotalIssues'
    ) THEN
        ALTER TABLE "SiteAuditHistories" ADD COLUMN "TotalIssues" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'SiteAuditHistories: Added TotalIssues column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'CriticalIssues'
    ) THEN
        ALTER TABLE "SiteAuditHistories" ADD COLUMN "CriticalIssues" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'SiteAuditHistories: Added CriticalIssues column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'WarningIssues'
    ) THEN
        ALTER TABLE "SiteAuditHistories" ADD COLUMN "WarningIssues" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'SiteAuditHistories: Added WarningIssues column';
    END IF;
    
    -- Remove old columns
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'IssuesFound'
    ) THEN
        ALTER TABLE "SiteAuditHistories" DROP COLUMN "IssuesFound";
        RAISE NOTICE 'SiteAuditHistories: Removed IssuesFound column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'IssuesFixed'
    ) THEN
        ALTER TABLE "SiteAuditHistories" DROP COLUMN "IssuesFixed";
        RAISE NOTICE 'SiteAuditHistories: Removed IssuesFixed column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SiteAuditHistories' AND column_name = 'ChangesSummary'
    ) THEN
        ALTER TABLE "SiteAuditHistories" DROP COLUMN "ChangesSummary";
        RAISE NOTICE 'SiteAuditHistories: Removed ChangesSummary column';
    END IF;
END $$;

-- ============================================================================
-- 8. CompetitiveIntelligence - Change to specific metrics model
-- ============================================================================
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CompetitiveIntelligence' AND column_name = 'VisibilityScore'
    ) THEN
        ALTER TABLE "CompetitiveIntelligence" ADD COLUMN "VisibilityScore" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'CompetitiveIntelligence: Added VisibilityScore column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CompetitiveIntelligence' AND column_name = 'CitationShare'
    ) THEN
        ALTER TABLE "CompetitiveIntelligence" ADD COLUMN "CitationShare" DECIMAL(5,2) NOT NULL DEFAULT 0;
        RAISE NOTICE 'CompetitiveIntelligence: Added CitationShare column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CompetitiveIntelligence' AND column_name = 'Rank'
    ) THEN
        ALTER TABLE "CompetitiveIntelligence" ADD COLUMN "Rank" INTEGER;
        RAISE NOTICE 'CompetitiveIntelligence: Added Rank column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CompetitiveIntelligence' AND column_name = 'PromptsTracked'
    ) THEN
        ALTER TABLE "CompetitiveIntelligence" ADD COLUMN "PromptsTracked" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'CompetitiveIntelligence: Added PromptsTracked column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CompetitiveIntelligence' AND column_name = 'AverageMentionPosition'
    ) THEN
        ALTER TABLE "CompetitiveIntelligence" ADD COLUMN "AverageMentionPosition" DECIMAL(4,2);
        RAISE NOTICE 'CompetitiveIntelligence: Added AverageMentionPosition column';
    END IF;
    
    -- Remove old columns
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CompetitiveIntelligence' AND column_name = 'MetricType'
    ) THEN
        ALTER TABLE "CompetitiveIntelligence" DROP COLUMN "MetricType";
        RAISE NOTICE 'CompetitiveIntelligence: Removed MetricType column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CompetitiveIntelligence' AND column_name = 'MetricValue'
    ) THEN
        ALTER TABLE "CompetitiveIntelligence" DROP COLUMN "MetricValue";
        RAISE NOTICE 'CompetitiveIntelligence: Removed MetricValue column';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'CompetitiveIntelligence' AND column_name = 'Notes'
    ) THEN
        ALTER TABLE "CompetitiveIntelligence" DROP COLUMN "Notes";
        RAISE NOTICE 'CompetitiveIntelligence: Removed Notes column';
    END IF;
END $$;

-- ============================================================================
-- Migration Complete
-- ============================================================================
DO $$ 
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Schema alignment migration completed!';
    RAISE NOTICE '========================================';
END $$;
