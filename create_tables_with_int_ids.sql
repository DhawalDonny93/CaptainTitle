-- =====================================================
-- PostgreSQL Table Creation Script with INT IDs
-- Run this AFTER dropping old tables
-- =====================================================

BEGIN;

-- =====================================================
-- LLM Tables
-- =====================================================

-- LlmPromptTemplate (no dependencies)
CREATE TABLE "LlmPromptTemplate" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "SystemPrompt" TEXT NOT NULL,
    "UserPromptTemplate" TEXT,
    "ResponseFormat" VARCHAR(20) NOT NULL DEFAULT 'json',
    "ResponseSchema" TEXT,
    "Model" VARCHAR(50) NOT NULL DEFAULT 'gpt-4o',
    "Temperature" DOUBLE PRECISION NOT NULL DEFAULT 0.7,
    "MaxTokens" INTEGER NOT NULL DEFAULT 1000,
    "RequiresImage" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Category" VARCHAR(50) NOT NULL DEFAULT 'general',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedByUserId" UUID
);

-- LlmConversation
CREATE TABLE "LlmConversation" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Title" VARCHAR(100) NOT NULL DEFAULT 'New Conversation',
    "Model" VARCHAR(20) NOT NULL DEFAULT 'gpt-4o',
    "TotalMessages" INTEGER NOT NULL DEFAULT 0,
    "TotalTokensUsed" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_LlmConversation_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- LlmMessage
CREATE TABLE "LlmMessage" (
    "Id" SERIAL PRIMARY KEY,
    "ConversationId" INTEGER NOT NULL,
    "Role" VARCHAR(20) NOT NULL,
    "RawContent" TEXT,
    "ParsedContent" TEXT NOT NULL,
    "PromptTemplateId" INTEGER,
    "ImageUrl" TEXT,
    "ImageBase64" TEXT,
    "PromptTokens" INTEGER,
    "CompletionTokens" INTEGER,
    "TotalTokens" INTEGER,
    "FinishReason" TEXT,
    "ResponseTimeMs" DOUBLE PRECISION,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmMessage_Conversation" FOREIGN KEY ("ConversationId") REFERENCES "LlmConversation"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LlmMessage_PromptTemplate" FOREIGN KEY ("PromptTemplateId") REFERENCES "LlmPromptTemplate"("Id") ON DELETE SET NULL
);

-- LlmUsageQuota
CREATE TABLE "LlmUsageQuota" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "PeriodStart" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "PeriodEnd" TIMESTAMP NOT NULL,
    "RequestsUsed" INTEGER NOT NULL DEFAULT 0,
    "RequestsLimit" INTEGER NOT NULL,
    "TokensUsed" INTEGER NOT NULL DEFAULT 0,
    "TokensLimit" INTEGER NOT NULL,
    "ImagesUsed" INTEGER NOT NULL DEFAULT 0,
    "ImagesLimit" INTEGER NOT NULL,
    "RequestsPerMinute" INTEGER NOT NULL,
    "RequestsPerHour" INTEGER NOT NULL,
    "RequestsPerDay" INTEGER NOT NULL,
    "ConsecutiveRequests" INTEGER NOT NULL DEFAULT 0,
    "LastRequestAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmUsageQuota_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- =====================================================
-- Subscription & Payment Tables
-- =====================================================

-- Subscription
CREATE TABLE "Subscription" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "ProductId" VARCHAR(255) NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "Platform" VARCHAR(20) NOT NULL,
    "PurchaseToken" VARCHAR(255) NOT NULL,
    "TransactionId" VARCHAR(255),
    "OriginalTransactionId" VARCHAR(255),
    "PurchaseDate" TIMESTAMP NOT NULL,
    "ExpiryDate" TIMESTAMP,
    "AutoRenew" BOOLEAN NOT NULL DEFAULT FALSE,
    "RevenueCatId" VARCHAR(255),
    "EntitlementIdentifier" VARCHAR(255),
    "OfferIdentifier" VARCHAR(255),
    "IsSandbox" BOOLEAN NOT NULL DEFAULT FALSE,
    "RenewalInfo" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Subscription_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- SubscriptionTransaction
CREATE TABLE "SubscriptionTransaction" (
    "Id" SERIAL PRIMARY KEY,
    "SubscriptionId" INTEGER NOT NULL,
    "TransactionId" VARCHAR(255) NOT NULL,
    "Amount" DECIMAL(10, 2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "TransactionDate" TIMESTAMP NOT NULL,
    "RevenueCatEventId" VARCHAR(255),
    "StoreTransactionId" VARCHAR(255),
    "TransactionType" VARCHAR(50),
    "RawData" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SubscriptionTransaction_Subscription" FOREIGN KEY ("SubscriptionId") REFERENCES "Subscription"("Id") ON DELETE CASCADE
);

-- Receipt
CREATE TABLE "Receipt" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Platform" VARCHAR(20) NOT NULL,
    "ReceiptData" TEXT NOT NULL,
    "Verified" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Receipt_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- PaywallEvent
CREATE TABLE "PaywallEvent" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID,
    "EventType" VARCHAR(50) NOT NULL,
    "PaywallId" VARCHAR(255) NOT NULL,
    "DeviceId" VARCHAR(255),
    "Platform" VARCHAR(20) NOT NULL,
    "EventData" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PaywallEvent_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE SET NULL
);

-- RevenueCatWebhookEvent
CREATE TABLE "RevenueCatWebhookEvent" (
    "Id" SERIAL PRIMARY KEY,
    "EventId" VARCHAR(255) NOT NULL,
    "EventType" VARCHAR(50) NOT NULL,
    "AppUserId" VARCHAR(255),
    "OriginalAppUserId" VARCHAR(255),
    "ProductId" VARCHAR(255),
    "EntitlementId" VARCHAR(255),
    "PurchasedAtMs" BIGINT,
    "ExpirationAtMs" BIGINT,
    "Environment" VARCHAR(20),
    "OriginalTransactionId" VARCHAR(255),
    "Currency" VARCHAR(10),
    "Price" DECIMAL(10, 2),
    "Processed" BOOLEAN NOT NULL DEFAULT FALSE,
    "ProcessedAt" TIMESTAMP,
    "ErrorMessage" TEXT,
    "RawPayload" JSONB NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- UserRevenueCatMapping
CREATE TABLE "UserRevenueCatMapping" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "RevenueCatId" VARCHAR(255) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserRevenueCatMapping_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- =====================================================
-- Onboarding Tables
-- =====================================================

-- OnboardingStep
CREATE TABLE "OnboardingStep" (
    "Id" SERIAL PRIMARY KEY,
    "StepKey" VARCHAR(100) NOT NULL UNIQUE,
    "StepName" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(500),
    "StepType" VARCHAR(50) NOT NULL DEFAULT 'questions',
    "MediaUrl" VARCHAR(1000),
    "MediaType" VARCHAR(50),
    "ContentHtml" TEXT,
    "DisplayOrder" INTEGER NOT NULL,
    "IsRequired" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- OnboardingQuestion
CREATE TABLE "OnboardingQuestion" (
    "Id" SERIAL PRIMARY KEY,
    "OnboardingStepId" INTEGER NOT NULL,
    "QuestionKey" VARCHAR(100) NOT NULL,
    "QuestionText" VARCHAR(500) NOT NULL,
    "QuestionType" VARCHAR(50) NOT NULL,
    "ValidationRules" TEXT,
    "Options" TEXT,
    "Placeholder" VARCHAR(500),
    "HelpText" VARCHAR(1000),
    "IsRequired" BOOLEAN NOT NULL DEFAULT TRUE,
    "DisplayOrder" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_OnboardingQuestion_Step" FOREIGN KEY ("OnboardingStepId") REFERENCES "OnboardingStep"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_OnboardingQuestion_StepKey" UNIQUE ("OnboardingStepId", "QuestionKey")
);

-- UserOnboardingSync
CREATE TABLE "UserOnboardingSync" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Version" VARCHAR(20) NOT NULL,
    "IsCompleted" BOOLEAN NOT NULL DEFAULT TRUE,
    "CompletedAt" TIMESTAMP NOT NULL,
    "SyncedAt" TIMESTAMP NOT NULL,
    "TotalSteps" INTEGER NOT NULL,
    "TotalAnswers" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserOnboardingSync_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserOnboardingSync_UserId" UNIQUE ("UserId")
);

-- UserOnboardingProgress
CREATE TABLE "UserOnboardingProgress" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "OnboardingStepId" INTEGER,
    "UserOnboardingSyncId" INTEGER,
    "StepId" VARCHAR(100),
    "StepKey" VARCHAR(100),
    "StepType" VARCHAR(50),
    "IsCompleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CompletedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserOnboardingProgress_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserOnboardingProgress_Step" FOREIGN KEY ("OnboardingStepId") REFERENCES "OnboardingStep"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserOnboardingProgress_Sync" FOREIGN KEY ("UserOnboardingSyncId") REFERENCES "UserOnboardingSync"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserOnboardingProgress_UserStep" UNIQUE ("UserId", "OnboardingStepId")
);

-- UserOnboardingAnswer
CREATE TABLE "UserOnboardingAnswer" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "OnboardingQuestionId" INTEGER,
    "UserOnboardingProgressId" INTEGER,
    "QuestionId" VARCHAR(100),
    "QuestionKey" VARCHAR(100),
    "AnswerValue" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserOnboardingAnswer_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserOnboardingAnswer_Question" FOREIGN KEY ("OnboardingQuestionId") REFERENCES "OnboardingQuestion"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserOnboardingAnswer_Progress" FOREIGN KEY ("UserOnboardingProgressId") REFERENCES "UserOnboardingProgress"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserOnboardingAnswer_UserQuestion" UNIQUE ("UserId", "OnboardingQuestionId")
);

-- =====================================================
-- Notification Tables
-- =====================================================

-- NotificationPreference
CREATE TABLE "NotificationPreference" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "PushEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "EmailEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "SmsEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "SystemNotifications" BOOLEAN NOT NULL DEFAULT TRUE,
    "SubscriptionNotifications" BOOLEAN NOT NULL DEFAULT TRUE,
    "SocialNotifications" BOOLEAN NOT NULL DEFAULT TRUE,
    "MarketingNotifications" BOOLEAN NOT NULL DEFAULT FALSE,
    "QuietHoursStart" TIME,
    "QuietHoursEnd" TIME,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_NotificationPreference_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_NotificationPreference_UserId" UNIQUE ("UserId")
);

-- ScheduledNotification
CREATE TABLE "ScheduledNotification" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Body" VARCHAR(500) NOT NULL,
    "Sound" VARCHAR(50) DEFAULT 'default',
    "Badge" VARCHAR(10),
    "Category" VARCHAR(50),
    "DataJson" TEXT,
    "ScheduledFor" TIMESTAMP NOT NULL,
    "IsSent" BOOLEAN NOT NULL DEFAULT FALSE,
    "SentAt" TIMESTAMP,
    "IsCancelled" BOOLEAN NOT NULL DEFAULT FALSE,
    "CancelledAt" TIMESTAMP,
    "ErrorMessage" VARCHAR(500),
    "RetryCount" INTEGER NOT NULL DEFAULT 0,
    "MaxRetries" INTEGER NOT NULL DEFAULT 3,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_ScheduledNotification_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- InAppNotification
CREATE TABLE "InAppNotification" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Body" VARCHAR(1000) NOT NULL,
    "Type" VARCHAR(50) NOT NULL DEFAULT 'info',
    "Category" VARCHAR(50),
    "ActionUrl" VARCHAR(500),
    "ActionText" VARCHAR(100),
    "ImageUrl" VARCHAR(500),
    "DataJson" TEXT,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReadAt" TIMESTAMP,
    "IsArchived" BOOLEAN NOT NULL DEFAULT FALSE,
    "ArchivedAt" TIMESTAMP,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "ExpiresAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_InAppNotification_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- DeviceToken
CREATE TABLE "DeviceToken" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Token" VARCHAR(500) NOT NULL,
    "Platform" VARCHAR(20) NOT NULL,
    "DeviceId" VARCHAR(50),
    "DeviceModel" VARCHAR(100),
    "AppVersion" VARCHAR(20),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastUsedAt" TIMESTAMP,
    CONSTRAINT "FK_DeviceToken_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- DeviceRegistration
CREATE TABLE "DeviceRegistration" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "DeviceId" TEXT NOT NULL,
    "PublicKey" TEXT NOT NULL,
    "DeviceName" TEXT NOT NULL,
    "DeviceType" TEXT NOT NULL,
    "RegisteredAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastUsedAt" TIMESTAMP,
    "IsActive" BOOLEAN NOT NULL,
    CONSTRAINT "FK_DeviceRegistration_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_DeviceRegistration_UserDevice" UNIQUE ("UserId", "DeviceId")
);

-- =====================================================
-- System & Compliance Tables
-- =====================================================

-- AuditLog
CREATE TABLE "AuditLog" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID,
    "Action" VARCHAR(100) NOT NULL,
    "EntityType" VARCHAR(100) NOT NULL,
    "EntityId" UUID,
    "Changes" TEXT,
    "IpAddress" VARCHAR(45) NOT NULL,
    "UserAgent" VARCHAR(500),
    "DeviceId" VARCHAR(50),
    "AdditionalInfo" VARCHAR(500),
    "Success" BOOLEAN NOT NULL DEFAULT TRUE,
    "ErrorMessage" VARCHAR(500),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_AuditLog_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE SET NULL
);

-- DataExport
CREATE TABLE "DataExport" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "ExportId" VARCHAR(100) NOT NULL,
    "IsReady" BOOLEAN NOT NULL DEFAULT FALSE,
    "FilePath" VARCHAR(500),
    "FileSizeBytes" BIGINT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "IsDownloaded" BOOLEAN NOT NULL DEFAULT FALSE,
    "DownloadedAt" TIMESTAMP,
    CONSTRAINT "FK_DataExport_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- PolicyDocument
CREATE TABLE "PolicyDocument" (
    "Id" SERIAL PRIMARY KEY,
    "Type" VARCHAR(50) NOT NULL,
    "Version" VARCHAR(20) NOT NULL,
    "Content" TEXT NOT NULL,
    "Summary" VARCHAR(500),
    "EffectiveDate" TIMESTAMP NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- UserPolicyAcceptance
CREATE TABLE "UserPolicyAcceptance" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "PolicyId" INTEGER NOT NULL,
    "AcceptedAt" TIMESTAMP NOT NULL,
    "IpAddress" VARCHAR(45) NOT NULL,
    "UserAgent" VARCHAR(500),
    "DeviceId" VARCHAR(50),
    CONSTRAINT "FK_UserPolicyAcceptance_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserPolicyAcceptance_Policy" FOREIGN KEY ("PolicyId") REFERENCES "PolicyDocument"("Id") ON DELETE CASCADE
);

-- ParentalConsent
CREATE TABLE "ParentalConsent" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "ParentEmail" VARCHAR(100) NOT NULL,
    "ParentName" VARCHAR(100),
    "ConsentToken" VARCHAR(100) NOT NULL,
    "IsGranted" BOOLEAN NOT NULL DEFAULT FALSE,
    "GrantedAt" TIMESTAMP,
    "IpAddress" VARCHAR(45),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    CONSTRAINT "FK_ParentalConsent_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- SystemSetting
CREATE TABLE "SystemSetting" (
    "Id" SERIAL PRIMARY KEY,
    "Key" VARCHAR(100) NOT NULL UNIQUE,
    "Value" TEXT NOT NULL,
    "Type" VARCHAR(50) NOT NULL DEFAULT 'string',
    "Category" VARCHAR(100) NOT NULL DEFAULT 'general',
    "Description" VARCHAR(500),
    "IsPublic" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- UserPreference
CREATE TABLE "UserPreference" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Language" VARCHAR(10) NOT NULL DEFAULT 'en',
    "Theme" VARCHAR(20) NOT NULL DEFAULT 'light',
    "Timezone" VARCHAR(50) NOT NULL DEFAULT 'UTC',
    "CustomPreferences" TEXT NOT NULL DEFAULT '{}',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserPreference_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserPreference_UserId" UNIQUE ("UserId")
);

-- UserRole
CREATE TABLE "UserRole" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Role" VARCHAR(50) NOT NULL DEFAULT 'User',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserRole_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserRole_UserRole" UNIQUE ("UserId", "Role")
);

-- =====================================================
-- Create Indexes for Performance
-- =====================================================

-- LLM Indexes
CREATE INDEX "IX_LlmConversation_UserId" ON "LlmConversation"("UserId");
CREATE INDEX "IX_LlmConversation_CreatedAt" ON "LlmConversation"("CreatedAt");
CREATE INDEX "IX_LlmMessage_ConversationId" ON "LlmMessage"("ConversationId");
CREATE INDEX "IX_LlmMessage_CreatedAt" ON "LlmMessage"("CreatedAt");
CREATE INDEX "IX_LlmUsageQuota_UserId" ON "LlmUsageQuota"("UserId");
CREATE INDEX "IX_LlmUsageQuota_PeriodStart" ON "LlmUsageQuota"("PeriodStart");

-- Subscription Indexes
CREATE INDEX "IX_Subscription_UserId" ON "Subscription"("UserId");
CREATE INDEX "IX_Subscription_Status" ON "Subscription"("Status");
CREATE INDEX "IX_Subscription_ExpiryDate" ON "Subscription"("ExpiryDate");
CREATE INDEX "IX_SubscriptionTransaction_SubscriptionId" ON "SubscriptionTransaction"("SubscriptionId");
CREATE INDEX "IX_Receipt_UserId" ON "Receipt"("UserId");
CREATE INDEX "IX_PaywallEvent_UserId" ON "PaywallEvent"("UserId");
CREATE INDEX "IX_UserRevenueCatMapping_UserId" ON "UserRevenueCatMapping"("UserId");
CREATE INDEX "IX_UserRevenueCatMapping_RevenueCatId" ON "UserRevenueCatMapping"("RevenueCatId");

-- Onboarding Indexes
CREATE INDEX "IX_OnboardingStep_DisplayOrder" ON "OnboardingStep"("DisplayOrder");
CREATE INDEX "IX_OnboardingQuestion_StepId" ON "OnboardingQuestion"("OnboardingStepId");
CREATE INDEX "IX_OnboardingQuestion_DisplayOrder" ON "OnboardingQuestion"("DisplayOrder");
CREATE INDEX "IX_UserOnboardingSync_UserId" ON "UserOnboardingSync"("UserId");
CREATE INDEX "IX_UserOnboardingProgress_UserId" ON "UserOnboardingProgress"("UserId");
CREATE INDEX "IX_UserOnboardingProgress_StepId" ON "UserOnboardingProgress"("OnboardingStepId");
CREATE INDEX "IX_UserOnboardingAnswer_UserId" ON "UserOnboardingAnswer"("UserId");
CREATE INDEX "IX_UserOnboardingAnswer_QuestionId" ON "UserOnboardingAnswer"("OnboardingQuestionId");

-- Notification Indexes
CREATE INDEX "IX_NotificationPreference_UserId" ON "NotificationPreference"("UserId");
CREATE INDEX "IX_ScheduledNotification_UserId" ON "ScheduledNotification"("UserId");
CREATE INDEX "IX_ScheduledNotification_ScheduledFor" ON "ScheduledNotification"("ScheduledFor");
CREATE INDEX "IX_InAppNotification_UserId" ON "InAppNotification"("UserId");
CREATE INDEX "IX_InAppNotification_IsRead" ON "InAppNotification"("IsRead");
CREATE INDEX "IX_DeviceToken_UserId" ON "DeviceToken"("UserId");
CREATE INDEX "IX_DeviceToken_Token" ON "DeviceToken"("Token");
CREATE INDEX "IX_DeviceRegistration_UserId" ON "DeviceRegistration"("UserId");

-- System Indexes
CREATE INDEX "IX_AuditLog_UserId" ON "AuditLog"("UserId");
CREATE INDEX "IX_AuditLog_CreatedAt" ON "AuditLog"("CreatedAt");
CREATE INDEX "IX_AuditLog_EntityType" ON "AuditLog"("EntityType");
CREATE INDEX "IX_DataExport_UserId" ON "DataExport"("UserId");
CREATE INDEX "IX_UserPolicyAcceptance_UserId" ON "UserPolicyAcceptance"("UserId");
CREATE INDEX "IX_UserPolicyAcceptance_PolicyId" ON "UserPolicyAcceptance"("PolicyId");
CREATE INDEX "IX_ParentalConsent_UserId" ON "ParentalConsent"("UserId");
CREATE INDEX "IX_UserPreference_UserId" ON "UserPreference"("UserId");
CREATE INDEX "IX_UserRole_UserId" ON "UserRole"("UserId");

COMMIT;

-- =====================================================
-- Verification Queries
-- =====================================================

SELECT 
    schemaname,
    tablename,
    indexname
FROM pg_indexes
WHERE schemaname = 'public'
AND tablename IN (
    'LlmPromptTemplate', 'LlmConversation', 'LlmMessage', 'LlmUsageQuota',
    'Subscription', 'SubscriptionTransaction', 'Receipt', 'PaywallEvent',
    'OnboardingStep', 'OnboardingQuestion', 'UserOnboardingProgress'
)
ORDER BY tablename, indexname;

-- Check table row counts (should be 0 for fresh tables)
SELECT 
    'LlmPromptTemplate' as table_name, COUNT(*) as row_count FROM "LlmPromptTemplate"
UNION ALL SELECT 'LlmConversation', COUNT(*) FROM "LlmConversation"
UNION ALL SELECT 'Subscription', COUNT(*) FROM "Subscription"
UNION ALL SELECT 'OnboardingStep', COUNT(*) FROM "OnboardingStep";
