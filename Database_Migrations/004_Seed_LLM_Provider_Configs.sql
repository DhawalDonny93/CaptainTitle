-- ============================================================================
-- SEED LLM PROVIDER CONFIGURATIONS
-- Date: 2026-03-29
-- Description: Seed initial LLM providers and their configurations
-- ============================================================================

BEGIN;

-- ============================================
-- 1. Insert LLM Providers
-- ============================================

-- Insert OpenAI provider
INSERT INTO "LlmProvider" ("Name", "DisplayName", "ApiEndpoint", "IsActive", "Priority", "MaxTokens", "CostPer1kInputTokens", "CostPer1kOutputTokens", "RateLimitPerMinute", "CreatedAt", "UpdatedAt")
VALUES 
    ('OpenAI', 'OpenAI (ChatGPT)', 'https://api.openai.com/v1', TRUE, 100, 4000, 0.0015, 0.002, 60, NOW(), NOW())
ON CONFLICT ("Name") DO UPDATE SET
    "DisplayName" = EXCLUDED."DisplayName",
    "ApiEndpoint" = EXCLUDED."ApiEndpoint",
    "IsActive" = EXCLUDED."IsActive",
    "Priority" = EXCLUDED."Priority",
    "MaxTokens" = EXCLUDED."MaxTokens",
    "CostPer1kInputTokens" = EXCLUDED."CostPer1kInputTokens",
    "CostPer1kOutputTokens" = EXCLUDED."CostPer1kOutputTokens",
    "RateLimitPerMinute" = EXCLUDED."RateLimitPerMinute",
    "UpdatedAt" = NOW();

-- Insert Anthropic provider
INSERT INTO "LlmProvider" ("Name", "DisplayName", "ApiEndpoint", "IsActive", "Priority", "MaxTokens", "CostPer1kInputTokens", "CostPer1kOutputTokens", "RateLimitPerMinute", "CreatedAt", "UpdatedAt")
VALUES 
    ('Anthropic', 'Anthropic (Claude)', 'https://api.anthropic.com/v1', TRUE, 90, 4000, 0.003, 0.015, 50, NOW(), NOW())
ON CONFLICT ("Name") DO UPDATE SET
    "DisplayName" = EXCLUDED."DisplayName",
    "ApiEndpoint" = EXCLUDED."ApiEndpoint",
    "IsActive" = EXCLUDED."IsActive",
    "Priority" = EXCLUDED."Priority",
    "MaxTokens" = EXCLUDED."MaxTokens",
    "CostPer1kInputTokens" = EXCLUDED."CostPer1kInputTokens",
    "CostPer1kOutputTokens" = EXCLUDED."CostPer1kOutputTokens",
    "RateLimitPerMinute" = EXCLUDED."RateLimitPerMinute",
    "UpdatedAt" = NOW();

-- Insert Google provider
INSERT INTO "LlmProvider" ("Name", "DisplayName", "ApiEndpoint", "IsActive", "Priority", "MaxTokens", "CostPer1kInputTokens", "CostPer1kOutputTokens", "RateLimitPerMinute", "CreatedAt", "UpdatedAt")
VALUES 
    ('Google', 'Google (Gemini)', 'https://generativelanguage.googleapis.com/v1beta', TRUE, 80, 2048, 0.00025, 0.0005, 60, NOW(), NOW())
ON CONFLICT ("Name") DO UPDATE SET
    "DisplayName" = EXCLUDED."DisplayName",
    "ApiEndpoint" = EXCLUDED."ApiEndpoint",
    "IsActive" = EXCLUDED."IsActive",
    "Priority" = EXCLUDED."Priority",
    "MaxTokens" = EXCLUDED."MaxTokens",
    "CostPer1kInputTokens" = EXCLUDED."CostPer1kInputTokens",
    "CostPer1kOutputTokens" = EXCLUDED."CostPer1kOutputTokens",
    "RateLimitPerMinute" = EXCLUDED."RateLimitPerMinute",
    "UpdatedAt" = NOW();

-- ============================================
-- 2. Insert Default Configurations (Placeholders)
-- ============================================

-- Note: API keys should be added via Admin UI or manually updated
-- These are just placeholder configurations

-- OpenAI configs
INSERT INTO "LlmProviderConfig" ("ProviderId", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'ApiKey',
    NULL, -- Will be set via Admin UI
    TRUE,
    NOW(),
    NOW()
FROM "LlmProvider" p
WHERE p."Name" = 'OpenAI'
ON CONFLICT ("ProviderId", "ConfigKey") DO NOTHING;

INSERT INTO "LlmProviderConfig" ("ProviderId", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'Endpoint',
    'https://api.openai.com/v1',
    FALSE,
    NOW(),
    NOW()
FROM "LlmProvider" p
WHERE p."Name" = 'OpenAI'
ON CONFLICT ("ProviderId", "ConfigKey") DO NOTHING;

-- Anthropic configs
INSERT INTO "LlmProviderConfig" ("ProviderId", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'ApiKey',
    NULL, -- Will be set via Admin UI
    TRUE,
    NOW(),
    NOW()
FROM "LlmProvider" p
WHERE p."Name" = 'Anthropic'
ON CONFLICT ("ProviderId", "ConfigKey") DO NOTHING;

INSERT INTO "LlmProviderConfig" ("ProviderId", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'Endpoint',
    'https://api.anthropic.com/v1',
    FALSE,
    NOW(),
    NOW()
FROM "LlmProvider" p
WHERE p."Name" = 'Anthropic'
ON CONFLICT ("ProviderId", "ConfigKey") DO NOTHING;

-- Google configs
INSERT INTO "LlmProviderConfig" ("ProviderId", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'ApiKey',
    NULL, -- Will be set via Admin UI
    TRUE,
    NOW(),
    NOW()
FROM "LlmProvider" p
WHERE p."Name" = 'Google'
ON CONFLICT ("ProviderId", "ConfigKey") DO NOTHING;

INSERT INTO "LlmProviderConfig" ("ProviderId", "ConfigKey", "ConfigValue", "IsEncrypted", "CreatedAt", "UpdatedAt")
SELECT 
    p."Id",
    'Endpoint',
    'https://generativelanguage.googleapis.com/v1beta',
    FALSE,
    NOW(),
    NOW()
FROM "LlmProvider" p
WHERE p."Name" = 'Google'
ON CONFLICT ("ProviderId", "ConfigKey") DO NOTHING;

COMMIT;

-- ============================================
-- Verification Queries
-- ============================================

-- Check providers
SELECT 
    "Id",
    "Name",
    "DisplayName",
    "IsActive",
    "Priority",
    "ApiEndpoint"
FROM "LlmProvider"
ORDER BY "Priority" DESC;

-- Check configurations
SELECT 
    p."Name" AS "Provider",
    c."ConfigKey",
    CASE 
        WHEN c."IsEncrypted" THEN '[ENCRYPTED]'
        ELSE c."ConfigValue"
    END AS "ConfigValue",
    c."IsEncrypted"
FROM "LlmProviderConfig" c
JOIN "LlmProvider" p ON c."ProviderId" = p."Id"
ORDER BY p."Name", c."ConfigKey";

-- ============================================
-- IMPORTANT: Set API Keys via Admin UI
-- ============================================

-- After running this migration, you MUST set API keys via:
-- 1. Admin UI: Navigate to Settings → LLM Providers
-- 2. Or manually update using encrypted values:
--    UPDATE "LlmProviderConfig" 
--    SET "ConfigValue" = '[ENCRYPTED_VALUE]', "IsEncrypted" = TRUE
--    WHERE "ProviderId" = (SELECT "Id" FROM "LlmProvider" WHERE "Name" = 'OpenAI')
--    AND "ConfigKey" = 'ApiKey';
