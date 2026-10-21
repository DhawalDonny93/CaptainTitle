-- =====================================================
-- COMPLETE POSTGRESQL DATABASE SCHEMA FOR SUPABASE
-- All tables with INTEGER IDs (not UUID)
-- Pascal Case naming convention
-- Compatible with Supabase PostgreSQL
-- =====================================================

BEGIN;

-- =====================================================
-- CORE USER & AUTHENTICATION TABLES
-- =====================================================

-- User Table (Core table - must be created first)
CREATE TABLE "User" (
    "Id" SERIAL PRIMARY KEY,
    "Email" VARCHAR(50),
    "FirstName" VARCHAR(30),
    "LastName" VARCHAR(30),
    "PhoneNumber" VARCHAR(20),
    "Gender" VARCHAR(100),
    "DateOfBirth" TIMESTAMP,
    "ProfilePhotoUrl" VARCHAR(500),
    "ProfilePhotoThumbnailUrl" VARCHAR(500),
    "PasswordHash" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "EmailVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "PhoneVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "OnboardingCompleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "AgeVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "RequiresParentalConsent" BOOLEAN NOT NULL DEFAULT FALSE,
    "ParentalConsentGranted" BOOLEAN NOT NULL DEFAULT FALSE,
    "FailedLoginAttempts" INTEGER NOT NULL DEFAULT 0,
    "LockoutEnd" TIMESTAMP,
    "AgreesTermsandPolicy" BOOLEAN DEFAULT TRUE,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP,
    "ScheduledDeletionDate" TIMESTAMP,
    "DeletionReason" TEXT,
    "IsSubscribed" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX "IX_User_Email" ON "User"("Email") WHERE "Email" IS NOT NULL;
CREATE UNIQUE INDEX "IX_User_PhoneNumber" ON "User"("PhoneNumber") WHERE "PhoneNumber" IS NOT NULL;
CREATE INDEX "IX_User_IsActive" ON "User"("IsActive");
CREATE INDEX "IX_User_CreatedAt" ON "User"("CreatedAt");

-- UserProvider Table
CREATE TABLE "UserProvider" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Provider" VARCHAR(50) NOT NULL,
    "ProviderId" VARCHAR(255) NOT NULL,
    "ProviderData" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserProvider_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserProvider_Provider_ProviderId" ON "UserProvider"("Provider", "ProviderId");
CREATE INDEX "IX_UserProvider_UserId" ON "UserProvider"("UserId");

-- RefreshToken Table
CREATE TABLE "RefreshToken" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Token" VARCHAR(255) NOT NULL,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_RefreshToken_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_RefreshToken_Token" ON "RefreshToken"("Token");
CREATE INDEX "IX_RefreshToken_UserId" ON "RefreshToken"("UserId");
CREATE INDEX "IX_RefreshToken_ExpiresAt" ON "RefreshToken"("ExpiresAt");

-- UserSession Table
CREATE TABLE "UserSession" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "DeviceId" VARCHAR(255) NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "DeviceModel" VARCHAR(100) NOT NULL,
    "AppVersion" VARCHAR(50) NOT NULL,
    "LastActiveAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "TimeoutMinutes" INTEGER NOT NULL DEFAULT 30,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserSession_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserSession_UserId" ON "UserSession"("UserId");
CREATE INDEX "IX_UserSession_DeviceId" ON "UserSession"("DeviceId");
CREATE INDEX "IX_UserSession_ExpiresAt" ON "UserSession"("ExpiresAt");

-- OtpVerification Table
CREATE TABLE "OtpVerification" (
    "Id" SERIAL PRIMARY KEY,
    "Email" VARCHAR(50),
    "PhoneNumber" VARCHAR(20),
    "Otp" VARCHAR(6) NOT NULL,
    "SessionId" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "IsUsed" BOOLEAN NOT NULL DEFAULT FALSE,
    "AttemptCount" INTEGER NOT NULL DEFAULT 0,
    "MaxAttempts" INTEGER NOT NULL DEFAULT 3
);

CREATE UNIQUE INDEX "IX_OtpVerification_SessionId" ON "OtpVerification"("SessionId");
CREATE INDEX "IX_OtpVerification_PhoneNumber_CreatedAt" ON "OtpVerification"("PhoneNumber", "CreatedAt");
CREATE INDEX "IX_OtpVerification_Email" ON "OtpVerification"("Email");

-- PasswordReset Table
CREATE TABLE "PasswordReset" (
    "Id" SERIAL PRIMARY KEY,
    "Email" TEXT NOT NULL,
    "Token" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "IsUsed" BOOLEAN NOT NULL DEFAULT FALSE,
    "UserId" INTEGER,
    CONSTRAINT "FK_PasswordReset_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_PasswordReset_Token" ON "PasswordReset"("Token");
CREATE INDEX "IX_PasswordReset_Email_CreatedAt" ON "PasswordReset"("Email", "CreatedAt");
CREATE INDEX "IX_PasswordReset_UserId" ON "PasswordReset"("UserId");

-- =====================================================
-- LLM TABLES
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
    "CreatedByUserId" INTEGER
);

CREATE UNIQUE INDEX "IX_LlmPromptTemplate_Name" ON "LlmPromptTemplate"("Name");
CREATE INDEX "IX_LlmPromptTemplate_IsActive" ON "LlmPromptTemplate"("IsActive");
CREATE INDEX "IX_LlmPromptTemplate_Category" ON "LlmPromptTemplate"("Category");

-- LlmConversation
CREATE TABLE "LlmConversation" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Title" VARCHAR(100) NOT NULL DEFAULT 'New Conversation',
    "Model" VARCHAR(20) NOT NULL DEFAULT 'gpt-4o',
    "TotalMessages" INTEGER NOT NULL DEFAULT 0,
    "TotalTokensUsed" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_LlmConversation_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_LlmConversation_UserId" ON "LlmConversation"("UserId");
CREATE INDEX "IX_LlmConversation_IsDeleted" ON "LlmConversation"("IsDeleted");
CREATE INDEX "IX_LlmConversation_CreatedAt" ON "LlmConversation"("CreatedAt");

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

CREATE INDEX "IX_LlmMessage_ConversationId" ON "LlmMessage"("ConversationId");
CREATE INDEX "IX_LlmMessage_CreatedAt" ON "LlmMessage"("CreatedAt");
CREATE INDEX "IX_LlmMessage_PromptTemplateId" ON "LlmMessage"("PromptTemplateId");

-- LlmUsageQuota
CREATE TABLE "LlmUsageQuota" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
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

CREATE UNIQUE INDEX "IX_LlmUsageQuota_UserId" ON "LlmUsageQuota"("UserId");
CREATE INDEX "IX_LlmUsageQuota_PeriodStart" ON "LlmUsageQuota"("PeriodStart");

-- =====================================================
-- SUBSCRIPTION & PAYMENT TABLES
-- =====================================================

-- Product Table
CREATE TABLE "Product" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "Price" DECIMAL(10, 2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL,
    "Period" VARCHAR(20),
    "TrialPeriodDays" INTEGER,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "IX_Product_IsActive" ON "Product"("IsActive");

-- Subscription
CREATE TABLE "Subscription" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
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

CREATE INDEX "IX_Subscription_UserId" ON "Subscription"("UserId");
CREATE INDEX "IX_Subscription_Status" ON "Subscription"("Status");
CREATE INDEX "IX_Subscription_ProductId" ON "Subscription"("ProductId");
CREATE INDEX "IX_Subscription_ExpiryDate" ON "Subscription"("ExpiryDate");
CREATE INDEX "IX_Subscription_TransactionId" ON "Subscription"("TransactionId");
CREATE INDEX "IX_Subscription_OriginalTransactionId" ON "Subscription"("OriginalTransactionId");
CREATE UNIQUE INDEX "IX_Subscription_UserId_ProductId_PurchaseToken" ON "Subscription"("UserId", "ProductId", "PurchaseToken");

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

CREATE INDEX "IX_SubscriptionTransaction_SubscriptionId" ON "SubscriptionTransaction"("SubscriptionId");
CREATE INDEX "IX_SubscriptionTransaction_TransactionId" ON "SubscriptionTransaction"("TransactionId");

-- Receipt
CREATE TABLE "Receipt" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Platform" VARCHAR(20) NOT NULL,
    "ReceiptData" TEXT NOT NULL,
    "Verified" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Receipt_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Receipt_UserId" ON "Receipt"("UserId");
CREATE INDEX "IX_Receipt_Platform" ON "Receipt"("Platform");

-- PaywallEvent
CREATE TABLE "PaywallEvent" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER,
    "EventType" VARCHAR(50) NOT NULL,
    "PaywallId" VARCHAR(255) NOT NULL,
    "DeviceId" VARCHAR(255),
    "Platform" VARCHAR(20) NOT NULL,
    "EventData" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PaywallEvent_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_PaywallEvent_UserId" ON "PaywallEvent"("UserId");
CREATE INDEX "IX_PaywallEvent_EventType" ON "PaywallEvent"("EventType");
CREATE INDEX "IX_PaywallEvent_PaywallId" ON "PaywallEvent"("PaywallId");
CREATE INDEX "IX_PaywallEvent_CreatedAt" ON "PaywallEvent"("CreatedAt");

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

CREATE INDEX "IX_RevenueCatWebhookEvent_EventType" ON "RevenueCatWebhookEvent"("EventType");
CREATE INDEX "IX_RevenueCatWebhookEvent_AppUserId" ON "RevenueCatWebhookEvent"("AppUserId");
CREATE INDEX "IX_RevenueCatWebhookEvent_OriginalTransactionId" ON "RevenueCatWebhookEvent"("OriginalTransactionId");
CREATE INDEX "IX_RevenueCatWebhookEvent_Processed" ON "RevenueCatWebhookEvent"("Processed");
CREATE INDEX "IX_RevenueCatWebhookEvent_CreatedAt" ON "RevenueCatWebhookEvent"("CreatedAt");

-- UserRevenueCatMapping
CREATE TABLE "UserRevenueCatMapping" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "RevenueCatId" VARCHAR(255) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserRevenueCatMapping_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserRevenueCatMapping_UserId_RevenueCatId" ON "UserRevenueCatMapping"("UserId", "RevenueCatId");
CREATE INDEX "IX_UserRevenueCatMapping_UserId" ON "UserRevenueCatMapping"("UserId");
CREATE INDEX "IX_UserRevenueCatMapping_RevenueCatId" ON "UserRevenueCatMapping"("RevenueCatId");

-- =====================================================
-- ONBOARDING TABLES
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

CREATE UNIQUE INDEX "IX_OnboardingStep_StepKey" ON "OnboardingStep"("StepKey");
CREATE INDEX "IX_OnboardingStep_DisplayOrder" ON "OnboardingStep"("DisplayOrder");

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
    CONSTRAINT "FK_OnboardingQuestion_Step" FOREIGN KEY ("OnboardingStepId") REFERENCES "OnboardingStep"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_OnboardingQuestion_StepId_QuestionKey" ON "OnboardingQuestion"("OnboardingStepId", "QuestionKey");
CREATE INDEX "IX_OnboardingQuestion_StepId" ON "OnboardingQuestion"("OnboardingStepId");
CREATE INDEX "IX_OnboardingQuestion_DisplayOrder" ON "OnboardingQuestion"("DisplayOrder");

-- UserOnboardingSync
CREATE TABLE "UserOnboardingSync" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Version" VARCHAR(20) NOT NULL,
    "IsCompleted" BOOLEAN NOT NULL DEFAULT TRUE,
    "CompletedAt" TIMESTAMP NOT NULL,
    "SyncedAt" TIMESTAMP NOT NULL,
    "TotalSteps" INTEGER NOT NULL,
    "TotalAnswers" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserOnboardingSync_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserOnboardingSync_UserId" ON "UserOnboardingSync"("UserId");
CREATE INDEX "IX_UserOnboardingSync_UserId_Index" ON "UserOnboardingSync"("UserId");

-- UserOnboardingProgress
CREATE TABLE "UserOnboardingProgress" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
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
    CONSTRAINT "FK_UserOnboardingProgress_Sync" FOREIGN KEY ("UserOnboardingSyncId") REFERENCES "UserOnboardingSync"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserOnboardingProgress_UserId_StepId" ON "UserOnboardingProgress"("UserId", "OnboardingStepId");
CREATE INDEX "IX_UserOnboardingProgress_UserId" ON "UserOnboardingProgress"("UserId");
CREATE INDEX "IX_UserOnboardingProgress_StepId" ON "UserOnboardingProgress"("OnboardingStepId");
CREATE INDEX "IX_UserOnboardingProgress_IsCompleted" ON "UserOnboardingProgress"("IsCompleted");

-- UserOnboardingAnswer
CREATE TABLE "UserOnboardingAnswer" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "OnboardingQuestionId" INTEGER,
    "UserOnboardingProgressId" INTEGER,
    "QuestionId" VARCHAR(100),
    "QuestionKey" VARCHAR(100),
    "AnswerValue" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserOnboardingAnswer_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserOnboardingAnswer_Question" FOREIGN KEY ("OnboardingQuestionId") REFERENCES "OnboardingQuestion"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserOnboardingAnswer_Progress" FOREIGN KEY ("UserOnboardingProgressId") REFERENCES "UserOnboardingProgress"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserOnboardingAnswer_UserId_QuestionId" ON "UserOnboardingAnswer"("UserId", "OnboardingQuestionId");
CREATE INDEX "IX_UserOnboardingAnswer_UserId" ON "UserOnboardingAnswer"("UserId");
CREATE INDEX "IX_UserOnboardingAnswer_QuestionId" ON "UserOnboardingAnswer"("OnboardingQuestionId");

-- =====================================================
-- NOTIFICATION TABLES
-- =====================================================

-- NotificationPreference
CREATE TABLE "NotificationPreference" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
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
    CONSTRAINT "FK_NotificationPreference_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_NotificationPreference_UserId" ON "NotificationPreference"("UserId");

-- ScheduledNotification
CREATE TABLE "ScheduledNotification" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
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

CREATE INDEX "IX_ScheduledNotification_UserId" ON "ScheduledNotification"("UserId");
CREATE INDEX "IX_ScheduledNotification_ScheduledFor" ON "ScheduledNotification"("ScheduledFor");
CREATE INDEX "IX_ScheduledNotification_IsSent" ON "ScheduledNotification"("IsSent");
CREATE INDEX "IX_ScheduledNotification_IsCancelled" ON "ScheduledNotification"("IsCancelled");
CREATE INDEX "IX_ScheduledNotification_Composite" ON "ScheduledNotification"("IsSent", "IsCancelled", "ScheduledFor", "RetryCount");

-- InAppNotification
CREATE TABLE "InAppNotification" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
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

CREATE INDEX "IX_InAppNotification_UserId" ON "InAppNotification"("UserId");
CREATE INDEX "IX_InAppNotification_IsRead" ON "InAppNotification"("IsRead");
CREATE INDEX "IX_InAppNotification_IsArchived" ON "InAppNotification"("IsArchived");
CREATE INDEX "IX_InAppNotification_CreatedAt" ON "InAppNotification"("CreatedAt");
CREATE INDEX "IX_InAppNotification_Composite" ON "InAppNotification"("UserId", "IsRead", "IsArchived");

-- DeviceToken
CREATE TABLE "DeviceToken" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
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

CREATE UNIQUE INDEX "IX_DeviceToken_UserId_DeviceId" ON "DeviceToken"("UserId", "DeviceId");
CREATE INDEX "IX_DeviceToken_UserId" ON "DeviceToken"("UserId");
CREATE INDEX "IX_DeviceToken_Token" ON "DeviceToken"("Token");

-- DeviceRegistration
CREATE TABLE "DeviceRegistration" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "DeviceId" TEXT NOT NULL,
    "PublicKey" TEXT NOT NULL,
    "DeviceName" TEXT NOT NULL,
    "DeviceType" TEXT NOT NULL,
    "RegisteredAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastUsedAt" TIMESTAMP,
    "IsActive" BOOLEAN NOT NULL,
    CONSTRAINT "FK_DeviceRegistration_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_DeviceRegistration_UserId_DeviceId" ON "DeviceRegistration"("UserId", "DeviceId");
CREATE INDEX "IX_DeviceRegistration_UserId" ON "DeviceRegistration"("UserId");

-- =====================================================
-- SYSTEM & COMPLIANCE TABLES
-- =====================================================

-- AuditLog
CREATE TABLE "AuditLog" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER,
    "Action" VARCHAR(100) NOT NULL,
    "EntityType" VARCHAR(100) NOT NULL,
    "EntityId" INTEGER,
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

CREATE INDEX "IX_AuditLog_UserId" ON "AuditLog"("UserId");
CREATE INDEX "IX_AuditLog_Action" ON "AuditLog"("Action");
CREATE INDEX "IX_AuditLog_EntityType" ON "AuditLog"("EntityType");
CREATE INDEX "IX_AuditLog_CreatedAt" ON "AuditLog"("CreatedAt");

-- DataExport
CREATE TABLE "DataExport" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
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

CREATE INDEX "IX_DataExport_UserId" ON "DataExport"("UserId");
CREATE UNIQUE INDEX "IX_DataExport_ExportId" ON "DataExport"("ExportId");
CREATE INDEX "IX_DataExport_IsReady" ON "DataExport"("IsReady");

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

CREATE UNIQUE INDEX "IX_PolicyDocument_Type_Version" ON "PolicyDocument"("Type", "Version");
CREATE INDEX "IX_PolicyDocument_IsActive" ON "PolicyDocument"("IsActive");

-- UserPolicyAcceptance
CREATE TABLE "UserPolicyAcceptance" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "PolicyId" INTEGER NOT NULL,
    "AcceptedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IpAddress" VARCHAR(45) NOT NULL,
    "UserAgent" VARCHAR(500),
    "DeviceId" VARCHAR(50),
    CONSTRAINT "FK_UserPolicyAcceptance_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserPolicyAcceptance_Policy" FOREIGN KEY ("PolicyId") REFERENCES "PolicyDocument"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserPolicyAcceptance_UserId_PolicyId" ON "UserPolicyAcceptance"("UserId", "PolicyId");
CREATE INDEX "IX_UserPolicyAcceptance_UserId" ON "UserPolicyAcceptance"("UserId");
CREATE INDEX "IX_UserPolicyAcceptance_PolicyId" ON "UserPolicyAcceptance"("PolicyId");
CREATE INDEX "IX_UserPolicyAcceptance_AcceptedAt" ON "UserPolicyAcceptance"("AcceptedAt");

-- ParentalConsent
CREATE TABLE "ParentalConsent" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
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

CREATE INDEX "IX_ParentalConsent_UserId" ON "ParentalConsent"("UserId");
CREATE UNIQUE INDEX "IX_ParentalConsent_ConsentToken" ON "ParentalConsent"("ConsentToken");
CREATE INDEX "IX_ParentalConsent_IsGranted" ON "ParentalConsent"("IsGranted");

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

CREATE UNIQUE INDEX "IX_SystemSetting_Key" ON "SystemSetting"("Key");
CREATE INDEX "IX_SystemSetting_Category" ON "SystemSetting"("Category");
CREATE INDEX "IX_SystemSetting_IsPublic" ON "SystemSetting"("IsPublic");

-- UserPreference
CREATE TABLE "UserPreference" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Language" VARCHAR(10) NOT NULL DEFAULT 'en',
    "Theme" VARCHAR(20) NOT NULL DEFAULT 'light',
    "Timezone" VARCHAR(50) NOT NULL DEFAULT 'UTC',
    "CustomPreferences" TEXT NOT NULL DEFAULT '{}',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserPreference_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserPreference_UserId" ON "UserPreference"("UserId");

-- UserRole
CREATE TABLE "UserRole" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Role" VARCHAR(50) NOT NULL DEFAULT 'User',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserRole_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserRole_UserId_Role" ON "UserRole"("UserId", "Role");
CREATE INDEX "IX_UserRole_UserId" ON "UserRole"("UserId");
CREATE INDEX "IX_UserRole_Role" ON "UserRole"("Role");

-- =====================================================
-- ANALYTICS TABLES
-- =====================================================

-- AnalyticsEvents
CREATE TABLE "AnalyticsEvents" (
    "Id" BIGSERIAL PRIMARY KEY,
    "EventId" VARCHAR(255) UNIQUE NOT NULL,
    "UserId" VARCHAR(255) NOT NULL,
    "EventName" VARCHAR(255) NOT NULL,
    "Properties" JSONB,
    "Timestamp" TIMESTAMP NOT NULL,
    "SessionId" VARCHAR(255),
    "Platform" VARCHAR(50),
    "AppVersion" VARCHAR(50),
    "MixpanelTracked" BOOLEAN DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX "IX_AnalyticsEvents_EventId" ON "AnalyticsEvents"("EventId");
CREATE INDEX "IX_AnalyticsEvents_UserId" ON "AnalyticsEvents"("UserId");
CREATE INDEX "IX_AnalyticsEvents_EventName" ON "AnalyticsEvents"("EventName");
CREATE INDEX "IX_AnalyticsEvents_Timestamp" ON "AnalyticsEvents"("Timestamp");
CREATE INDEX "IX_AnalyticsEvents_SessionId" ON "AnalyticsEvents"("SessionId");
CREATE INDEX "IX_AnalyticsEvents_CreatedAt" ON "AnalyticsEvents"("CreatedAt");
CREATE INDEX "IX_AnalyticsEvents_MixpanelTracked" ON "AnalyticsEvents"("MixpanelTracked");

-- UserProperties
CREATE TABLE "UserProperties" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(255) UNIQUE NOT NULL,
    "Properties" JSONB NOT NULL DEFAULT '{}',
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX "IX_UserProperties_UserId" ON "UserProperties"("UserId");

-- RevenueEvents
CREATE TABLE "RevenueEvents" (
    "Id" BIGSERIAL PRIMARY KEY,
    "RevenueId" VARCHAR(255) UNIQUE NOT NULL,
    "UserId" VARCHAR(255) NOT NULL,
    "Amount" DECIMAL(10, 2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL DEFAULT 'USD',
    "ProductId" VARCHAR(255) NOT NULL,
    "TransactionId" VARCHAR(255) UNIQUE NOT NULL,
    "Properties" JSONB,
    "Timestamp" TIMESTAMP NOT NULL,
    "MixpanelTracked" BOOLEAN DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX "IX_RevenueEvents_RevenueId" ON "RevenueEvents"("RevenueId");
CREATE UNIQUE INDEX "IX_RevenueEvents_TransactionId" ON "RevenueEvents"("TransactionId");
CREATE INDEX "IX_RevenueEvents_UserId" ON "RevenueEvents"("UserId");
CREATE INDEX "IX_RevenueEvents_Timestamp" ON "RevenueEvents"("Timestamp");
CREATE INDEX "IX_RevenueEvents_CreatedAt" ON "RevenueEvents"("CreatedAt");

-- AnalyticsFunnels
CREATE TABLE "AnalyticsFunnels" (
    "Id" SERIAL PRIMARY KEY,
    "FunnelId" VARCHAR(255) UNIQUE NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "Steps" JSONB NOT NULL DEFAULT '[]',
    "Filters" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX "IX_AnalyticsFunnels_FunnelId" ON "AnalyticsFunnels"("FunnelId");
CREATE INDEX "IX_AnalyticsFunnels_CreatedAt" ON "AnalyticsFunnels"("CreatedAt");

-- AnalyticsCohorts
CREATE TABLE "AnalyticsCohorts" (
    "Id" SERIAL PRIMARY KEY,
    "CohortId" VARCHAR(255) UNIQUE NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "Criteria" JSONB NOT NULL DEFAULT '{}',
    "UserCount" INTEGER DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX "IX_AnalyticsCohorts_CohortId" ON "AnalyticsCohorts"("CohortId");
CREATE INDEX "IX_AnalyticsCohorts_CreatedAt" ON "AnalyticsCohorts"("CreatedAt");

-- AnalyticsExports
CREATE TABLE "AnalyticsExports" (
    "Id" SERIAL PRIMARY KEY,
    "ExportId" VARCHAR(255) UNIQUE NOT NULL,
    "Type" VARCHAR(50) NOT NULL,
    "Format" VARCHAR(10) NOT NULL DEFAULT 'csv',
    "StartDate" TIMESTAMP NOT NULL,
    "EndDate" TIMESTAMP NOT NULL,
    "Filters" JSONB,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'processing',
    "FilePath" TEXT,
    "FileSize" BIGINT,
    "ExpiresAt" TIMESTAMP,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CompletedAt" TIMESTAMP
);

CREATE UNIQUE INDEX "IX_AnalyticsExports_ExportId" ON "AnalyticsExports"("ExportId");
CREATE INDEX "IX_AnalyticsExports_Status" ON "AnalyticsExports"("Status");
CREATE INDEX "IX_AnalyticsExports_CreatedAt" ON "AnalyticsExports"("CreatedAt");
CREATE INDEX "IX_AnalyticsExports_ExpiresAt" ON "AnalyticsExports"("ExpiresAt");

-- =====================================================
-- AI SEO & CONTENT MANAGEMENT TABLES
-- =====================================================

-- Project Table
CREATE TABLE "Project" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Domain" VARCHAR(255),
    "BrandVoice" TEXT,
    "TargetAudience" TEXT,
    "CmsCredentials" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Project_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Project_UserId" ON "Project" ("UserId");
CREATE INDEX "IX_Project_Domain" ON "Project" ("Domain");
CREATE INDEX "IX_Project_IsActive" ON "Project" ("IsActive");

-- ContentArticle Table
CREATE TABLE "ContentArticle" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Slug" VARCHAR(255),
    "ContentHtml" TEXT,
    "ContentMarkdown" TEXT,
    "MetaTitle" VARCHAR(255),
    "MetaDescription" TEXT,
    "FeaturedImageUrl" TEXT,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Draft',
    "ScheduledPublishAt" TIMESTAMP,
    "PublishedAt" TIMESTAMP,
    "WordCount" INTEGER NOT NULL DEFAULT 0,
    "AiModelUsed" VARCHAR(100),
    "GenerationPrompt" TEXT,
    "SeoScore" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_ContentArticle_Project_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ContentArticle_ProjectId" ON "ContentArticle" ("ProjectId");
CREATE INDEX "IX_ContentArticle_Slug" ON "ContentArticle" ("Slug");
CREATE INDEX "IX_ContentArticle_Status" ON "ContentArticle" ("Status");
CREATE INDEX "IX_ContentArticle_PublishedAt" ON "ContentArticle" ("PublishedAt");

-- AiTrackingPrompt Table
CREATE TABLE "AiTrackingPrompt" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "PromptText" TEXT NOT NULL,
    "Category" VARCHAR(100),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_AiTrackingPrompt_Project_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AiTrackingPrompt_ProjectId" ON "AiTrackingPrompt" ("ProjectId");
CREATE INDEX "IX_AiTrackingPrompt_IsActive" ON "AiTrackingPrompt" ("IsActive");

-- AiTrackingResult Table
CREATE TABLE "AiTrackingResult" (
    "Id" SERIAL PRIMARY KEY,
    "PromptId" INTEGER NOT NULL,
    "AiEngine" VARCHAR(50) NOT NULL,
    "CountryCode" VARCHAR(10),
    "ResponseText" TEXT,
    "BrandMentioned" BOOLEAN NOT NULL DEFAULT FALSE,
    "MentionPosition" INTEGER,
    "SentimentScore" DOUBLE PRECISION,
    "ExecutedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_AiTrackingResult_AiTrackingPrompt_PromptId" FOREIGN KEY ("PromptId") REFERENCES "AiTrackingPrompt" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AiTrackingResult_PromptId" ON "AiTrackingResult" ("PromptId");
CREATE INDEX "IX_AiTrackingResult_ExecutedAt" ON "AiTrackingResult" ("ExecutedAt");
CREATE INDEX "IX_AiTrackingResult_AiEngine" ON "AiTrackingResult" ("AiEngine");
CREATE INDEX "IX_AiTrackingResult_BrandMentioned" ON "AiTrackingResult" ("BrandMentioned");

COMMIT;

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- List all created tables
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Count total tables
SELECT COUNT(*) as total_tables
FROM information_schema.tables 
WHERE table_schema = 'public';

-- List all indexes
SELECT tablename, indexname 
FROM pg_indexes 
WHERE schemaname = 'public' 
ORDER BY tablename, indexname;

-- Verify foreign key constraints
SELECT
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
    AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_schema = 'public'
ORDER BY tc.table_name;
