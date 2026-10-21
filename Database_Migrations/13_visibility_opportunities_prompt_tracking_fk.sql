-- ============================================================================
-- Migration 13: Add PromptTrackingId FK to VisibilityOpportunities
--
-- Purpose:
--   Replace text-based deduplication (PromptText string comparison) with an
--   integer FK to PromptTrackings.Id. This reduces storage, speeds up dedup
--   queries (integer vs TEXT comparison), and enables a partial unique index
--   to enforce one active opportunity per prompt+type at the DB level.
--
-- Safe to run multiple times (all statements are idempotent).
-- ============================================================================

-- 1. Add the PromptTrackingId column (nullable — existing rows have no FK yet)
ALTER TABLE "VisibilityOpportunities"
    ADD COLUMN IF NOT EXISTS "PromptTrackingId" INTEGER;

-- 2. Add the foreign key constraint (SET NULL on delete so removing a prompt
--    doesn't cascade-delete the opportunity history)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_visibility_opportunities_prompt_tracking'
          AND table_name = 'VisibilityOpportunities'
    ) THEN
        ALTER TABLE "VisibilityOpportunities"
            ADD CONSTRAINT "fk_visibility_opportunities_prompt_tracking"
            FOREIGN KEY ("PromptTrackingId")
            REFERENCES "PromptTrackings"("Id")
            ON DELETE SET NULL;
    END IF;
END $$;

-- 3. Index on PromptTrackingId for fast FK lookups and dedup queries
CREATE INDEX IF NOT EXISTS "idx_visibility_opportunities_prompt_tracking_id"
    ON "VisibilityOpportunities"("PromptTrackingId")
    WHERE "PromptTrackingId" IS NOT NULL;

-- 4. Composite index used by the dedup SELECT in OpportunityDetectionService
--    (ProjectId, OpportunityType, PromptTrackingId, Status) — covers both
--    existingLowVisIds and existingCompetitorIds queries exactly
CREATE INDEX IF NOT EXISTS "idx_visibility_opportunities_dedup"
    ON "VisibilityOpportunities"("ProjectId", "OpportunityType", "PromptTrackingId")
    WHERE "PromptTrackingId" IS NOT NULL
      AND "Status" IN ('New', 'InProgress');

-- 5. Back-fill PromptTrackingId for existing rows where PromptText matches
--    a PromptTracking record in the same project (best-effort, no error if
--    no match found — those rows keep PromptTrackingId = NULL and fall back
--    to text-based dedup in application code).
--    Must run BEFORE the unique index so duplicates are resolved first.
UPDATE "VisibilityOpportunities" vo
SET    "PromptTrackingId" = pt."Id"
FROM   "PromptTrackings" pt
WHERE  vo."ProjectId"        = pt."ProjectId"
  AND  vo."PromptText"       = pt."PromptText"
  AND  vo."PromptTrackingId" IS NULL;

-- 6. Remove duplicate active rows that were created before this migration,
--    keeping only the most recent one per (ProjectId, PromptTrackingId,
--    OpportunityType, Platform).
--    Must run BEFORE the unique index creation to avoid constraint violations.
DELETE FROM "VisibilityOpportunities"
WHERE "Id" IN (
    SELECT "Id"
    FROM (
        SELECT
            "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "ProjectId", "PromptTrackingId", "OpportunityType", "Platform"
                ORDER BY "DiscoveredAt" DESC
            ) AS rn
        FROM "VisibilityOpportunities"
        WHERE "PromptTrackingId" IS NOT NULL
          AND "Status" IN ('New', 'InProgress')
    ) ranked
    WHERE rn > 1
);

-- 7. Partial unique constraint: only one active (New/InProgress) opportunity
--    per (ProjectId, PromptTrackingId, OpportunityType, Platform).
--    Dismissed/Actioned records are excluded so history is preserved.
--    Created AFTER back-fill and dedup DELETE so no existing rows violate it.
CREATE UNIQUE INDEX IF NOT EXISTS "idx_visibility_opportunities_unique_prompt"
    ON "VisibilityOpportunities"("ProjectId", "PromptTrackingId", "OpportunityType", "Platform")
    WHERE "PromptTrackingId" IS NOT NULL
      AND "Status" IN ('New', 'InProgress');
