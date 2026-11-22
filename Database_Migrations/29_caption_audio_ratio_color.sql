-- ============================================================
-- Migration 29: Captain Title - studio Audio, Ratio and Color panels
--
-- WHAT IT DOES
--   Adds to "CaptionProjects" (all with defaults that reproduce the export as it was before these panels):
--     Color panel
--       "Warmth"          INTEGER  -50 (cool) .. 50 (warm), 0 = neutral
--       "Tint"            INTEGER  -50 (green) .. 50 (magenta), 0 = neutral
--     Audio panel
--       "AudioFadeIn"     DOUBLE   seconds of fade-in of the original audio, 0 = none
--       "AudioFadeOut"    DOUBLE   seconds of fade-out of the original audio, 0 = none
--       "AudioNormalize"  BOOLEAN  loudness-normalize the original audio (-14 LUFS)
--     Ratio panel
--       "AspectRatio"     VARCHAR  'original' | '9:16' | '1:1' | '4:5' | '16:9'
--       "FrameFit"        VARCHAR  'blur' | 'color' | 'crop' - how the source fills a different frame
--       "FrameColor"      VARCHAR  '#RRGGBB' background for FrameFit = 'color'
--
-- PREREQUISITES: 22 -> 21 -> 25 -> 26 -> 27 (-> 28) already applied.
-- RUN ORDER:     ... 27 -> 28 -> 29.  Run it BEFORE deploying the application build that contains these panels
--                (that build reads and writes these columns; the previous build keeps working with them in place).
-- IDEMPOTENT:    yes - ADD COLUMN IF NOT EXISTS; safe to run again.
-- DESTRUCTIVE:   no. Nothing is dropped, deleted or rewritten. Existing projects get the neutral defaults.
-- LOCKS:         a short lock on "CaptionProjects" (adding columns with constant defaults does not rewrite the table on Postgres 11+).
-- ROLLBACK:      deploy the previous build first, then:
--                  ALTER TABLE "CaptionProjects"
--                    DROP COLUMN IF EXISTS "Warmth", DROP COLUMN IF EXISTS "Tint",
--                    DROP COLUMN IF EXISTS "AudioFadeIn", DROP COLUMN IF EXISTS "AudioFadeOut", DROP COLUMN IF EXISTS "AudioNormalize",
--                    DROP COLUMN IF EXISTS "AspectRatio", DROP COLUMN IF EXISTS "FrameFit", DROP COLUMN IF EXISTS "FrameColor";
--                Back up first: dropping them loses the users' Audio / Ratio / Color settings.
--
-- READ-ONLY CHECK: the SELECT at the bottom of this file (safe to run any time, before or after).
-- ============================================================

BEGIN;

ALTER TABLE "CaptionProjects"
    ADD COLUMN IF NOT EXISTS "Warmth"         INTEGER          NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "Tint"           INTEGER          NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "AudioFadeIn"    DOUBLE PRECISION NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "AudioFadeOut"   DOUBLE PRECISION NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "AudioNormalize" BOOLEAN          NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS "AspectRatio"    VARCHAR(10)      NOT NULL DEFAULT 'original',
    ADD COLUMN IF NOT EXISTS "FrameFit"       VARCHAR(10)      NOT NULL DEFAULT 'blur',
    ADD COLUMN IF NOT EXISTS "FrameColor"     VARCHAR(9)       NOT NULL DEFAULT '#000000';

COMMIT;

-- ---------------------------------------------------------------------------------------------------
-- READ-ONLY CHECK: expect 8 rows, one per column above, with the listed type and default.
-- ---------------------------------------------------------------------------------------------------
-- SELECT column_name, data_type, column_default, is_nullable
--   FROM information_schema.columns
--  WHERE table_name = 'CaptionProjects'
--    AND column_name IN ('Warmth','Tint','AudioFadeIn','AudioFadeOut','AudioNormalize','AspectRatio','FrameFit','FrameColor')
--  ORDER BY column_name;
