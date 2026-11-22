-- ============================================================
-- Migration 30: Captain Title - Export dialog options (resolution + format)
--
-- WHAT IT DOES
--   Adds to "CaptionProjects" (defaults reproduce the export as it was before the new Export dialog):
--     "RenderResolution"  VARCHAR(10)  'original' | '1080' | '720' | '480'  - short side of the exported frame; 'original' keeps the frame size
--     "RenderFormat"      VARCHAR(10)  'mp4' | 'mov'                        - container of the export (H.264 + AAC in both)
--
-- PREREQUISITES: 29_caption_audio_ratio_color.sql already applied.
-- RUN ORDER:     ... 28 -> 29 -> 30.  Run it BEFORE deploying the application build that contains the new Export dialog
--                (that build reads and writes these columns; the previous build keeps working with them in place).
-- IDEMPOTENT:    yes - ADD COLUMN IF NOT EXISTS; safe to run again.
-- DESTRUCTIVE:   no. Nothing is dropped, deleted or rewritten. Existing projects get 'original' / 'mp4' (same export as before).
-- LOCKS:         a short lock on "CaptionProjects" (constant defaults do not rewrite the table on Postgres 11+).
-- ROLLBACK:      deploy the previous build first, then:
--                  ALTER TABLE "CaptionProjects" DROP COLUMN IF EXISTS "RenderResolution", DROP COLUMN IF EXISTS "RenderFormat";
--                Back up first: dropping them loses the users' export choices.
--
-- READ-ONLY CHECK: the SELECT at the bottom of this file (safe to run any time, before or after).
-- ============================================================

BEGIN;

ALTER TABLE "CaptionProjects"
    ADD COLUMN IF NOT EXISTS "RenderResolution" VARCHAR(10) NOT NULL DEFAULT 'original',
    ADD COLUMN IF NOT EXISTS "RenderFormat"     VARCHAR(10) NOT NULL DEFAULT 'mp4';

COMMIT;

-- ---------------------------------------------------------------------------------------------------
-- READ-ONLY CHECK: expect 2 rows, one per column above, with the listed type and default.
-- ---------------------------------------------------------------------------------------------------
-- SELECT column_name, data_type, character_maximum_length, column_default, is_nullable
--   FROM information_schema.columns
--  WHERE table_name = 'CaptionProjects'
--    AND column_name IN ('RenderResolution','RenderFormat')
--  ORDER BY column_name;
