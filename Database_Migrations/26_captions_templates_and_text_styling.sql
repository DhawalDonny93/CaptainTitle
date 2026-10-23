-- ============================================================
-- Migration 26: Captain Title - two fixed templates (Street Glow, Doodle Marker) + granular text styling
-- Source: sync of old Captain Title commits 4ee2eea..c3dc0e6 (see docs/captain-title/OLD_SYNC_LOG.md)
--
-- WHAT IT DOES
--   1. Adds the project-wide styling columns to "CaptionProjects": script mode (Roman/Native), Outline, Shadow, Glow,
--      Background box, Bold, Italic (old ProjectEdit fields). Existing rows get the old defaults.
--   2. Adds the 6 new bundled caption fonts to "CaptionFonts" (the .ttf files ship in AppBackendBasic.API/Assets/captions/fonts).
--   3. Adds the two new templates "street_glow" and "doodle_marker" to "CaptionThemes".
--   4. Retires the 9 old templates (IsActive = FALSE; rows are kept, admins can re-activate them in Admin > Captions)
--      and makes "street_glow" the default template - ONLY if the default is still the factory value 'viral'.
--
-- PREREQUISITES: 22_generic_template_schema.sql, 21_captain_title_captions.sql (and 25_* if you use Razorpay) already applied.
-- RUN ORDER:     ... 21 -> 25 -> 26.   Run it BEFORE deploying the application build that contains this change.
-- IDEMPOTENT:    yes - ADD COLUMN IF NOT EXISTS, ON CONFLICT DO NOTHING; the UPDATE steps only touch factory-default state.
-- DESTRUCTIVE:   no data is deleted. Step 4 only flips IsActive on the 9 legacy templates (see ROLLBACK).
-- ROLLBACK:      ALTER TABLE "CaptionProjects" DROP COLUMN "ScriptMode", ... (list below);
--                UPDATE "CaptionThemes" SET "IsActive" = TRUE WHERE "Key" IN (<legacy keys>);
--                DELETE FROM "CaptionThemes" WHERE "Key" IN ('street_glow','doodle_marker');
--                UPDATE "SystemSetting" SET "Value" = 'viral' WHERE "Key" = 'captions.default_theme';
--
-- READ-ONLY CHECK (run first if unsure what state the database is in) - see the SELECTs at the very bottom of this file.
-- ============================================================

BEGIN;

-- 1. project-wide styling columns ---------------------------------------------------------------
ALTER TABLE "CaptionProjects"
    ADD COLUMN IF NOT EXISTS "ScriptMode"        VARCHAR(10)      NOT NULL DEFAULT 'roman',
    ADD COLUMN IF NOT EXISTS "OutlineEnabled"    BOOLEAN          NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS "OutlineWidth"      DOUBLE PRECISION NOT NULL DEFAULT 2,
    ADD COLUMN IF NOT EXISTS "OutlineColor"      VARCHAR(9)       NOT NULL DEFAULT '#000000',
    ADD COLUMN IF NOT EXISTS "ShadowEnabled"     BOOLEAN          NOT NULL DEFAULT TRUE,
    ADD COLUMN IF NOT EXISTS "ShadowAngle"       DOUBLE PRECISION NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ShadowDistance"    DOUBLE PRECISION NOT NULL DEFAULT 2,
    ADD COLUMN IF NOT EXISTS "ShadowBlur"        DOUBLE PRECISION NOT NULL DEFAULT 2,
    ADD COLUMN IF NOT EXISTS "ShadowOpacity"     DOUBLE PRECISION NOT NULL DEFAULT 0.5,
    ADD COLUMN IF NOT EXISTS "ShadowColor"       VARCHAR(9)       NOT NULL DEFAULT '#000000',
    ADD COLUMN IF NOT EXISTS "GlowEnabled"       BOOLEAN          NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS "GlowColor"         VARCHAR(9)       NULL,
    ADD COLUMN IF NOT EXISTS "GlowBlur"          DOUBLE PRECISION NOT NULL DEFAULT 8,
    ADD COLUMN IF NOT EXISTS "GlowOpacity"       DOUBLE PRECISION NOT NULL DEFAULT 0.6,
    ADD COLUMN IF NOT EXISTS "BackgroundEnabled" BOOLEAN          NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS "BackgroundColor"   VARCHAR(9)       NOT NULL DEFAULT '#000000',
    ADD COLUMN IF NOT EXISTS "BackgroundOpacity" DOUBLE PRECISION NOT NULL DEFAULT 0.6,
    ADD COLUMN IF NOT EXISTS "Bold"              BOOLEAN          NOT NULL DEFAULT TRUE,
    ADD COLUMN IF NOT EXISTS "Italic"            BOOLEAN          NOT NULL DEFAULT FALSE;

-- 2. new bundled fonts (Family = the font's real family name inside the .ttf, i.e. what libass resolves) ----
INSERT INTO "CaptionFonts" ("Key","Label","Family","IsActive","SortOrder") VALUES
  ('archivo','Archivo Black','Archivo Black',TRUE,5),
  ('space_grotesk','Space Grotesk','Space Grotesk',TRUE,6),
  ('baloo2','Baloo 2','Baloo 2 ExtraBold',TRUE,7),
  ('hind','Hind','Hind',TRUE,8),
  ('mukta','Mukta','Mukta ExtraBold',TRUE,9),
  ('kalam','Kalam','Kalam',TRUE,10)
ON CONFLICT ("Key") DO NOTHING;

-- 3. the two fixed templates --------------------------------------------------------------------
-- street_glow: line-based (headline Anton #FF2E2E 72px + subline Playfair italic #FFFFFF 44px); look and motion are built into the code.
-- doodle_marker: word-based tiers (all Caveat 56px; "key" = yellow highlighter pill, "feature" = red pencil loop).
INSERT INTO "CaptionThemes" ("Key","Name","BaseColor","AccentColor","AccentFont","TiersJson","CardJson","IsActive","SortOrder") VALUES
  ('street_glow','Street Glow','#FFFFFF','#FF2E2E','Anton',NULL,'{"stroke":null,"shadow":"0 0 14px rgba(255,46,46,.95)","glow":"0 0 28px rgba(255,46,46,.6)","highlight":false}'::jsonb,TRUE,0),
  ('doodle_marker','Doodle Marker',NULL,'#FACC15',NULL,'{"support":{"font":"Caveat","css_font":"''Caveat'', cursive","color":"#FFFFFF","size":56},"feature":{"font":"Caveat","css_font":"''Caveat'', cursive","color":"#FFFFFF","size":56},"key":{"font":"Caveat","css_font":"''Caveat'', cursive","color":"#111111","size":56}}'::jsonb,'{"stroke":null,"shadow":null,"glow":null,"highlight":true}'::jsonb,TRUE,1)
ON CONFLICT ("Key") DO NOTHING;

-- 4. retire the 9 old templates + switch the default (only while the default is still the factory 'viral') -----------
UPDATE "CaptionThemes" SET "IsActive" = FALSE
 WHERE "Key" IN ('viral','cinematic','minimal','podcast','meme_mix','street_pop','editorial_pop','clean_vogue','street_heat');

UPDATE "SystemSetting" SET "Value" = 'street_glow'
 WHERE "Key" = 'captions.default_theme' AND "Value" = 'viral';

COMMIT;

-- ============================================================
-- READ-ONLY CHECKS (safe to run any time, before or after)
-- ============================================================
-- new columns present? (expect 19 rows)
SELECT column_name FROM information_schema.columns
 WHERE table_name = 'CaptionProjects'
   AND column_name IN ('ScriptMode','OutlineEnabled','OutlineWidth','OutlineColor','ShadowEnabled','ShadowAngle','ShadowDistance','ShadowBlur',
                       'ShadowOpacity','ShadowColor','GlowEnabled','GlowColor','GlowBlur','GlowOpacity','BackgroundEnabled','BackgroundColor',
                       'BackgroundOpacity','Bold','Italic')
 ORDER BY column_name;
-- templates and their state
SELECT "Key","Name","IsActive","SortOrder" FROM "CaptionThemes" ORDER BY "IsActive" DESC, "SortOrder";
SELECT "Key","Value" FROM "SystemSetting" WHERE "Key" = 'captions.default_theme';
