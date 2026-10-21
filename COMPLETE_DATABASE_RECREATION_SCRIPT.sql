-- ============================================================================
-- COMPLETE DATABASE RECREATION SCRIPT - INTEGER IDs
-- ============================================================================
-- Date: January 10, 2026
-- Description: Drops all existing tables and recreates with INTEGER IDs
-- WARNING: THIS WILL DELETE ALL DATA - USE ONLY FOR FRESH SETUP
-- ============================================================================

BEGIN;

-- ============================================================================
-- STEP 1: DROP ALL EXISTING TABLES (in reverse dependency order)
-- ============================================================================

DROP TABLE IF EXISTS "WebNotification" CASCADE;
DROP TABLE IF EXISTS "LlmMessage" CASCADE;
DROP TABLE IF EXISTS "LlmConversation" CASCADE;
DROP TABLE IF EXISTS "LlmUsageQuota" CASCADE;
DROP TABLE IF EXISTS "LlmPromptTemplate" CASCADE;
DROP TABLE IF EXISTS "UserOnboardingAnswer" CASCADE;
DROP TABLE IF EXISTS "UserOnboardingProgress" CASCADE;
DROP TABLE IF EXISTS "UserOnboardingSync" CASCADE;
DROP TABLE IF EXISTS "OnboardingQuestion" CASCADE;
DROP TABLE IF EXISTS "OnboardingStep" CASCADE;
DROP TABLE IF EXISTS "ScheduledNotification" CASCADE;
DROP TABLE IF EXISTS "InAppNotification" CASCADE;
DROP TABLE IF EXISTS "NotificationPreference" CASCADE;
DROP TABLE IF EXISTS "DeviceToken" CASCADE;
DROP TABLE IF EXISTS "UserPolicyAcceptance" CASCADE;
DROP TABLE IF EXISTS "PolicyDocument" CASCADE;
DROP TABLE IF EXISTS "DataExport" CASCADE;
DROP TABLE IF EXISTS "ParentalConsent" CASCADE;
DROP TABLE IF EXISTS "AuditLog" CASCADE;
DROP TABLE IF EXISTS "UserPreference" CASCADE;
DROP TABLE IF EXISTS "UserRole" CASCADE;
DROP TABLE IF EXISTS "SystemSetting" CASCADE;
DROP TABLE IF EXISTS "RevenueCatWebhookEvent" CASCADE;
DROP TABLE IF EXISTS "UserRevenueCatMapping" CASCADE;
DROP TABLE IF EXISTS "PaywallEvent" CASCADE;
DROP TABLE IF EXISTS "Receipt" CASCADE;
DROP TABLE IF EXISTS "SubscriptionTransaction" CASCADE;
DROP TABLE IF EXISTS "Subscription" CASCADE;
DROP TABLE IF EXISTS "Product" CASCADE;
DROP TABLE IF EXISTS "DeviceRegistration" CASCADE;
DROP TABLE IF EXISTS "PasswordReset" CASCADE;
DROP TABLE IF EXISTS "OtpVerification" CASCADE;
DROP TABLE IF EXISTS "UserSession" CASCADE;
DROP TABLE IF EXISTS "RefreshToken" CASCADE;
DROP TABLE IF EXISTS "UserProvider" CASCADE;
DROP TABLE IF EXISTS "User" CASCADE;

-- ============================================================================
-- STEP 2: CREATE USER TABLE (Core table - INTEGER ID)
-- ============================================================================

CREATE TABLE "User" (
    "Id" SERIAL PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "FirstName" VARCHAR(100),
    "LastName" VARCHAR(100),
    "PhoneNumber" VARCHAR(20),
    "DateOfBirth" DATE,
    "ProfilePictureUrl" VARCHAR(500),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "EmailVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "PhoneVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP,
    "LastLoginAt" TIMESTAMP,
    "FailedLoginAttempts" INTEGER NOT NULL DEFAULT 0,
    "LockoutEnd" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "IX_User_Email" ON "User"("Email");
CREATE INDEX "IX_User_PhoneNumber" ON "User"("PhoneNumber");
CREATE INDEX "IX_User_IsActive" ON "User"("IsActive");
CREATE INDEX "IX_User_CreatedAt" ON "User"("CreatedAt");

-- ============================================================================
-- STEP 3: AUTHENTICATION & SESSION TABLES
-- ============================================================================

-- UserProvider (OAuth providers)
CREATE TABLE "UserProvider" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Provider" VARCHAR(50) NOT NULL,
    "ProviderId" VARCHAR(255) NOT NULL,
    "ProviderData" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserProvider_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserProvider_Provider_ProviderId" UNIQUE ("Provider", "ProviderId")
);

CREATE INDEX "IX_UserProvider_UserId" ON "UserProvider"("UserId");
CREATE INDEX "IX_UserProvider_Provider" ON "UserProvider"("Provider");

-- RefreshToken
CREATE TABLE "RefreshToken" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Token" VARCHAR(500) NOT NULL UNIQUE,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_RefreshToken_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_RefreshToken_UserId" ON "RefreshToken"("UserId");
CREATE INDEX "IX_RefreshToken_Token" ON "RefreshToken"("Token");

-- UserSession
CREATE TABLE "UserSession" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "DeviceId" VARCHAR(255),
    "Platform" VARCHAR(50),
    "DeviceModel" VARCHAR(100),
    "AppVersion" VARCHAR(50),
    "IpAddress" VARCHAR(45),
    "UserAgent" TEXT,
    "LastActiveAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "TimeoutMinutes" INTEGER NOT NULL DEFAULT 30,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserSession_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserSession_UserId" ON "UserSession"("UserId");
CREATE INDEX "IX_UserSession_DeviceId" ON "UserSession"("DeviceId");

-- OtpVerification
CREATE TABLE "OtpVerification" (
    "Id" SERIAL PRIMARY KEY,
    "Email" VARCHAR(255),
    "PhoneNumber" VARCHAR(20),
    "Otp" VARCHAR(10) NOT NULL,
    "SessionId" VARCHAR(255) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "IsUsed" BOOLEAN NOT NULL DEFAULT FALSE,
    "AttemptCount" INTEGER NOT NULL DEFAULT 0,
    "MaxAttempts" INTEGER NOT NULL DEFAULT 3
);

CREATE INDEX "IX_OtpVerification_SessionId" ON "OtpVerification"("SessionId");
CREATE INDEX "IX_OtpVerification_Email" ON "OtpVerification"("Email");

-- PasswordReset
CREATE TABLE "PasswordReset" (
    "Id" SERIAL PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL,
    "Token" VARCHAR(500) NOT NULL UNIQUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "IsUsed" BOOLEAN NOT NULL DEFAULT FALSE,
    "UserId" INTEGER,
    CONSTRAINT "FK_PasswordReset_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_PasswordReset_Token" ON "PasswordReset"("Token");
CREATE INDEX "IX_PasswordReset_Email" ON "PasswordReset"("Email");

-- DeviceRegistration
CREATE TABLE "DeviceRegistration" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "DeviceId" VARCHAR(255) NOT NULL,
    "PublicKey" TEXT NOT NULL,
    "DeviceName" VARCHAR(100),
    "DeviceType" VARCHAR(50),
    "RegisteredAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastUsedAt" TIMESTAMP,
    CONSTRAINT "FK_DeviceRegistration_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_DeviceRegistration_UserId_DeviceId" UNIQUE ("UserId", "DeviceId")
);

CREATE INDEX "IX_DeviceRegistration_UserId" ON "DeviceRegistration"("UserId");

-- ============================================================================
-- STEP 4: SUBSCRIPTION & PAYMENT TABLES
-- ============================================================================

-- Product
CREATE TABLE "Product" (
    "Id" VARCHAR(100) PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "Price" DECIMAL(10, 2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL DEFAULT 'USD',
    "Period" VARCHAR(50),
    "TrialPeriodDays" INTEGER,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "ProductType" VARCHAR(50),
    "Features" JSONB,
    "Metadata" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Subscription
CREATE TABLE "Subscription" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "ProductId" VARCHAR(100) NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "PurchaseToken" TEXT,
    "TransactionId" VARCHAR(255),
    "OriginalTransactionId" VARCHAR(255),
    "PurchaseDate" TIMESTAMP NOT NULL,
    "ExpiresDate" TIMESTAMP,
    "AutoRenewStatus" BOOLEAN NOT NULL DEFAULT TRUE,
    "CancellationDate" TIMESTAMP,
    "GracePeriodExpiresDate" TIMESTAMP,
    "BillingIssueDetectedAt" TIMESTAMP,
    "RevenueCatSubscriberId" VARCHAR(255),
    "Environment" VARCHAR(50),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Subscription_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Subscription_Product" FOREIGN KEY ("ProductId") 
        REFERENCES "Product"("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Subscription_UserId" ON "Subscription"("UserId");
CREATE INDEX "IX_Subscription_Status" ON "Subscription"("Status");
CREATE INDEX "IX_Subscription_ProductId" ON "Subscription"("ProductId");

-- SubscriptionTransaction
CREATE TABLE "SubscriptionTransaction" (
    "Id" SERIAL PRIMARY KEY,
    "SubscriptionId" INTEGER NOT NULL,
    "TransactionId" VARCHAR(255) NOT NULL UNIQUE,
    "Amount" DECIMAL(10, 2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL,
    "TransactionDate" TIMESTAMP NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SubscriptionTransaction_Subscription" FOREIGN KEY ("SubscriptionId") 
        REFERENCES "Subscription"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_SubscriptionTransaction_SubscriptionId" ON "SubscriptionTransaction"("SubscriptionId");
CREATE INDEX "IX_SubscriptionTransaction_TransactionId" ON "SubscriptionTransaction"("TransactionId");

-- Receipt
CREATE TABLE "Receipt" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "ReceiptData" TEXT NOT NULL,
    "IsVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "VerifiedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Receipt_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Receipt_UserId" ON "Receipt"("UserId");

-- PaywallEvent
CREATE TABLE "PaywallEvent" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER,
    "EventType" VARCHAR(100) NOT NULL,
    "PaywallId" VARCHAR(100),
    "DeviceId" VARCHAR(255),
    "Platform" VARCHAR(50),
    "EventData" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PaywallEvent_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_PaywallEvent_UserId" ON "PaywallEvent"("UserId");
CREATE INDEX "IX_PaywallEvent_EventType" ON "PaywallEvent"("EventType");

-- UserRevenueCatMapping
CREATE TABLE "UserRevenueCatMapping" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL UNIQUE,
    "RevenueCatId" VARCHAR(255) NOT NULL UNIQUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserRevenueCatMapping_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserRevenueCatMapping_RevenueCatId" ON "UserRevenueCatMapping"("RevenueCatId");

-- RevenueCatWebhookEvent
CREATE TABLE "RevenueCatWebhookEvent" (
    "Id" SERIAL PRIMARY KEY,
    "EventType" VARCHAR(100) NOT NULL,
    "AppUserId" VARCHAR(255) NOT NULL,
    "EventData" JSONB NOT NULL,
    "ProcessedAt" TIMESTAMP,
    "IsProcessed" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "IX_RevenueCatWebhookEvent_AppUserId" ON "RevenueCatWebhookEvent"("AppUserId");
CREATE INDEX "IX_RevenueCatWebhookEvent_IsProcessed" ON "RevenueCatWebhookEvent"("IsProcessed");

-- ============================================================================
-- STEP 5: ONBOARDING TABLES
-- ============================================================================

-- OnboardingStep
CREATE TABLE "OnboardingStep" (
    "Id" SERIAL PRIMARY KEY,
    "StepKey" VARCHAR(100) NOT NULL UNIQUE,
    "StepOrder" INTEGER NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "StepType" VARCHAR(50) NOT NULL,
    "IsRequired" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- OnboardingQuestion
CREATE TABLE "OnboardingQuestion" (
    "Id" SERIAL PRIMARY KEY,
    "OnboardingStepId" INTEGER NOT NULL,
    "QuestionKey" VARCHAR(100) NOT NULL UNIQUE,
    "QuestionText" TEXT NOT NULL,
    "QuestionType" VARCHAR(50) NOT NULL,
    "Options" JSONB,
    "IsRequired" BOOLEAN NOT NULL DEFAULT TRUE,
    "QuestionOrder" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_OnboardingQuestion_OnboardingStep" FOREIGN KEY ("OnboardingStepId") 
        REFERENCES "OnboardingStep"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_OnboardingQuestion_OnboardingStepId" ON "OnboardingQuestion"("OnboardingStepId");

-- UserOnboardingSync
CREATE TABLE "UserOnboardingSync" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Version" INTEGER NOT NULL DEFAULT 1,
    "IsCompleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CompletedAt" TIMESTAMP,
    "LastSyncAt" TIMESTAMP,
    "TotalSteps" INTEGER NOT NULL DEFAULT 0,
    "CompletedSteps" INTEGER NOT NULL DEFAULT 0,
    "TotalAnswers" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserOnboardingSync_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserOnboardingSync_UserId" ON "UserOnboardingSync"("UserId");

-- UserOnboardingProgress
CREATE TABLE "UserOnboardingProgress" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "OnboardingStepId" INTEGER,
    "UserOnboardingSyncId" INTEGER NOT NULL,
    "ClientStepId" VARCHAR(100),
    "ClientStepKey" VARCHAR(100),
    "ClientStepType" VARCHAR(50),
    "IsCompleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CompletedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserOnboardingProgress_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserOnboardingProgress_OnboardingStep" FOREIGN KEY ("OnboardingStepId") 
        REFERENCES "OnboardingStep"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_UserOnboardingProgress_UserOnboardingSync" FOREIGN KEY ("UserOnboardingSyncId") 
        REFERENCES "UserOnboardingSync"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserOnboardingProgress_UserId" ON "UserOnboardingProgress"("UserId");
CREATE INDEX "IX_UserOnboardingProgress_UserOnboardingSyncId" ON "UserOnboardingProgress"("UserOnboardingSyncId");

-- UserOnboardingAnswer
CREATE TABLE "UserOnboardingAnswer" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "OnboardingQuestionId" INTEGER,
    "UserOnboardingProgressId" INTEGER NOT NULL,
    "ClientQuestionId" VARCHAR(100),
    "ClientQuestionKey" VARCHAR(100),
    "AnswerValue" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserOnboardingAnswer_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserOnboardingAnswer_OnboardingQuestion" FOREIGN KEY ("OnboardingQuestionId") 
        REFERENCES "OnboardingQuestion"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_UserOnboardingAnswer_UserOnboardingProgress" FOREIGN KEY ("UserOnboardingProgressId") 
        REFERENCES "UserOnboardingProgress"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserOnboardingAnswer_UserId" ON "UserOnboardingAnswer"("UserId");
CREATE INDEX "IX_UserOnboardingAnswer_UserOnboardingProgressId" ON "UserOnboardingAnswer"("UserOnboardingProgressId");

-- ============================================================================
-- STEP 6: NOTIFICATION TABLES
-- ============================================================================

-- DeviceToken
CREATE TABLE "DeviceToken" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Token" TEXT NOT NULL,
    "Platform" VARCHAR(20) NOT NULL,
    "TokenType" VARCHAR(20) NOT NULL DEFAULT 'Native',
    "DeviceId" VARCHAR(255),
    "DeviceModel" VARCHAR(100),
    "AppVersion" VARCHAR(50),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "LastUsedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_DeviceToken_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_DeviceToken_Token" UNIQUE ("Token")
);

CREATE INDEX "IX_DeviceToken_UserId" ON "DeviceToken"("UserId");
CREATE INDEX "IX_DeviceToken_Platform" ON "DeviceToken"("Platform");

-- NotificationPreference
CREATE TABLE "NotificationPreference" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL UNIQUE,
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
    CONSTRAINT "FK_NotificationPreference_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

-- ScheduledNotification
CREATE TABLE "ScheduledNotification" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Body" TEXT NOT NULL,
    "Sound" VARCHAR(100),
    "Badge" INTEGER,
    "Category" VARCHAR(100),
    "Data" JSONB,
    "ScheduledFor" TIMESTAMP NOT NULL,
    "IsSent" BOOLEAN NOT NULL DEFAULT FALSE,
    "SentAt" TIMESTAMP,
    "IsCancelled" BOOLEAN NOT NULL DEFAULT FALSE,
    "CancelledAt" TIMESTAMP,
    "RetryCount" INTEGER NOT NULL DEFAULT 0,
    "MaxRetries" INTEGER NOT NULL DEFAULT 3,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_ScheduledNotification_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ScheduledNotification_UserId" ON "ScheduledNotification"("UserId");
CREATE INDEX "IX_ScheduledNotification_ScheduledFor" ON "ScheduledNotification"("ScheduledFor");

-- InAppNotification
CREATE TABLE "InAppNotification" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Body" TEXT NOT NULL,
    "Type" VARCHAR(50) NOT NULL,
    "Category" VARCHAR(100),
    "ActionUrl" VARCHAR(500),
    "ActionText" VARCHAR(100),
    "ImageUrl" VARCHAR(500),
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReadAt" TIMESTAMP,
    "IsArchived" BOOLEAN NOT NULL DEFAULT FALSE,
    "ArchivedAt" TIMESTAMP,
    "Priority" VARCHAR(20) NOT NULL DEFAULT 'Normal',
    "ExpiresAt" TIMESTAMP,
    "Data" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_InAppNotification_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_InAppNotification_UserId" ON "InAppNotification"("UserId");
CREATE INDEX "IX_InAppNotification_IsRead" ON "InAppNotification"("IsRead");
CREATE INDEX "IX_InAppNotification_CreatedAt" ON "InAppNotification"("CreatedAt" DESC);

-- WebNotification (New - for toast/banner notifications)
CREATE TABLE "WebNotification" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(100),
    "Message" TEXT NOT NULL,
    "Type" VARCHAR(20) NOT NULL DEFAULT 'info',
    "Icon" VARCHAR(50),
    "UserId" INTEGER,
    "IsGlobal" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsDismissible" BOOLEAN NOT NULL DEFAULT TRUE,
    "DurationMs" INTEGER DEFAULT 5000,
    "ActionUrl" VARCHAR(255),
    "ActionText" VARCHAR(50),
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP,
    "ReadAt" TIMESTAMP,
    CONSTRAINT "FK_WebNotification_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_WebNotification_UserId" ON "WebNotification"("UserId");
CREATE INDEX "IX_WebNotification_IsGlobal" ON "WebNotification"("IsGlobal");
CREATE INDEX "IX_WebNotification_CreatedAt" ON "WebNotification"("CreatedAt" DESC);

-- ============================================================================
-- STEP 7: COMPLIANCE & DATA PROTECTION TABLES
-- ============================================================================

-- PolicyDocument
CREATE TABLE "PolicyDocument" (
    "Id" SERIAL PRIMARY KEY,
    "PolicyType" VARCHAR(50) NOT NULL,
    "Version" VARCHAR(20) NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Content" TEXT NOT NULL,
    "EffectiveDate" TIMESTAMP NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "UQ_PolicyDocument_PolicyType_Version" UNIQUE ("PolicyType", "Version")
);

-- UserPolicyAcceptance
CREATE TABLE "UserPolicyAcceptance" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "PolicyId" INTEGER NOT NULL,
    "AcceptedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IpAddress" VARCHAR(45),
    "UserAgent" TEXT,
    "DeviceId" VARCHAR(255),
    CONSTRAINT "FK_UserPolicyAcceptance_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserPolicyAcceptance_PolicyDocument" FOREIGN KEY ("PolicyId") 
        REFERENCES "PolicyDocument"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserPolicyAcceptance_UserId_PolicyId" UNIQUE ("UserId", "PolicyId")
);

CREATE INDEX "IX_UserPolicyAcceptance_UserId" ON "UserPolicyAcceptance"("UserId");

-- ParentalConsent
CREATE TABLE "ParentalConsent" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "ParentEmail" VARCHAR(255) NOT NULL,
    "ParentName" VARCHAR(200),
    "ConsentToken" VARCHAR(500) NOT NULL UNIQUE,
    "IsGranted" BOOLEAN NOT NULL DEFAULT FALSE,
    "GrantedAt" TIMESTAMP,
    "IpAddress" VARCHAR(45),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    CONSTRAINT "FK_ParentalConsent_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ParentalConsent_UserId" ON "ParentalConsent"("UserId");
CREATE INDEX "IX_ParentalConsent_ConsentToken" ON "ParentalConsent"("ConsentToken");

-- DataExport
CREATE TABLE "DataExport" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "ExportId" VARCHAR(100) NOT NULL UNIQUE,
    "IsReady" BOOLEAN NOT NULL DEFAULT FALSE,
    "FilePath" VARCHAR(500),
    "FileSize" BIGINT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "DownloadedAt" TIMESTAMP,
    CONSTRAINT "FK_DataExport_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_DataExport_UserId" ON "DataExport"("UserId");
CREATE INDEX "IX_DataExport_ExportId" ON "DataExport"("ExportId");

-- ============================================================================
-- STEP 8: SYSTEM & AUDIT TABLES
-- ============================================================================

-- AuditLog
CREATE TABLE "AuditLog" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER,
    "Action" VARCHAR(100) NOT NULL,
    "EntityType" VARCHAR(100),
    "EntityId" INTEGER,
    "Changes" JSONB,
    "IpAddress" VARCHAR(45),
    "UserAgent" TEXT,
    "DeviceId" VARCHAR(255),
    "AdditionalInfo" JSONB,
    "Success" BOOLEAN NOT NULL DEFAULT TRUE,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_AuditLog_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_AuditLog_UserId" ON "AuditLog"("UserId");
CREATE INDEX "IX_AuditLog_Action" ON "AuditLog"("Action");
CREATE INDEX "IX_AuditLog_CreatedAt" ON "AuditLog"("CreatedAt" DESC);

-- UserRole
CREATE TABLE "UserRole" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Role" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserRole_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserRole_UserId_Role" UNIQUE ("UserId", "Role")
);

CREATE INDEX "IX_UserRole_UserId" ON "UserRole"("UserId");
CREATE INDEX "IX_UserRole_Role" ON "UserRole"("Role");

-- UserPreference
CREATE TABLE "UserPreference" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL UNIQUE,
    "Language" VARCHAR(10) NOT NULL DEFAULT 'en',
    "Theme" VARCHAR(20) NOT NULL DEFAULT 'light',
    "Timezone" VARCHAR(50) NOT NULL DEFAULT 'UTC',
    "CustomPreferences" JSONB,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserPreference_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

-- SystemSetting
CREATE TABLE "SystemSetting" (
    "Id" SERIAL PRIMARY KEY,
    "Key" VARCHAR(100) NOT NULL UNIQUE,
    "Value" TEXT NOT NULL,
    "Description" TEXT,
    "DataType" VARCHAR(50) NOT NULL DEFAULT 'String',
    "IsEncrypted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================================
-- STEP 9: LLM TABLES
-- ============================================================================

-- LlmConversation
CREATE TABLE "LlmConversation" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Title" VARCHAR(200),
    "Model" VARCHAR(100) NOT NULL,
    "TotalMessages" INTEGER NOT NULL DEFAULT 0,
    "TotalTokensUsed" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmConversation_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_LlmConversation_UserId" ON "LlmConversation"("UserId");

-- LlmMessage
CREATE TABLE "LlmMessage" (
    "Id" SERIAL PRIMARY KEY,
    "ConversationId" INTEGER NOT NULL,
    "Role" VARCHAR(20) NOT NULL,
    "Content" TEXT NOT NULL,
    "TokenCount" INTEGER NOT NULL DEFAULT 0,
    "Model" VARCHAR(100),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmMessage_LlmConversation" FOREIGN KEY ("ConversationId") 
        REFERENCES "LlmConversation"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_LlmMessage_ConversationId" ON "LlmMessage"("ConversationId");

-- LlmUsageQuota
CREATE TABLE "LlmUsageQuota" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "PeriodStart" TIMESTAMP NOT NULL,
    "PeriodEnd" TIMESTAMP NOT NULL,
    "RequestsUsed" INTEGER NOT NULL DEFAULT 0,
    "RequestsLimit" INTEGER NOT NULL,
    "TokensUsed" INTEGER NOT NULL DEFAULT 0,
    "TokensLimit" INTEGER NOT NULL,
    "ImagesUsed" INTEGER NOT NULL DEFAULT 0,
    "ImagesLimit" INTEGER NOT NULL,
    "RateLimitPerMinute" INTEGER NOT NULL DEFAULT 10,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_LlmUsageQuota_User" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_LlmUsageQuota_UserId" ON "LlmUsageQuota"("UserId");

-- LlmPromptTemplate
CREATE TABLE "LlmPromptTemplate" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL UNIQUE,
    "Description" TEXT,
    "Template" TEXT NOT NULL,
    "Category" VARCHAR(50),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================================
-- STEP 10: INSERT SAMPLE DATA
-- ============================================================================

-- Insert default admin user
INSERT INTO "User" ("Email", "FirstName", "LastName", "IsActive", "EmailVerified", "CreatedAt", "UpdatedAt")
VALUES ('admin@example.com', 'Admin', 'User', TRUE, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert admin role
INSERT INTO "UserRole" ("UserId", "Role", "CreatedAt", "UpdatedAt")
VALUES (1, 'Admin', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert sample products
INSERT INTO "Product" ("Id", "Name", "Description", "Price", "Currency", "Period", "IsActive", "CreatedAt", "UpdatedAt")
VALUES 
    ('monthly_premium', 'Premium Monthly', 'Monthly premium subscription', 9.99, 'USD', 'monthly', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('yearly_premium', 'Premium Yearly', 'Yearly premium subscription', 99.99, 'USD', 'yearly', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert sample policy documents
INSERT INTO "PolicyDocument" ("PolicyType", "Version", "Title", "Content", "EffectiveDate", "IsActive", "CreatedAt")
VALUES 
    ('PrivacyPolicy', '1.0', 'Privacy Policy', 'Privacy policy content...', CURRENT_TIMESTAMP, TRUE, CURRENT_TIMESTAMP),
    ('TermsOfService', '1.0', 'Terms of Service', 'Terms of service content...', CURRENT_TIMESTAMP, TRUE, CURRENT_TIMESTAMP);

-- Insert sample global notification
INSERT INTO "WebNotification" ("Title", "Message", "Type", "IsGlobal", "IsDismissible", "DurationMs", "CreatedAt")
VALUES ('Welcome', 'Welcome to the platform!', 'info', TRUE, TRUE, 10000, CURRENT_TIMESTAMP);

COMMIT;

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- Count all tables
SELECT 
    schemaname,
    tablename,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = tablename) as column_count
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY tablename;

-- Verify User table
SELECT * FROM "User" LIMIT 5;

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
ORDER BY tc.table_name, kcu.column_name;

-- ============================================================================
-- END OF SCRIPT
-- ============================================================================
