-- ============================================================================
-- LLM ENHANCEMENTS MIGRATION SCRIPT
-- ============================================================================
-- Purpose: Add enhancements for LLM provider configuration and prompt templates
-- 
-- Changes:
-- 1. Ensure LlmJobConfig table exists with proper structure
-- 2. Ensure PromptTemplate table exists with proper structure
-- 3. Add indexes for performance
-- 4. Seed initial data
--
-- IMPORTANT: BACKUP YOUR DATABASE BEFORE RUNNING THIS SCRIPT!
-- ============================================================================

BEGIN;

-- ============================================================================
-- PHASE 1: Ensure LlmJobConfig Table Exists
-- ============================================================================
DELETE TABLE IF EXISTS "LlmJobConfigs";
CREATE TABLE IF NOT EXISTS "LlmJobConfigs" (
    "Id" SERIAL PRIMARY KEY,
    "JobKey" VARCHAR(100) NOT NULL UNIQUE,
    "JobName" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "Provider" VARCHAR(50) NOT NULL DEFAULT 'OpenAI',
    "Model" VARCHAR(100) NOT NULL,
    "MaxTokens" INTEGER NOT NULL DEFAULT 2000,
    "Temperature" DECIMAL(3,2) NOT NULL DEFAULT 0.7,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Category" VARCHAR(50) NOT NULL DEFAULT 'General',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for LlmJobConfigs
CREATE INDEX IF NOT EXISTS "IX_LlmJobConfigs_JobKey" ON "LlmJobConfigs" ("JobKey");
CREATE INDEX IF NOT EXISTS "IX_LlmJobConfigs_Category" ON "LlmJobConfigs" ("Category");
CREATE INDEX IF NOT EXISTS "IX_LlmJobConfigs_IsActive" ON "LlmJobConfigs" ("IsActive");

-- ============================================================================
-- PHASE 2: Ensure PromptTemplates Table Exists
-- ============================================================================

CREATE TABLE IF NOT EXISTS "PromptTemplates" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL UNIQUE,
    "Category" VARCHAR(100) NOT NULL,
    "Template" TEXT NOT NULL,
    "VariablesJson" TEXT,
    "Description" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "UsageCount" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for PromptTemplates
CREATE INDEX IF NOT EXISTS "IX_PromptTemplates_Name" ON "PromptTemplates" ("Name");
CREATE INDEX IF NOT EXISTS "IX_PromptTemplates_Category" ON "PromptTemplates" ("Category");
CREATE INDEX IF NOT EXISTS "IX_PromptTemplates_IsActive" ON "PromptTemplates" ("IsActive");

RAISE NOTICE 'PromptTemplates table ensured';

-- ============================================================================
-- PHASE 3: Seed LlmJobConfig Data (if empty)
-- ============================================================================

INSERT INTO "LlmJobConfigs" ("JobKey", "JobName", "Description", "Provider", "Model", "MaxTokens", "Temperature", "Category", "IsActive")
SELECT * FROM (VALUES
    ('AiTracking', 'AI Visibility Tracking', 'Track brand visibility in AI responses', 'OpenAI', 'gpt-4o', 2000, 0.7, 'Tracking', TRUE),
    ('PromptResearch', 'Prompt Research Generation', 'Generate research prompts for topics', 'OpenAI', 'gpt-4o', 2000, 0.7, 'Research', TRUE),
    ('ContentGapAnalysis', 'Content Gap Analysis', 'Analyze content gaps vs competitors', 'OpenAI', 'gpt-4-turbo-preview', 2000, 0.7, 'Analysis', TRUE),
    ('SentimentAnalysis', 'Sentiment Analysis', 'Analyze sentiment of AI responses', 'OpenAI', 'gpt-4o-mini', 1500, 0.5, 'Analysis', TRUE),
    ('CitationExtraction', 'Citation Extraction', 'Extract citations from AI responses', 'OpenAI', 'gpt-4o-mini', 1500, 0.3, 'Analysis', TRUE),
    ('OpportunityDetection', 'Opportunity Detection', 'Detect community opportunities', 'OpenAI', 'gpt-4o', 2000, 0.7, 'Research', TRUE),
    ('ContentGeneration', 'Content Generation', 'Generate optimized content', 'OpenAI', 'gpt-4o', 3000, 0.8, 'Content', TRUE),
    ('SiteAudit', 'Site Audit Analysis', 'Analyze site for LLM optimization', 'OpenAI', 'gpt-4o', 2000, 0.5, 'Analysis', TRUE)
) AS v("JobKey", "JobName", "Description", "Provider", "Model", "MaxTokens", "Temperature", "Category", "IsActive")
WHERE NOT EXISTS (SELECT 1 FROM "LlmJobConfigs" WHERE "JobKey" = v."JobKey");

RAISE NOTICE 'LlmJobConfigs seeded';

-- ============================================================================
-- PHASE 4: Seed PromptTemplate Data (if empty)
-- ============================================================================

-- Note: Full templates will be seeded by the application on startup
-- This just ensures the table structure is ready

INSERT INTO "PromptTemplates" ("Name", "Category", "Template", "Description", "VariablesJson", "IsActive", "UsageCount")
SELECT * FROM (VALUES
    (
        'PromptResearchGeneration',
        'Research',
        'You are an AI prompt research assistant. Generate a list of prompts that users might ask AI assistants about {TOPIC}.',
        'Generate research prompts for a given topic',
        '{"TOPIC": "string", "INDUSTRY": "string", "AUDIENCE": "string", "BUSINESS_TYPE": "string", "BRAND_NAME": "string"}',
        TRUE,
        0
    ),
    (
        'VisibilityTest',
        'Tracking',
        'Answer this question naturally as you would for any user: {PROMPT_TEXT}',
        'Test brand visibility in AI responses',
        '{"PROMPT_TEXT": "string", "BRAND_NAME": "string", "BRAND_DOMAIN": "string"}',
        TRUE,
        0
    ),
    (
        'ContentGapAnalysis',
        'Analysis',
        'Analyze content gaps for {BRAND_NAME} compared to competitors: {COMPETITOR_DOMAINS}',
        'Analyze content gaps and opportunities',
        '{"BRAND_NAME": "string", "INDUSTRY": "string", "COMPETITORS": "string", "CURRENT_CONTENT": "string"}',
        TRUE,
        0
    ),
    (
        'SentimentAnalysis',
        'Analysis',
        'Analyze the sentiment of this AI response about {BRAND_NAME}: {RESPONSE_TEXT}',
        'Analyze sentiment of AI responses',
        '{"RESPONSE_TEXT": "string", "BRAND_NAME": "string"}',
        TRUE,
        0
    ),
    (
        'CitationExtraction',
        'Analysis',
        'Extract all citations and sources from this text: {RESPONSE_TEXT}',
        'Extract citations from AI responses',
        '{"RESPONSE_TEXT": "string"}',
        TRUE,
        0
    )
) AS v("Name", "Category", "Template", "Description", "VariablesJson", "IsActive", "UsageCount")
WHERE NOT EXISTS (SELECT 1 FROM "PromptTemplates" WHERE "Name" = v."Name");

RAISE NOTICE 'PromptTemplates seeded';

-- ============================================================================
-- PHASE 5: Add Update Triggers for UpdatedAt
-- ============================================================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger for LlmJobConfigs
DROP TRIGGER IF EXISTS update_llmjobconfigs_updated_at ON "LlmJobConfigs";
CREATE TRIGGER update_llmjobconfigs_updated_at
    BEFORE UPDATE ON "LlmJobConfigs"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger for PromptTemplates
DROP TRIGGER IF EXISTS update_prompttemplates_updated_at ON "PromptTemplates";
CREATE TRIGGER update_prompttemplates_updated_at
    BEFORE UPDATE ON "PromptTemplates"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

RAISE NOTICE 'Update triggers created';

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- Verify LlmJobConfigs
SELECT 
    'LlmJobConfigs' as table_name,
    COUNT(*) as record_count,
    COUNT(DISTINCT "Category") as categories
FROM "LlmJobConfigs";

-- Verify PromptTemplates
SELECT 
    'PromptTemplates' as table_name,
    COUNT(*) as record_count,
    COUNT(DISTINCT "Category") as categories
FROM "PromptTemplates";

-- Show LlmJobConfigs by category
SELECT 
    "Category",
    COUNT(*) as job_count,
    STRING_AGG("JobKey", ', ') as jobs
FROM "LlmJobConfigs"
WHERE "IsActive" = TRUE
GROUP BY "Category"
ORDER BY "Category";

-- Show PromptTemplates by category
SELECT 
    "Category",
    COUNT(*) as template_count,
    STRING_AGG("Name", ', ') as templates
FROM "PromptTemplates"
WHERE "IsActive" = TRUE
GROUP BY "Category"
ORDER BY "Category";

COMMIT;

-- ============================================================================
-- NOTES
-- ============================================================================
-- 
-- LlmJobConfig Usage:
-- - Admin can configure which provider/model to use for each job type
-- - Changes take effect immediately via in-memory cache (30 min TTL)
-- - No code deployment needed to switch models
--
-- PromptTemplate Usage:
-- - Templates stored in DB for easy updates
-- - Cached in memory for performance (1 hour TTL)
-- - Falls back to hardcoded templates if not in DB
-- - Usage tracking for analytics
--
-- Performance:
-- - Both tables use memory caching for fast lookups
-- - Indexes on key columns for efficient queries
-- - Minimal database hits after initial load
--
-- ============================================================================
