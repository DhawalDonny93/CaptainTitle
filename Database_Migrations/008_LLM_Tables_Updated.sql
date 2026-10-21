-- =============================================
-- LLM (ChatGPT) Integration Tables - Updated Schema
-- Backend-Controlled Prompts Architecture
-- =============================================

-- Table: LlmPromptTemplates
-- Stores admin-configured prompt templates
CREATE TABLE IF NOT EXISTS "LlmPromptTemplates" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL DEFAULT '',
    "SystemPrompt" TEXT NOT NULL,
    "UserPromptTemplate" TEXT NULL,
    "ResponseFormat" VARCHAR(20) NOT NULL DEFAULT 'json',
    "ResponseSchema" TEXT NULL,
    "Model" VARCHAR(50) NOT NULL DEFAULT 'gpt-4o',
    "Temperature" DOUBLE PRECISION NOT NULL DEFAULT 0.7,
    "MaxTokens" INTEGER NOT NULL DEFAULT 1000,
    "RequiresImage" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Category" VARCHAR(50) NOT NULL DEFAULT 'general',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "CreatedByUserId" UUID NULL,
    
    CONSTRAINT "FK_LlmPromptTemplates_Users" 
        FOREIGN KEY ("CreatedByUserId") 
        REFERENCES "Users"("Id") 
        ON DELETE SET NULL
);

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

-- Table: LlmMessages (Updated Schema)
-- Stores individual messages with parsed responses
CREATE TABLE IF NOT EXISTS "LlmMessages" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ConversationId" UUID NOT NULL,
    "Role" VARCHAR(20) NOT NULL,
    "RawContent" TEXT NULL,
    "ParsedContent" TEXT NOT NULL,
    "PromptTemplateId" UUID NULL,
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
        ON DELETE CASCADE,
    
    CONSTRAINT "FK_LlmMessages_PromptTemplates" 
        FOREIGN KEY ("PromptTemplateId") 
        REFERENCES "LlmPromptTemplates"("Id") 
        ON DELETE SET NULL
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

-- LlmPromptTemplates indexes
CREATE INDEX IF NOT EXISTS "IX_LlmPromptTemplates_Category" 
    ON "LlmPromptTemplates"("Category");

CREATE INDEX IF NOT EXISTS "IX_LlmPromptTemplates_IsActive" 
    ON "LlmPromptTemplates"("IsActive");

CREATE INDEX IF NOT EXISTS "IX_LlmPromptTemplates_Category_IsActive" 
    ON "LlmPromptTemplates"("Category", "IsActive");

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

CREATE INDEX IF NOT EXISTS "IX_LlmMessages_PromptTemplateId" 
    ON "LlmMessages"("PromptTemplateId");

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

COMMENT ON TABLE "LlmPromptTemplates" IS 'Admin-configured prompt templates for LLM interactions';
COMMENT ON TABLE "LlmConversations" IS 'Stores ChatGPT conversation metadata';
COMMENT ON TABLE "LlmMessages" IS 'Stores individual messages with parsed responses';
COMMENT ON TABLE "LlmUsageQuotas" IS 'Tracks LLM usage limits and rate limiting per user';

COMMENT ON COLUMN "LlmPromptTemplates"."SystemPrompt" IS 'System prompt that defines behavior and output format';
COMMENT ON COLUMN "LlmPromptTemplates"."UserPromptTemplate" IS 'Template for user prompt with placeholders like {userInput}, {context}';
COMMENT ON COLUMN "LlmPromptTemplates"."ResponseFormat" IS 'Expected response format: json, text, structured';
COMMENT ON COLUMN "LlmPromptTemplates"."ResponseSchema" IS 'JSON schema for structured output parsing';
COMMENT ON COLUMN "LlmMessages"."RawContent" IS 'Raw LLM response (for user messages: user context)';
COMMENT ON COLUMN "LlmMessages"."ParsedContent" IS 'Parsed/formatted response sent to frontend';
COMMENT ON COLUMN "LlmMessages"."PromptTemplateId" IS 'Reference to the prompt template used';

-- =============================================
-- Seed Data: Default Prompt Templates
-- =============================================

-- Image Analysis Template
INSERT INTO "LlmPromptTemplates" (
    "Name", 
    "Description", 
    "SystemPrompt", 
    "UserPromptTemplate",
    "ResponseFormat",
    "ResponseSchema",
    "Model",
    "Temperature",
    "MaxTokens",
    "RequiresImage",
    "Category",
    "IsActive"
)
VALUES (
    'Image Analysis',
    'Analyze and describe images in detail',
    'You are an expert image analyst. Analyze the provided image and return a structured JSON response with the following fields: "description" (detailed description), "objects" (array of detected objects), "colors" (dominant colors), "mood" (overall mood/atmosphere), "suggestions" (actionable suggestions based on the image).',
    'Analyze this image: {userInput}',
    'json',
    '{"description": "string", "objects": ["string"], "colors": ["string"], "mood": "string", "suggestions": ["string"]}',
    'gpt-4o',
    0.7,
    1500,
    TRUE,
    'image_analysis',
    TRUE
) ON CONFLICT DO NOTHING;

-- Document OCR Template
INSERT INTO "LlmPromptTemplates" (
    "Name", 
    "Description", 
    "SystemPrompt", 
    "UserPromptTemplate",
    "ResponseFormat",
    "ResponseSchema",
    "Model",
    "Temperature",
    "MaxTokens",
    "RequiresImage",
    "Category",
    "IsActive"
)
VALUES (
    'Document OCR',
    'Extract text and data from document images',
    'You are an OCR expert. Extract all text from the provided document image and return a structured JSON response with: "extractedText" (full text), "documentType" (type of document), "keyFields" (important fields like dates, names, amounts), "confidence" (extraction confidence 0-100).',
    'Extract text from this document: {userInput}',
    'json',
    '{"extractedText": "string", "documentType": "string", "keyFields": {}, "confidence": 0}',
    'gpt-4o',
    0.3,
    2000,
    TRUE,
    'document_processing',
    TRUE
) ON CONFLICT DO NOTHING;

-- Receipt Scanner Template
INSERT INTO "LlmPromptTemplates" (
    "Name", 
    "Description", 
    "SystemPrompt", 
    "UserPromptTemplate",
    "ResponseFormat",
    "ResponseSchema",
    "Model",
    "Temperature",
    "MaxTokens",
    "RequiresImage",
    "Category",
    "IsActive"
)
VALUES (
    'Receipt Scanner',
    'Extract structured data from receipt images',
    'You are a receipt parsing expert. Extract all information from the receipt image and return JSON with: "merchantName", "date", "total", "subtotal", "tax", "items" (array with name, quantity, price), "paymentMethod", "currency".',
    'Scan this receipt: {userInput}',
    'json',
    '{"merchantName": "string", "date": "string", "total": 0, "subtotal": 0, "tax": 0, "items": [{"name": "string", "quantity": 0, "price": 0}], "paymentMethod": "string", "currency": "string"}',
    'gpt-4o',
    0.2,
    1000,
    TRUE,
    'financial',
    TRUE
) ON CONFLICT DO NOTHING;

-- Product Recommendation Template
INSERT INTO "LlmPromptTemplates" (
    "Name", 
    "Description", 
    "SystemPrompt", 
    "UserPromptTemplate",
    "ResponseFormat",
    "ResponseSchema",
    "Model",
    "Temperature",
    "MaxTokens",
    "RequiresImage",
    "Category",
    "IsActive"
)
VALUES (
    'Product Recommendation',
    'Analyze product images and provide recommendations',
    'You are a product expert. Analyze the product image and provide recommendations. Return JSON with: "productName", "category", "estimatedPrice", "features" (array), "pros" (array), "cons" (array), "alternatives" (array), "buyingTips" (array).',
    'Analyze this product: {userInput}',
    'json',
    '{"productName": "string", "category": "string", "estimatedPrice": "string", "features": ["string"], "pros": ["string"], "cons": ["string"], "alternatives": ["string"], "buyingTips": ["string"]}',
    'gpt-4o',
    0.7,
    1500,
    TRUE,
    'shopping',
    TRUE
) ON CONFLICT DO NOTHING;

-- =============================================
-- Migration Complete
-- =============================================

-- Verify tables created
SELECT 
    table_name,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = t.table_name) as column_count
FROM information_schema.tables t
WHERE table_name LIKE 'Llm%'
ORDER BY table_name;
