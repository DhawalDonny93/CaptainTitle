-- ============================================================================
-- Migration 25: Razorpay payments + export render settings (Captain Title)
-- ----------------------------------------------------------------------------
-- Run AFTER 21_captain_title_captions.sql. Idempotent and safe to re-run (ADD COLUMN IF NOT EXISTS, ON CONFLICT DO NOTHING).
-- Runs in one transaction. Existing rows keep working: every existing payment is a Stripe payment ('stripe' default).
--
--   * "CaptionPlans"."RazorpayPlanId"               - Razorpay plan (plan_xxx) for subscription plans paid in INR
--   * "CaptionPaymentTransactions"."Provider"       - 'stripe' | 'razorpay' (SessionId holds the provider's session/order/subscription id)
--   * "CaptionPaymentTransactions"."ProviderPaymentId" - Razorpay payment id (pay_xxx) once known
--   * SystemSetting captions.render_* - x264 preset/CRF for exports. DEFAULTS ARE CHEAPER than the old app (which used
--     slow/CRF 17 and veryfast/CRF 26): 'High' = veryfast/18, 'Draft' = ultrafast/28. Edit them in Admin > Captain Title > Limits.
--
-- No rollback needed; to undo: ALTER TABLE ... DROP COLUMN IF EXISTS for the three columns and DELETE the four settings.
-- ============================================================================
BEGIN;

ALTER TABLE "CaptionPlans"
    ADD COLUMN IF NOT EXISTS "RazorpayPlanId" VARCHAR(100) NULL;

ALTER TABLE "CaptionPaymentTransactions"
    ADD COLUMN IF NOT EXISTS "Provider" VARCHAR(20) NOT NULL DEFAULT 'stripe';

ALTER TABLE "CaptionPaymentTransactions"
    ADD COLUMN IF NOT EXISTS "ProviderPaymentId" VARCHAR(255) NULL;

INSERT INTO "SystemSetting" ("Key","Value","Type","Category","Description","IsPublic") VALUES
  ('captions.render_high_preset','veryfast','string','captions','x264 preset for ''High quality'' exports (slower = smaller/better but much more CPU; the old app used ''slow'')',FALSE),
  ('captions.render_high_crf','18','int','captions','x264 CRF for ''High quality'' exports (lower = better; the old app used 17)',FALSE),
  ('captions.render_draft_preset','ultrafast','string','captions','x264 preset for ''Draft'' exports (the old app used ''veryfast'')',FALSE),
  ('captions.render_draft_crf','28','int','captions','x264 CRF for ''Draft'' exports (the old app used 26)',FALSE)
ON CONFLICT ("Key") DO NOTHING;

COMMIT;
