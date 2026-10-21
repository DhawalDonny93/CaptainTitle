-- =====================================================
-- Database Migrations for Production Readiness
-- =====================================================
-- Execute these migrations in order
-- Last Updated: October 6, 2025

-- =====================================================
-- Migration 1: Account Lockout Feature
-- Task 1.1: Account Lockout After Failed Attempts
-- =====================================================

-- Add account lockout columns to User table
ALTER TABLE "User" 
ADD COLUMN IF NOT EXISTS "FailedLoginAttempts" INTEGER NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS "LockoutEnd" TIMESTAMP WITH TIME ZONE NULL;

-- Create index for lockout queries
CREATE INDEX IF NOT EXISTS "IX_User_LockoutEnd" ON "User"("LockoutEnd");

-- Add comment for documentation
COMMENT ON COLUMN "User"."FailedLoginAttempts" IS 'Number of consecutive failed login attempts';
COMMENT ON COLUMN "User"."LockoutEnd" IS 'DateTime when account lockout expires (NULL if not locked)';

-- Verify migration
SELECT 'Migration 1: Account Lockout - COMPLETED' AS status;

-- =====================================================
-- End of Migration 1
-- =====================================================

-- =====================================================
-- Migration 2: Session Timeout Feature
-- Task 1.2: Session Timeout Configuration
-- =====================================================

-- Add session timeout columns to UserSession table
ALTER TABLE "UserSession" 
ADD COLUMN IF NOT EXISTS "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (CURRENT_TIMESTAMP + INTERVAL '30 minutes'),
ADD COLUMN IF NOT EXISTS "TimeoutMinutes" INTEGER NOT NULL DEFAULT 30;

-- Create index for session expiration queries
CREATE INDEX IF NOT EXISTS "IX_UserSession_ExpiresAt" ON "UserSession"("ExpiresAt");

-- Add comment for documentation
COMMENT ON COLUMN "UserSession"."ExpiresAt" IS 'DateTime when session expires';
COMMENT ON COLUMN "UserSession"."TimeoutMinutes" IS 'Session timeout duration in minutes';

-- Verify migration
SELECT 'Migration 2: Session Timeout - COMPLETED' AS status;

-- =====================================================
-- End of Migration 2
-- =====================================================

-- =====================================================
-- Migration 3: Account Deletion (GDPR - Right to be Forgotten)
-- Task 2.2: Account Deletion
-- =====================================================

-- Add account deletion columns to User table
ALTER TABLE "User" 
ADD COLUMN IF NOT EXISTS "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "DeletedAt" TIMESTAMP WITH TIME ZONE NULL,
ADD COLUMN IF NOT EXISTS "ScheduledDeletionDate" TIMESTAMP WITH TIME ZONE NULL,
ADD COLUMN IF NOT EXISTS "DeletionReason" TEXT NULL;

-- Create index for scheduled deletions query
CREATE INDEX IF NOT EXISTS "IX_User_ScheduledDeletionDate" ON "User"("ScheduledDeletionDate") WHERE "ScheduledDeletionDate" IS NOT NULL;

-- Create index for deleted users
CREATE INDEX IF NOT EXISTS "IX_User_IsDeleted" ON "User"("IsDeleted");

-- Add comment for documentation
COMMENT ON COLUMN "User"."IsDeleted" IS 'Indicates if the user account has been deleted';
COMMENT ON COLUMN "User"."DeletedAt" IS 'DateTime when account was permanently deleted';
COMMENT ON COLUMN "User"."ScheduledDeletionDate" IS 'DateTime when account is scheduled for deletion (30-day grace period)';
COMMENT ON COLUMN "User"."DeletionReason" IS 'User-provided reason for account deletion';

-- Verify migration
SELECT 'Migration 3: Account Deletion - COMPLETED' AS status;

-- =====================================================
-- End of Migration 3
-- =====================================================

-- =====================================================
-- Migration 4: Data Export (GDPR - Right to Data Portability)
-- Task 2.1: GDPR Data Export
-- =====================================================

-- Create DataExport table
CREATE TABLE IF NOT EXISTS "DataExport" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "ExportId" VARCHAR(100) NOT NULL,
    "IsReady" BOOLEAN NOT NULL DEFAULT FALSE,
    "FilePath" VARCHAR(500) NULL,
    "FileSizeBytes" BIGINT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IsDownloaded" BOOLEAN NOT NULL DEFAULT FALSE,
    "DownloadedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "FK_DataExport_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_DataExport_UserId" ON "DataExport"("UserId");
CREATE INDEX IF NOT EXISTS "IX_DataExport_ExportId" ON "DataExport"("ExportId");
CREATE INDEX IF NOT EXISTS "IX_DataExport_ExpiresAt" ON "DataExport"("ExpiresAt");

-- Add comments
COMMENT ON TABLE "DataExport" IS 'Tracks user data export requests for GDPR compliance';
COMMENT ON COLUMN "DataExport"."ExportId" IS 'Unique identifier for the export request';
COMMENT ON COLUMN "DataExport"."IsReady" IS 'Indicates if the export file is ready for download';
COMMENT ON COLUMN "DataExport"."FilePath" IS 'Path to the generated export file';
COMMENT ON COLUMN "DataExport"."ExpiresAt" IS 'DateTime when the export link expires (24 hours)';

-- Verify migration
SELECT 'Migration 4: Data Export - COMPLETED' AS status;

-- =====================================================
-- End of Migration 4
-- =====================================================

-- =====================================================
-- Migration 5: Privacy Policy & Terms of Service
-- Task 2.3 & 2.4: Policy Acceptance Tracking
-- =====================================================

-- Create PolicyDocument table
CREATE TABLE IF NOT EXISTS "PolicyDocument" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Type" VARCHAR(50) NOT NULL,
    "Version" VARCHAR(20) NOT NULL,
    "Content" TEXT NOT NULL,
    "Summary" VARCHAR(500) NULL,
    "EffectiveDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create UserPolicyAcceptance table
CREATE TABLE IF NOT EXISTS "UserPolicyAcceptance" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "PolicyId" UUID NOT NULL,
    "AcceptedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IpAddress" VARCHAR(45) NOT NULL,
    "UserAgent" VARCHAR(500) NULL,
    "DeviceId" VARCHAR(50) NULL,
    CONSTRAINT "FK_UserPolicyAcceptance_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserPolicyAcceptance_Policy" FOREIGN KEY ("PolicyId") REFERENCES "PolicyDocument"("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_PolicyDocument_Type_IsActive" ON "PolicyDocument"("Type", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_PolicyDocument_EffectiveDate" ON "PolicyDocument"("EffectiveDate");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PolicyDocument_Type_Version" ON "PolicyDocument"("Type", "Version");
CREATE INDEX IF NOT EXISTS "IX_UserPolicyAcceptance_UserId" ON "UserPolicyAcceptance"("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserPolicyAcceptance_PolicyId" ON "UserPolicyAcceptance"("PolicyId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserPolicyAcceptance_User_Policy" ON "UserPolicyAcceptance"("UserId", "PolicyId");

-- Add comments
COMMENT ON TABLE "PolicyDocument" IS 'Stores privacy policy and terms of service documents with version control';
COMMENT ON TABLE "UserPolicyAcceptance" IS 'Tracks user acceptance of policies with IP address and timestamp';
COMMENT ON COLUMN "PolicyDocument"."Type" IS 'Policy type: Privacy or Terms';
COMMENT ON COLUMN "PolicyDocument"."Version" IS 'Policy version number (e.g., 1.0, 2.1)';
COMMENT ON COLUMN "UserPolicyAcceptance"."IpAddress" IS 'IP address from which policy was accepted';

-- Insert default policies (you should update these with your actual policies)
INSERT INTO "PolicyDocument" ("Type", "Version", "Content", "Summary", "EffectiveDate", "IsActive")
VALUES 
(
    'Privacy',
    '1.0',
    '<h1>Privacy Policy</h1><p>This is a placeholder privacy policy. Please update with your actual privacy policy.</p>',
    'Initial privacy policy',
    CURRENT_TIMESTAMP,
    TRUE
),
(
    'Terms',
    '1.0',
    '<h1>Terms of Service</h1><p>This is a placeholder terms of service. Please update with your actual terms.</p>',
    'Initial terms of service',
    CURRENT_TIMESTAMP,
    TRUE
)
ON CONFLICT DO NOTHING;

-- Verify migration
SELECT 'Migration 5: Privacy Policy & Terms - COMPLETED' AS status;

-- =====================================================
-- End of Migration 5
-- =====================================================

-- =====================================================
-- Migration 6: Age Verification (COPPA Compliance)
-- Task 2.5: Age Verification
-- =====================================================

-- Add age verification columns to User table
ALTER TABLE "User" 
ADD COLUMN IF NOT EXISTS "AgeVerified" BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "RequiresParentalConsent" BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "ParentalConsentGranted" BOOLEAN NOT NULL DEFAULT FALSE;

-- Create ParentalConsent table
CREATE TABLE IF NOT EXISTS "ParentalConsent" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "ParentEmail" VARCHAR(100) NOT NULL,
    "ParentName" VARCHAR(100) NULL,
    "ConsentToken" VARCHAR(100) NOT NULL,
    "IsGranted" BOOLEAN NOT NULL DEFAULT FALSE,
    "GrantedAt" TIMESTAMP WITH TIME ZONE NULL,
    "IpAddress" VARCHAR(45) NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT "FK_ParentalConsent_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_User_AgeVerified" ON "User"("AgeVerified");
CREATE INDEX IF NOT EXISTS "IX_User_RequiresParentalConsent" ON "User"("RequiresParentalConsent");
CREATE INDEX IF NOT EXISTS "IX_ParentalConsent_UserId" ON "ParentalConsent"("UserId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ParentalConsent_ConsentToken" ON "ParentalConsent"("ConsentToken");
CREATE INDEX IF NOT EXISTS "IX_ParentalConsent_ExpiresAt" ON "ParentalConsent"("ExpiresAt");

-- Add comments
COMMENT ON COLUMN "User"."AgeVerified" IS 'Indicates if user age has been verified';
COMMENT ON COLUMN "User"."RequiresParentalConsent" IS 'True if user is under 18 and needs parental consent';
COMMENT ON COLUMN "User"."ParentalConsentGranted" IS 'True if parental consent has been granted';
COMMENT ON TABLE "ParentalConsent" IS 'Tracks parental consent requests for COPPA compliance';
COMMENT ON COLUMN "ParentalConsent"."ConsentToken" IS 'Unique token for granting consent via email link';

-- Verify migration
SELECT 'Migration 6: Age Verification - COMPLETED' AS status;

-- =====================================================
-- End of Migration 6
-- =====================================================

-- =====================================================
-- Migration 7: Audit Logging
-- Task 2.6: Comprehensive Audit Trail
-- =====================================================

-- Create AuditLog table
CREATE TABLE IF NOT EXISTS "AuditLog" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NULL,
    "Action" VARCHAR(100) NOT NULL,
    "EntityType" VARCHAR(100) NOT NULL,
    "EntityId" UUID NULL,
    "Changes" TEXT NULL,
    "IpAddress" VARCHAR(45) NOT NULL,
    "UserAgent" VARCHAR(500) NULL,
    "DeviceId" VARCHAR(50) NULL,
    "AdditionalInfo" VARCHAR(500) NULL,
    "Success" BOOLEAN NOT NULL DEFAULT TRUE,
    "ErrorMessage" VARCHAR(500) NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_AuditLog_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE SET NULL
);

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS "IX_AuditLog_UserId" ON "AuditLog"("UserId");
CREATE INDEX IF NOT EXISTS "IX_AuditLog_Action" ON "AuditLog"("Action");
CREATE INDEX IF NOT EXISTS "IX_AuditLog_EntityType" ON "AuditLog"("EntityType");
CREATE INDEX IF NOT EXISTS "IX_AuditLog_EntityId" ON "AuditLog"("EntityId");
CREATE INDEX IF NOT EXISTS "IX_AuditLog_CreatedAt" ON "AuditLog"("CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_AuditLog_Success" ON "AuditLog"("Success");
CREATE INDEX IF NOT EXISTS "IX_AuditLog_EntityType_EntityId" ON "AuditLog"("EntityType", "EntityId");

-- Add comments
COMMENT ON TABLE "AuditLog" IS 'Comprehensive audit trail for all sensitive operations';
COMMENT ON COLUMN "AuditLog"."Action" IS 'Action performed (Login, Logout, Update, Delete, etc.)';
COMMENT ON COLUMN "AuditLog"."EntityType" IS 'Type of entity affected (User, Subscription, etc.)';
COMMENT ON COLUMN "AuditLog"."Changes" IS 'JSON representation of changes made';
COMMENT ON COLUMN "AuditLog"."Success" IS 'Whether the operation succeeded';

-- Verify migration
SELECT 'Migration 7: Audit Logging - COMPLETED' AS status;

-- =====================================================
-- End of Migration 7
-- =====================================================

-- =====================================================
-- Migration 8: Push Notifications (APNs)
-- Task 4.1: APNs Configuration & Device Token Management
-- =====================================================

-- Create DeviceToken table
CREATE TABLE IF NOT EXISTS "DeviceToken" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "Token" VARCHAR(500) NOT NULL,
    "Platform" VARCHAR(20) NOT NULL,
    "DeviceId" VARCHAR(50) NULL,
    "DeviceModel" VARCHAR(100) NULL,
    "AppVersion" VARCHAR(20) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastUsedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "FK_DeviceToken_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_DeviceToken_UserId" ON "DeviceToken"("UserId");
CREATE INDEX IF NOT EXISTS "IX_DeviceToken_Token" ON "DeviceToken"("Token");
CREATE INDEX IF NOT EXISTS "IX_DeviceToken_Platform" ON "DeviceToken"("Platform");
CREATE INDEX IF NOT EXISTS "IX_DeviceToken_IsActive" ON "DeviceToken"("IsActive");
CREATE INDEX IF NOT EXISTS "IX_DeviceToken_UserId_Platform" ON "DeviceToken"("UserId", "Platform");

-- Add comments
COMMENT ON TABLE "DeviceToken" IS 'Stores device tokens for push notifications (APNs/FCM)';
COMMENT ON COLUMN "DeviceToken"."Token" IS 'Device token for push notifications';
COMMENT ON COLUMN "DeviceToken"."Platform" IS 'Platform: iOS or Android';
COMMENT ON COLUMN "DeviceToken"."IsActive" IS 'Whether the token is currently active';

-- Verify migration
SELECT 'Migration 8: Push Notifications - COMPLETED' AS status;

-- =====================================================
-- End of Migration 8
-- =====================================================

-- =====================================================
-- Migration 9: Profile Photos & Notification Preferences
-- Profile Photo Upload & Notification Preferences Management
-- =====================================================

-- Add missing columns to User table
ALTER TABLE "User" 
ADD COLUMN IF NOT EXISTS "FirstName" VARCHAR(30) NULL,
ADD COLUMN IF NOT EXISTS "PhoneNumber" VARCHAR(20) NULL,
ADD COLUMN IF NOT EXISTS "Gender" VARCHAR(100) NULL,
ADD COLUMN IF NOT EXISTS "OnboardingCompleted" BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "ProfilePhotoUrl" VARCHAR(500) NULL,
ADD COLUMN IF NOT EXISTS "ProfilePhotoThumbnailUrl" VARCHAR(500) NULL;

-- Create NotificationPreferences table
CREATE TABLE IF NOT EXISTS "NotificationPreferences" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL UNIQUE,
    "PushEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "EmailEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "SmsEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "Marketing" BOOLEAN NOT NULL DEFAULT FALSE,
    "Updates" BOOLEAN NOT NULL DEFAULT TRUE,
    "Security" BOOLEAN NOT NULL DEFAULT TRUE,
    "Social" BOOLEAN NOT NULL DEFAULT TRUE,
    "Reminders" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_NotificationPreferences_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_NotificationPreferences_UserId" ON "NotificationPreferences"("UserId");

-- Add comments
COMMENT ON COLUMN "User"."ProfilePhotoUrl" IS 'URL to user profile photo (large version)';
COMMENT ON COLUMN "User"."ProfilePhotoThumbnailUrl" IS 'URL to user profile photo thumbnail';
COMMENT ON TABLE "NotificationPreferences" IS 'User notification preferences for push, email, and SMS';
COMMENT ON COLUMN "NotificationPreferences"."Security" IS 'Security notifications - always enabled, cannot be disabled';

-- Verify migration
SELECT 'Migration 9: Profile Photos & Notification Preferences - COMPLETED' AS status;

-- =====================================================
-- End of Migration 9
-- =====================================================

-- =====================================================
-- Migration 10: Scheduled Notifications
-- Background job processing for scheduled notification delivery
-- =====================================================

-- Create ScheduledNotification table
CREATE TABLE IF NOT EXISTS "ScheduledNotification" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Body" VARCHAR(500) NOT NULL,
    "Sound" VARCHAR(50) NULL DEFAULT 'default',
    "Badge" VARCHAR(10) NULL,
    "Category" VARCHAR(50) NULL,
    "DataJson" TEXT NULL,
    "ScheduledFor" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IsSent" BOOLEAN NOT NULL DEFAULT FALSE,
    "SentAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsCancelled" BOOLEAN NOT NULL DEFAULT FALSE,
    "CancelledAt" TIMESTAMP WITH TIME ZONE NULL,
    "ErrorMessage" VARCHAR(500) NULL,
    "RetryCount" INTEGER NOT NULL DEFAULT 0,
    "MaxRetries" INTEGER NOT NULL DEFAULT 3,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_ScheduledNotification_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS "IX_ScheduledNotification_UserId" ON "ScheduledNotification"("UserId");
CREATE INDEX IF NOT EXISTS "IX_ScheduledNotification_ScheduledFor" ON "ScheduledNotification"("ScheduledFor");
CREATE INDEX IF NOT EXISTS "IX_ScheduledNotification_IsSent" ON "ScheduledNotification"("IsSent");
CREATE INDEX IF NOT EXISTS "IX_ScheduledNotification_IsCancelled" ON "ScheduledNotification"("IsCancelled");
CREATE INDEX IF NOT EXISTS "IX_ScheduledNotification_Processing" ON "ScheduledNotification"("IsSent", "IsCancelled", "ScheduledFor", "RetryCount");

-- Add comments
COMMENT ON TABLE "ScheduledNotification" IS 'Scheduled notifications for future delivery';
COMMENT ON COLUMN "ScheduledNotification"."ScheduledFor" IS 'When the notification should be sent';
COMMENT ON COLUMN "ScheduledNotification"."IsSent" IS 'Whether the notification has been sent';
COMMENT ON COLUMN "ScheduledNotification"."RetryCount" IS 'Number of send attempts';
COMMENT ON COLUMN "ScheduledNotification"."MaxRetries" IS 'Maximum number of retry attempts';

-- Verify migration
SELECT 'Migration 10: Scheduled Notifications - COMPLETED' AS status;

-- =====================================================
-- End of Migration 10
-- =====================================================

-- =====================================================
-- Migration 11: In-App Notifications
-- Notification center for in-app notifications
-- =====================================================

-- Create InAppNotification table
CREATE TABLE IF NOT EXISTS "InAppNotification" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Body" VARCHAR(1000) NOT NULL,
    "Type" VARCHAR(50) NOT NULL DEFAULT 'info',
    "Category" VARCHAR(50) NULL,
    "ActionUrl" VARCHAR(500) NULL,
    "ActionText" VARCHAR(100) NULL,
    "ImageUrl" VARCHAR(500) NULL,
    "DataJson" TEXT NULL,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReadAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsArchived" BOOLEAN NOT NULL DEFAULT FALSE,
    "ArchivedAt" TIMESTAMP WITH TIME ZONE NULL,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_InAppNotification_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_UserId" ON "InAppNotification"("UserId");
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_IsRead" ON "InAppNotification"("IsRead");
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_IsArchived" ON "InAppNotification"("IsArchived");
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_CreatedAt" ON "InAppNotification"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_InAppNotification_UserReadArchived" ON "InAppNotification"("UserId", "IsRead", "IsArchived");

-- Add comments
COMMENT ON TABLE "InAppNotification" IS 'In-app notifications for notification center';
COMMENT ON COLUMN "InAppNotification"."Type" IS 'Notification type: info, success, warning, error';
COMMENT ON COLUMN "InAppNotification"."Priority" IS '0 = normal, 1 = high, 2 = urgent';
COMMENT ON COLUMN "InAppNotification"."ExpiresAt" IS 'When the notification expires and should not be shown';

-- Verify migration
SELECT 'Migration 11: In-App Notifications - COMPLETED' AS status;

-- =====================================================
-- End of Migration 11
-- =====================================================

-- =====================================================
-- Migration 12: User Roles (Admin Authentication)
-- Role-based access control for admin panel
-- =====================================================

-- Create UserRole table
CREATE TABLE IF NOT EXISTS "UserRole" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "Role" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserRole_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS "IX_UserRole_UserId" ON "UserRole"("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserRole_Role" ON "UserRole"("Role");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserRole_UserId_Role" ON "UserRole"("UserId", "Role");

-- Add comments
COMMENT ON TABLE "UserRole" IS 'User roles for role-based access control';
COMMENT ON COLUMN "UserRole"."Role" IS 'Role name: Admin, Moderator, User';

-- Verify migration
SELECT 'Migration 12: User Roles - COMPLETED' AS status;

-- =====================================================
-- End of Migration 12
-- =====================================================

-- =====================================================
-- Migration 13: System Settings
-- Dynamic system configuration management
-- =====================================================

-- Create SystemSetting table
CREATE TABLE IF NOT EXISTS "SystemSetting" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Key" VARCHAR(100) NOT NULL,
    "Value" TEXT NOT NULL,
    "Type" VARCHAR(50) NOT NULL DEFAULT 'string',
    "Category" VARCHAR(100) NOT NULL DEFAULT 'general',
    "Description" VARCHAR(500),
    "IsPublic" BOOLEAN NOT NULL DEFAULT false,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for efficient querying
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SystemSetting_Key" ON "SystemSetting"("Key");
CREATE INDEX IF NOT EXISTS "IX_SystemSetting_Category" ON "SystemSetting"("Category");
CREATE INDEX IF NOT EXISTS "IX_SystemSetting_IsPublic" ON "SystemSetting"("IsPublic");

-- Add comments
COMMENT ON TABLE "SystemSetting" IS 'System configuration settings';
COMMENT ON COLUMN "SystemSetting"."Key" IS 'Unique setting key';
COMMENT ON COLUMN "SystemSetting"."Type" IS 'Value type: string, int, bool, json';
COMMENT ON COLUMN "SystemSetting"."Category" IS 'Setting category: general, features, limits, email, notifications';
COMMENT ON COLUMN "SystemSetting"."IsPublic" IS 'Can be accessed by non-admin users';

-- Verify migration
SELECT 'Migration 13: System Settings - COMPLETED' AS status;

-- =====================================================
-- End of Migration 13
-- =====================================================

-- =====================================================
-- Migration 14: User Preferences
-- User UI and app behavior preferences
-- =====================================================

-- Create UserPreference table
CREATE TABLE IF NOT EXISTS "UserPreference" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "Language" VARCHAR(10) NOT NULL DEFAULT 'en',
    "Theme" VARCHAR(20) NOT NULL DEFAULT 'light',
    "Timezone" VARCHAR(50) NOT NULL DEFAULT 'UTC',
    "CustomPreferences" TEXT NOT NULL DEFAULT '{}',
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserPreference_User" FOREIGN KEY ("UserId") REFERENCES "User"("Id") ON DELETE CASCADE
);

-- Create unique index on UserId
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserPreference_UserId" ON "UserPreference"("UserId");

-- Add comments
COMMENT ON TABLE "UserPreference" IS 'User preferences for UI and app behavior';
COMMENT ON COLUMN "UserPreference"."Language" IS 'UI language code (en, es, fr, etc.)';
COMMENT ON COLUMN "UserPreference"."Theme" IS 'UI theme (light, dark, auto)';
COMMENT ON COLUMN "UserPreference"."Timezone" IS 'IANA timezone identifier';
COMMENT ON COLUMN "UserPreference"."CustomPreferences" IS 'JSON for additional custom preferences';

-- Verify migration
SELECT 'Migration 14: User Preferences - COMPLETED' AS status;

-- =====================================================
-- End of Migration 14
-- =====================================================
