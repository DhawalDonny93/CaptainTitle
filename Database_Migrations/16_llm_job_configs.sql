-- ============================================================
-- Migration 16: LLM Job Configurations
-- Adds LlmJobConfigs table so admins can control which
-- provider + model each background job / LLM process uses,
-- without redeploying the application.
-- Changes are picked up within 30 minutes via in-memory cache
-- (or immediately after calling POST /api/admin/llm-job-configs/invalidate-cache).
-- ============================================================

CREATE TABLE IF NOT EXISTS "LlmJobConfigs" (
    "Id"          SERIAL PRIMARY KEY,
    "JobKey"      VARCHAR(100) NOT NULL,   -- machine key used in code (RequestType)
    "JobName"     VARCHAR(200) NOT NULL,   -- human-readable label shown in Admin UI
    "Description" TEXT,
    "Provider"    VARCHAR(50)  NOT NULL DEFAULT 'OpenAI',  -- OpenAI | Anthropic | Google
    "Model"       VARCHAR(100) NOT NULL,
    "MaxTokens"   INTEGER      NOT NULL DEFAULT 2000,
    "Temperature" NUMERIC(4,2) NOT NULL DEFAULT 0.70,
    "IsActive"    BOOLEAN      NOT NULL DEFAULT TRUE,
    "Category"    VARCHAR(50)  NOT NULL DEFAULT 'General',
    "CreatedAt"   TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt"   TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Unique constraint: one config per job key
CREATE UNIQUE INDEX IF NOT EXISTS "idx_llm_job_configs_job_key"
    ON "LlmJobConfigs"("JobKey");

-- Index for admin listing by category
CREATE INDEX IF NOT EXISTS "idx_llm_job_configs_category"
    ON "LlmJobConfigs"("Category");

-- Index for active-only lookups (used by service)
CREATE INDEX IF NOT EXISTS "idx_llm_job_configs_active"
    ON "LlmJobConfigs"("IsActive");

-- ============================================================
-- Seed: default config for every LLM-calling job/process
-- These are the current hardcoded defaults extracted from code.
-- Admins can override any of these from the Admin UI.
-- ============================================================

INSERT INTO "LlmJobConfigs"
    ("JobKey", "JobName", "Description", "Provider", "Model", "MaxTokens", "Temperature", "IsActive", "Category")
VALUES
    -- ── Tracking ──────────────────────────────────────────────
    ('AiTracking',
     'AI Prompt Tracking',
     'Checks each active prompt against real LLM platforms to measure brand visibility and mention rate.',
     'OpenAI', 'gpt-4o', 1000, 0.70, TRUE, 'Tracking'),

    ('PromptTest',
     'Prompt Visibility Test',
     'On-demand test of a single prompt across selected platforms (ChatGPT, Claude, Gemini, Perplexity).',
     'OpenAI', 'gpt-4o', 1000, 0.70, TRUE, 'Tracking'),

    -- ── Research ──────────────────────────────────────────────
    ('PromptResearchGeneration',
     'Prompt Research Generation',
     'Uses LLM to generate a list of relevant prompts for a given topic and industry.',
     'OpenAI', 'gpt-4-turbo-preview', 2000, 0.70, TRUE, 'Research'),

    -- ── Analysis ──────────────────────────────────────────────
    ('SentimentAnalysis',
     'Sentiment Analysis',
     'Analyses the sentiment of a brand mention context (Positive / Neutral / Negative).',
     'Anthropic', 'claude-3-5-sonnet-20241022', 500, 0.30, TRUE, 'Analysis'),

    ('ContentGapAnalysis',
     'Content Gap Analysis',
     'Detects content topics that competitors cover but the brand does not.',
     'OpenAI', 'gpt-4o', 2000, 0.50, TRUE, 'Analysis'),

    ('CompetitorAnalysis',
     'Competitor Analysis',
     'Analyses a competitor domain for visibility score, strengths, and weaknesses.',
     'OpenAI', 'gpt-4o', 2000, 0.50, TRUE, 'Analysis'),

    -- ── Content ───────────────────────────────────────────────
    ('ContentGeneration',
     'Content Article Generation',
     'Generates a full SEO-optimised article targeting specific prompts and platforms.',
     'OpenAI', 'gpt-4o', 4000, 0.70, TRUE, 'Content'),

    ('DraftReply',
     'Community Draft Reply',
     'Generates a helpful, authentic draft reply for a Reddit / Quora community opportunity.',
     'OpenAI', 'gpt-4o', 500, 0.80, TRUE, 'Content'),

    -- ── Opportunities ─────────────────────────────────────────
    ('OpportunityDetection',
     'Opportunity Detection',
     'Scans visibility metrics and site audit results to detect new SEO / visibility opportunities.',
     'OpenAI', 'gpt-4o-mini', 1500, 0.50, TRUE, 'Opportunities'),

    ('SeoOpportunityAnalysis',
     'SEO Opportunity Analysis',
     'Generates actionable SEO recommendations from site audit findings.',
     'OpenAI', 'gpt-4o-mini', 1500, 0.50, TRUE, 'Opportunities')

ON CONFLICT ("JobKey") DO NOTHING;
