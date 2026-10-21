-- Migration: Add Brand Ownership Flag
-- Date: 2026-02-21
-- Description: Adds IsCompetitor boolean field to prompts and related tracking tables to distinguish between own brand and competitor prompts

-- ============================================
-- 1. Add IsCompetitor to PromptTracking
-- ============================================
ALTER TABLE "PromptTracking" 
ADD COLUMN "IsCompetitor" BOOLEAN NOT NULL DEFAULT FALSE;

-- Add index for filtering by IsCompetitor
CREATE INDEX "IX_PromptTracking_IsCompetitor" ON "PromptTracking"("IsCompetitor");

-- Add composite index for common queries
CREATE INDEX "IX_PromptTracking_Project_IsCompetitor" ON "PromptTracking"("ProjectId", "IsCompetitor");

-- Add comment
COMMENT ON COLUMN "PromptTracking"."IsCompetitor" IS 'Indicates if prompt is for competitor brand. FALSE = Own brand, TRUE = Competitor';

-- ============================================
-- 2. Add IsCompetitor to PromptPlatformTracking
-- ============================================
ALTER TABLE "PromptPlatformTracking" 
ADD COLUMN "IsCompetitor" BOOLEAN NOT NULL DEFAULT FALSE;

-- Add index for filtering
CREATE INDEX "IX_PromptPlatformTracking_IsCompetitor" ON "PromptPlatformTracking"("IsCompetitor");

-- Add composite index for platform + ownership queries
CREATE INDEX "IX_PromptPlatformTracking_Platform_IsCompetitor" ON "PromptPlatformTracking"("Platform", "IsCompetitor");

-- Add comment
COMMENT ON COLUMN "PromptPlatformTracking"."IsCompetitor" IS 'Inherited from parent PromptTracking. FALSE = Own brand, TRUE = Competitor';

-- ============================================
-- 3. Add IsCompetitor to PromptVisibilityMetrics
-- ============================================
ALTER TABLE "PromptVisibilityMetrics" 
ADD COLUMN "IsCompetitor" BOOLEAN NOT NULL DEFAULT FALSE;

-- Add index for filtering metrics by ownership
CREATE INDEX "IX_PromptVisibilityMetrics_IsCompetitor" ON "PromptVisibilityMetrics"("IsCompetitor");

-- Add composite index for date-based ownership queries
CREATE INDEX "IX_PromptVisibilityMetrics_CheckDate_IsCompetitor" ON "PromptVisibilityMetrics"("CheckDate", "IsCompetitor");

-- Add comment
COMMENT ON COLUMN "PromptVisibilityMetrics"."IsCompetitor" IS 'Inherited from parent PromptPlatformTracking. FALSE = Own brand, TRUE = Competitor';

-- ============================================
-- 4. Add IsCompetitor to Citation
-- ============================================
ALTER TABLE "Citation" 
ADD COLUMN "IsCompetitor" BOOLEAN NOT NULL DEFAULT FALSE;

-- Add index for filtering citations by ownership
CREATE INDEX "IX_Citation_IsCompetitor" ON "Citation"("IsCompetitor");

-- Add composite index for project + ownership queries
CREATE INDEX "IX_Citation_Project_IsCompetitor" ON "Citation"("ProjectId", "IsCompetitor");

-- Add composite index for platform + ownership queries
CREATE INDEX "IX_Citation_Platform_IsCompetitor" ON "Citation"("Platform", "IsCompetitor");

-- Add comment
COMMENT ON COLUMN "Citation"."IsCompetitor" IS 'Indicates if citation is for competitor prompt. FALSE = Own brand, TRUE = Competitor';

-- ============================================
-- 5. Add IsCompetitor to CitationMetric
-- ============================================
ALTER TABLE "CitationMetric" 
ADD COLUMN "IsCompetitor" BOOLEAN NOT NULL DEFAULT FALSE;

-- Add index for filtering metrics by ownership
CREATE INDEX "IX_CitationMetric_IsCompetitor" ON "CitationMetric"("IsCompetitor");

-- Add composite index for date + ownership queries
CREATE INDEX "IX_CitationMetric_CheckDate_IsCompetitor" ON "CitationMetric"("CheckDate", "IsCompetitor");

-- Add comment
COMMENT ON COLUMN "CitationMetric"."IsCompetitor" IS 'Inherited from parent Citation. FALSE = Own brand, TRUE = Competitor';

-- ============================================
-- 6. Add IsCompetitor to BrandMention
-- ============================================
ALTER TABLE "BrandMention" 
ADD COLUMN "IsCompetitor" BOOLEAN NOT NULL DEFAULT FALSE;

-- Add index for filtering mentions by ownership
CREATE INDEX "IX_BrandMention_IsCompetitor" ON "BrandMention"("IsCompetitor");

-- Add composite index for project + ownership queries
CREATE INDEX "IX_BrandMention_Project_IsCompetitor" ON "BrandMention"("ProjectId", "IsCompetitor");

-- Add composite index for platform + ownership queries
CREATE INDEX "IX_BrandMention_Platform_IsCompetitor" ON "BrandMention"("Platform", "IsCompetitor");

-- Add comment
COMMENT ON COLUMN "BrandMention"."IsCompetitor" IS 'Indicates if mention is for competitor prompt. FALSE = Own brand, TRUE = Competitor';

-- ============================================
-- 7. Create trigger to auto-populate IsCompetitor in child tables
-- ============================================

-- Trigger function to sync IsCompetitor from PromptTracking to PromptPlatformTracking
CREATE OR REPLACE FUNCTION sync_prompt_platform_is_competitor()
RETURNS TRIGGER AS $$
BEGIN
    -- When inserting, get IsCompetitor from parent PromptTracking
    IF TG_OP = 'INSERT' THEN
        SELECT "IsCompetitor" INTO NEW."IsCompetitor"
        FROM "PromptTracking"
        WHERE "Id" = NEW."PromptId";
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_sync_prompt_platform_is_competitor
    BEFORE INSERT ON "PromptPlatformTracking"
    FOR EACH ROW
    EXECUTE FUNCTION sync_prompt_platform_is_competitor();

-- Trigger function to sync IsCompetitor from PromptPlatformTracking to PromptVisibilityMetrics
CREATE OR REPLACE FUNCTION sync_visibility_metrics_is_competitor()
RETURNS TRIGGER AS $$
BEGIN
    -- When inserting, get IsCompetitor from parent PromptPlatformTracking
    IF TG_OP = 'INSERT' THEN
        SELECT "IsCompetitor" INTO NEW."IsCompetitor"
        FROM "PromptPlatformTracking"
        WHERE "Id" = NEW."PromptPlatformId";
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_sync_visibility_metrics_is_competitor
    BEFORE INSERT ON "PromptVisibilityMetrics"
    FOR EACH ROW
    EXECUTE FUNCTION sync_visibility_metrics_is_competitor();

-- Trigger function to sync IsCompetitor from Citation to CitationMetric
CREATE OR REPLACE FUNCTION sync_citation_metric_is_competitor()
RETURNS TRIGGER AS $$
BEGIN
    -- When inserting, get IsCompetitor from parent Citation
    IF TG_OP = 'INSERT' THEN
        SELECT "IsCompetitor" INTO NEW."IsCompetitor"
        FROM "Citation"
        WHERE "Id" = NEW."CitationId";
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_sync_citation_metric_is_competitor
    BEFORE INSERT ON "CitationMetric"
    FOR EACH ROW
    EXECUTE FUNCTION sync_citation_metric_is_competitor();

-- ============================================
-- Migration Complete
-- ============================================
-- Tables Updated: 6
-- Columns Added: 6 (IsCompetitor BOOLEAN)
-- Indexes Created: 15
-- Triggers Created: 3
-- Comments: IsCompetitor = FALSE for Own brand, TRUE for Competitor
