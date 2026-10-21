-- Migration: Add Competitor Type Classification (Direct vs SERP)
-- Date: 2026-02-21
-- Description: Adds CompetitorType field to distinguish between Direct competitors (manually added)
--              and SERP competitors (found during prompt tracking). Also adds CompetitorId and 
--              CompetitorType tracking to BrandMention and Citation tables.

-- ============================================
-- 1. Add CompetitorType to Competitor table
-- ============================================
ALTER TABLE "Competitor" 
ADD COLUMN IF NOT EXISTS "CompetitorType" VARCHAR(20) NOT NULL DEFAULT 'Direct';

-- Add index for filtering by competitor type
CREATE INDEX IF NOT EXISTS "IX_Competitor_CompetitorType" ON "Competitor"("CompetitorType");

-- Add composite index for project + type queries
CREATE INDEX IF NOT EXISTS "IX_Competitor_Project_Type" ON "Competitor"("ProjectId", "CompetitorType");

-- Add comment
COMMENT ON COLUMN "Competitor"."CompetitorType" IS 'Type of competitor: Direct (manually added by user) or SERP (discovered during prompt tracking)';

-- ============================================
-- 2. Add CompetitorId and CompetitorType to BrandMention
-- ============================================
ALTER TABLE "BrandMention" 
ADD COLUMN IF NOT EXISTS "CompetitorId" INTEGER,
ADD COLUMN IF NOT EXISTS "CompetitorType" VARCHAR(20);

-- Add foreign key constraint
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_BrandMention_Competitor'
    ) THEN
        ALTER TABLE "BrandMention"
        ADD CONSTRAINT "FK_BrandMention_Competitor" 
        FOREIGN KEY ("CompetitorId") 
        REFERENCES "Competitor"("Id") 
        ON DELETE SET NULL;
    END IF;
END $$;

-- Add indexes
CREATE INDEX IF NOT EXISTS "IX_BrandMention_CompetitorId" ON "BrandMention"("CompetitorId");
CREATE INDEX IF NOT EXISTS "IX_BrandMention_CompetitorType" ON "BrandMention"("CompetitorType");
CREATE INDEX IF NOT EXISTS "IX_BrandMention_Project_CompetitorType" ON "BrandMention"("ProjectId", "CompetitorType");

-- Add comments
COMMENT ON COLUMN "BrandMention"."CompetitorId" IS 'Reference to the Competitor if this mention is for a competitor brand';
COMMENT ON COLUMN "BrandMention"."CompetitorType" IS 'Type of competitor: Direct or SERP. Only populated when IsCompetitor = true';

-- ============================================
-- 3. Add CompetitorId and CompetitorType to Citation
-- ============================================
ALTER TABLE "Citation" 
ADD COLUMN IF NOT EXISTS "CompetitorId" INTEGER,
ADD COLUMN IF NOT EXISTS "CompetitorType" VARCHAR(20);

-- Add foreign key constraint
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_Citation_Competitor'
    ) THEN
        ALTER TABLE "Citation"
        ADD CONSTRAINT "FK_Citation_Competitor" 
        FOREIGN KEY ("CompetitorId") 
        REFERENCES "Competitor"("Id") 
        ON DELETE SET NULL;
    END IF;
END $$;

-- Add indexes
CREATE INDEX IF NOT EXISTS "IX_Citation_CompetitorId" ON "Citation"("CompetitorId");
CREATE INDEX IF NOT EXISTS "IX_Citation_CompetitorType" ON "Citation"("CompetitorType");
CREATE INDEX IF NOT EXISTS "IX_Citation_Project_CompetitorType" ON "Citation"("ProjectId", "CompetitorType");

-- Add comments
COMMENT ON COLUMN "Citation"."CompetitorId" IS 'Reference to the Competitor if this citation is for a competitor brand';
COMMENT ON COLUMN "Citation"."CompetitorType" IS 'Type of competitor: Direct or SERP. Only populated when IsCompetitor = true';

-- ============================================
-- 4. Update existing Direct competitors
-- ============================================
-- Set all existing competitors to 'Direct' type (they were manually added)
UPDATE "Competitor" 
SET "CompetitorType" = 'Direct' 
WHERE "CompetitorType" IS NULL OR "CompetitorType" = '';

-- ============================================
-- 5. Create helper function to find or create SERP competitor
-- ============================================
CREATE OR REPLACE FUNCTION find_or_create_serp_competitor(
    p_project_id INTEGER,
    p_domain VARCHAR(255),
    p_name VARCHAR(200)
)
RETURNS INTEGER AS $$
DECLARE
    v_competitor_id INTEGER;
BEGIN
    -- Try to find existing SERP competitor
    SELECT "Id" INTO v_competitor_id
    FROM "Competitor"
    WHERE "ProjectId" = p_project_id 
      AND "Domain" = p_domain 
      AND "CompetitorType" = 'SERP'
      AND "IsActive" = TRUE;
    
    -- If not found, create new SERP competitor
    IF v_competitor_id IS NULL THEN
        INSERT INTO "Competitor" (
            "ProjectId",
            "Name",
            "Domain",
            "CompetitorType",
            "IsActive",
            "CreatedAt"
        ) VALUES (
            p_project_id,
            p_name,
            p_domain,
            'SERP',
            TRUE,
            CURRENT_TIMESTAMP
        )
        RETURNING "Id" INTO v_competitor_id;
    END IF;
    
    RETURN v_competitor_id;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION find_or_create_serp_competitor IS 'Helper function to find existing SERP competitor or create new one during prompt tracking';

-- ============================================
-- 6. Create view for competitor analytics
-- ============================================
CREATE OR REPLACE VIEW "CompetitorAnalytics" AS
SELECT 
    c."Id" AS "CompetitorId",
    c."ProjectId",
    c."Name",
    c."Domain",
    c."CompetitorType",
    c."IsActive",
    
    -- Brand Mention stats
    COUNT(DISTINCT bm."Id") AS "TotalMentions",
    COUNT(DISTINCT CASE WHEN bm."IsMentioned" = TRUE THEN bm."Id" END) AS "PositiveMentions",
    AVG(CASE WHEN bm."IsMentioned" = TRUE THEN bm."Position" END) AS "AverageMentionPosition",
    
    -- Citation stats
    COUNT(DISTINCT ct."Id") AS "TotalCitations",
    AVG(CASE WHEN ct."Id" IS NOT NULL THEN ct."Position" END) AS "AverageCitationPosition",
    
    -- Sentiment analysis
    AVG(bm."SentimentScore") AS "AverageSentiment",
    COUNT(DISTINCT CASE WHEN bm."Sentiment" = 'Positive' THEN bm."Id" END) AS "PositiveSentimentCount",
    COUNT(DISTINCT CASE WHEN bm."Sentiment" = 'Negative' THEN bm."Id" END) AS "NegativeSentimentCount",
    
    -- Platform breakdown
    COUNT(DISTINCT bm."Platform") AS "PlatformsCovered",
    
    -- Timestamps
    MAX(bm."CheckDate") AS "LastMentionDate",
    MAX(ct."DiscoveredAt") AS "LastCitationDate",
    c."CreatedAt" AS "CompetitorCreatedAt"
    
FROM "Competitor" c
LEFT JOIN "BrandMention" bm ON c."Id" = bm."CompetitorId"
LEFT JOIN "Citation" ct ON c."Id" = ct."CompetitorId"
GROUP BY c."Id", c."ProjectId", c."Name", c."Domain", c."CompetitorType", c."IsActive", c."CreatedAt";

COMMENT ON VIEW "CompetitorAnalytics" IS 'Analytics view showing competitor performance metrics including mentions, citations, and sentiment';

-- ============================================
-- 7. Create indexes on the view for better performance
-- ============================================
-- Note: Materialized view would be better for large datasets
-- To create materialized view instead, uncomment below:
/*
DROP VIEW IF EXISTS "CompetitorAnalytics";
CREATE MATERIALIZED VIEW "CompetitorAnalytics" AS
[same SELECT statement as above];

CREATE INDEX "IX_CompetitorAnalytics_ProjectId" ON "CompetitorAnalytics"("ProjectId");
CREATE INDEX "IX_CompetitorAnalytics_CompetitorType" ON "CompetitorAnalytics"("CompetitorType");

-- Refresh materialized view (run this periodically or via cron job)
-- REFRESH MATERIALIZED VIEW "CompetitorAnalytics";
*/

-- ============================================
-- Migration Complete
-- ============================================
-- Summary:
-- - Added CompetitorType to Competitor table (Direct/SERP)
-- - Added CompetitorId and CompetitorType to BrandMention table
-- - Added CompetitorId and CompetitorType to Citation table
-- - Created helper function for SERP competitor management
-- - Created CompetitorAnalytics view for reporting
-- - Created 9 new indexes for query performance
-- - All existing competitors marked as 'Direct' type
