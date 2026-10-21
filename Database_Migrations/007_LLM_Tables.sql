-- =============================================
-- LLM (ChatGPT) Integration Tables
-- =============================================

-- Table: LlmConversations
-- Stores conversation metadata
CREATE TABLE IF NOT EXISTS "LlmConversations" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "Title" VARCHAR(100) NOT NULL DEFAULT 'New Conversation',
    "Model" VARCHAR(20) NOT NULL DEFAULT 'gpt-4o',
    "TotalMessages" INTEGER NOT NULL DEFAULT 0,
    "TotalTokensUsed" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    
    CONSTRAINT "FK_LlmConversations_Users" 
        FOREIGN KEY ("UserId") 
        REFERENCES "Users"("Id") 
        ON DELETE CASCADE
);

-- Table: LlmMessages
-- Stores individual messages in conversations
CREATE TABLE IF NOT EXISTS "LlmMessages" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ConversationId" UUID NOT NULL,
    "Role" VARCHAR(20) NOT NULL, -- 'user', 'assistant', 'system'
    "Content" TEXT NOT NULL,
    "ImageUrl" TEXT NULL,
    "ImageBase64" TEXT NULL,
    "PromptTokens" INTEGER NULL,
    "CompletionTokens" INTEGER NULL,
    "TotalTokens" INTEGER NULL,
    "FinishReason" VARCHAR(50) NULL,
    "ResponseTimeMs" DOUBLE PRECISION NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_LlmMessages_Conversations" 
        FOREIGN KEY ("ConversationId") 
        REFERENCES "LlmConversations"("Id") 
        ON DELETE CASCADE
);

-- Table: LlmUsageQuotas
-- Tracks usage limits and rate limiting per user
CREATE TABLE IF NOT EXISTS "LlmUsageQuotas" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "PeriodStart" TIMESTAMP NOT NULL DEFAULT NOW(),
    "PeriodEnd" TIMESTAMP NOT NULL DEFAULT NOW() + INTERVAL '30 days',
    "RequestsUsed" INTEGER NOT NULL DEFAULT 0,
    "RequestsLimit" INTEGER NOT NULL DEFAULT 100,
    "TokensUsed" INTEGER NOT NULL DEFAULT 0,
    "TokensLimit" INTEGER NOT NULL DEFAULT 100000,
    "ImagesUsed" INTEGER NOT NULL DEFAULT 0,
    "ImagesLimit" INTEGER NOT NULL DEFAULT 50,
    "RequestsPerMinute" INTEGER NOT NULL DEFAULT 10,
    "RequestsPerHour" INTEGER NOT NULL DEFAULT 100,
    "RequestsPerDay" INTEGER NOT NULL DEFAULT 500,
    "LastRequestAt" TIMESTAMP NULL,
    "ConsecutiveRequests" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_LlmUsageQuotas_Users" 
        FOREIGN KEY ("UserId") 
        REFERENCES "Users"("Id") 
        ON DELETE CASCADE,
    
    CONSTRAINT "UQ_LlmUsageQuotas_UserPeriod" 
        UNIQUE ("UserId", "PeriodStart")
);

-- =============================================
-- Indexes for Performance
-- =============================================

-- LlmConversations indexes
CREATE INDEX IF NOT EXISTS "IX_LlmConversations_UserId" 
    ON "LlmConversations"("UserId");

CREATE INDEX IF NOT EXISTS "IX_LlmConversations_UpdatedAt" 
    ON "LlmConversations"("UpdatedAt" DESC);

CREATE INDEX IF NOT EXISTS "IX_LlmConversations_UserId_IsDeleted" 
    ON "LlmConversations"("UserId", "IsDeleted");

-- LlmMessages indexes
CREATE INDEX IF NOT EXISTS "IX_LlmMessages_ConversationId" 
    ON "LlmMessages"("ConversationId");

CREATE INDEX IF NOT EXISTS "IX_LlmMessages_CreatedAt" 
    ON "LlmMessages"("CreatedAt");

CREATE INDEX IF NOT EXISTS "IX_LlmMessages_ConversationId_CreatedAt" 
    ON "LlmMessages"("ConversationId", "CreatedAt");

-- LlmUsageQuotas indexes
CREATE INDEX IF NOT EXISTS "IX_LlmUsageQuotas_UserId" 
    ON "LlmUsageQuotas"("UserId");

CREATE INDEX IF NOT EXISTS "IX_LlmUsageQuotas_PeriodEnd" 
    ON "LlmUsageQuotas"("PeriodEnd");

CREATE INDEX IF NOT EXISTS "IX_LlmUsageQuotas_UserId_PeriodEnd" 
    ON "LlmUsageQuotas"("UserId", "PeriodEnd");

-- =============================================
-- Comments for Documentation
-- =============================================

COMMENT ON TABLE "LlmConversations" IS 'Stores ChatGPT conversation metadata';
COMMENT ON TABLE "LlmMessages" IS 'Stores individual messages in ChatGPT conversations';
COMMENT ON TABLE "LlmUsageQuotas" IS 'Tracks LLM usage limits and rate limiting per user';

COMMENT ON COLUMN "LlmConversations"."Model" IS 'OpenAI model used: gpt-4o, gpt-4-turbo, gpt-3.5-turbo';
COMMENT ON COLUMN "LlmMessages"."Role" IS 'Message role: user, assistant, or system';
COMMENT ON COLUMN "LlmMessages"."ImageBase64" IS 'Base64 encoded image for vision models';
COMMENT ON COLUMN "LlmUsageQuotas"."RequestsLimit" IS 'Max requests per period (based on subscription)';
COMMENT ON COLUMN "LlmUsageQuotas"."TokensLimit" IS 'Max tokens per period (based on subscription)';
COMMENT ON COLUMN "LlmUsageQuotas"."RequestsPerMinute" IS 'Rate limit: requests per minute';

-- =============================================
-- Sample Data for Testing (Optional)
-- =============================================

-- You can insert test data here if needed
-- Example:
-- INSERT INTO "LlmUsageQuotas" ("UserId", "RequestsLimit", "TokensLimit", "ImagesLimit")
-- VALUES ('your-user-id', 100, 100000, 50);
