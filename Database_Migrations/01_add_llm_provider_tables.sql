-- Migration: Add LLM Provider Tables
-- Date: 2026-02-07
-- Description: Creates tables for LLM provider management, request tracking, and cost monitoring

-- ============================================
-- 1. LlmProvider Table
-- ============================================
CREATE TABLE "LlmProvider" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "ApiEndpoint" VARCHAR(500),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "MaxTokens" INTEGER NOT NULL DEFAULT 4000,
    "CostPer1kInputTokens" DECIMAL(10,6),
    "CostPer1kOutputTokens" DECIMAL(10,6),
    "RateLimitPerMinute" INTEGER NOT NULL DEFAULT 60,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for LlmProvider
CREATE INDEX "IX_LlmProvider_IsActive" ON "LlmProvider"("IsActive");
CREATE INDEX "IX_LlmProvider_Priority" ON "LlmProvider"("Priority");

-- ============================================
-- 2. LlmProviderConfig Table
-- ============================================
CREATE TABLE "LlmProviderConfig" (
    "Id" SERIAL PRIMARY KEY,
    "ProviderId" INTEGER NOT NULL,
    "ConfigKey" VARCHAR(100) NOT NULL,
    "ConfigValue" TEXT,
    "IsEncrypted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmProviderConfig_Provider" FOREIGN KEY ("ProviderId") 
        REFERENCES "LlmProvider"("Id") ON DELETE CASCADE
);

-- Indexes for LlmProviderConfig
CREATE INDEX "IX_LlmProviderConfig_ProviderId" ON "LlmProviderConfig"("ProviderId");
CREATE UNIQUE INDEX "IX_LlmProviderConfig_Unique" ON "LlmProviderConfig"("ProviderId", "ConfigKey");

-- ============================================
-- 3. LlmRequest Table
-- ============================================
CREATE TABLE "LlmRequest" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER,
    "ProjectId" INTEGER,
    "ProviderId" INTEGER NOT NULL,
    "Model" VARCHAR(100) NOT NULL,
    "RequestType" VARCHAR(50) NOT NULL,
    "PromptText" TEXT NOT NULL,
    "SystemPrompt" TEXT,
    "Temperature" DECIMAL(3,2) NOT NULL DEFAULT 0.7,
    "MaxTokens" INTEGER NOT NULL DEFAULT 2000,
    "RequestTokens" INTEGER,
    "ResponseTokens" INTEGER,
    "TotalTokens" INTEGER,
    "Cost" DECIMAL(10,6),
    "ResponseText" TEXT,
    "ResponseJson" JSONB,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "ErrorMessage" TEXT,
    "LatencyMs" INTEGER,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmRequest_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_LlmRequest_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_LlmRequest_Provider" FOREIGN KEY ("ProviderId") 
        REFERENCES "LlmProvider"("Id") ON DELETE RESTRICT
);

-- Indexes for LlmRequest
CREATE INDEX "IX_LlmRequest_UserId" ON "LlmRequest"("UserId");
CREATE INDEX "IX_LlmRequest_ProjectId" ON "LlmRequest"("ProjectId");
CREATE INDEX "IX_LlmRequest_ProviderId" ON "LlmRequest"("ProviderId");
CREATE INDEX "IX_LlmRequest_Status" ON "LlmRequest"("Status");
CREATE INDEX "IX_LlmRequest_CreatedAt" ON "LlmRequest"("CreatedAt");
CREATE INDEX "IX_LlmRequest_RequestType" ON "LlmRequest"("RequestType");

-- ============================================
-- 4. LlmCostTracking Table
-- ============================================
CREATE TABLE "LlmCostTracking" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER,
    "ProjectId" INTEGER,
    "ProviderId" INTEGER NOT NULL,
    "TrackingDate" DATE NOT NULL,
    "TotalRequests" INTEGER NOT NULL DEFAULT 0,
    "SuccessfulRequests" INTEGER NOT NULL DEFAULT 0,
    "FailedRequests" INTEGER NOT NULL DEFAULT 0,
    "TotalInputTokens" BIGINT NOT NULL DEFAULT 0,
    "TotalOutputTokens" BIGINT NOT NULL DEFAULT 0,
    "TotalCost" DECIMAL(10,2) NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmCostTracking_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LlmCostTracking_Project" FOREIGN KEY ("ProjectId") 
        REFERENCES "Project"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LlmCostTracking_Provider" FOREIGN KEY ("ProviderId") 
        REFERENCES "LlmProvider"("Id") ON DELETE RESTRICT
);

-- Indexes for LlmCostTracking
CREATE INDEX "IX_LlmCostTracking_UserId" ON "LlmCostTracking"("UserId");
CREATE INDEX "IX_LlmCostTracking_ProjectId" ON "LlmCostTracking"("ProjectId");
CREATE INDEX "IX_LlmCostTracking_TrackingDate" ON "LlmCostTracking"("TrackingDate");
CREATE UNIQUE INDEX "IX_LlmCostTracking_Unique" ON "LlmCostTracking"("UserId", "ProjectId", "ProviderId", "TrackingDate");

-- ============================================
-- 5. Seed Initial Provider Data
-- ============================================

-- Insert OpenAI Provider
INSERT INTO "LlmProvider" ("Name", "DisplayName", "ApiEndpoint", "IsActive", "Priority", "MaxTokens", "CostPer1kInputTokens", "CostPer1kOutputTokens", "RateLimitPerMinute")
VALUES ('OpenAI', 'OpenAI (GPT-4 / GPT-3.5)', 'https://api.openai.com/v1', TRUE, 100, 8000, 0.03, 0.06, 500);

-- Insert Anthropic Provider
INSERT INTO "LlmProvider" ("Name", "DisplayName", "ApiEndpoint", "IsActive", "Priority", "MaxTokens", "CostPer1kInputTokens", "CostPer1kOutputTokens", "RateLimitPerMinute")
VALUES ('Anthropic', 'Anthropic (Claude)', 'https://api.anthropic.com/v1', TRUE, 90, 4000, 0.015, 0.075, 400);

-- Insert Google Provider
INSERT INTO "LlmProvider" ("Name", "DisplayName", "ApiEndpoint", "IsActive", "Priority", "MaxTokens", "CostPer1kInputTokens", "CostPer1kOutputTokens", "RateLimitPerMinute")
VALUES ('Google', 'Google (Gemini)', 'https://generativelanguage.googleapis.com/v1beta', TRUE, 80, 2048, 0.00025, 0.0005, 600);

-- ============================================
-- Migration Complete
-- ============================================
-- Tables Created: 4
-- Indexes Created: 13
-- Foreign Keys: 8
-- Seed Records: 3 providers
