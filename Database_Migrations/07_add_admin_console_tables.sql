-- Migration: Add Admin Console Enhancement Tables
-- Date: 2026-02-08
-- Description: Creates tables for admin audit logging, feature flags, system health monitoring, and email templates

-- ============================================
-- 1. AdminAuditLog Table
-- ============================================
CREATE TABLE "AdminAuditLog" (
    "Id" SERIAL PRIMARY KEY,
    "AdminUserId" INTEGER NOT NULL,
    "Action" VARCHAR(100) NOT NULL,
    "EntityType" VARCHAR(50),
    "EntityId" VARCHAR(100),
    "OldValue" TEXT,
    "NewValue" TEXT,
    "IpAddress" VARCHAR(50),
    "UserAgent" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_AdminAuditLog_User" FOREIGN KEY ("AdminUserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AdminAuditLog_AdminUserId" ON "AdminAuditLog"("AdminUserId");
CREATE INDEX "IX_AdminAuditLog_Action" ON "AdminAuditLog"("Action");
CREATE INDEX "IX_AdminAuditLog_EntityType" ON "AdminAuditLog"("EntityType");
CREATE INDEX "IX_AdminAuditLog_CreatedAt" ON "AdminAuditLog"("CreatedAt");

-- ============================================
-- 2. FeatureFlag Table
-- ============================================
CREATE TABLE "FeatureFlag" (
    "Id" SERIAL PRIMARY KEY,
    "FlagKey" VARCHAR(100) NOT NULL UNIQUE,
    "DisplayName" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "EnabledForUserIds" TEXT,
    "EnabledForRoles" TEXT,
    "RolloutPercentage" INTEGER DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "IX_FeatureFlag_IsEnabled" ON "FeatureFlag"("IsEnabled");
CREATE INDEX "IX_FeatureFlag_FlagKey" ON "FeatureFlag"("FlagKey");

-- ============================================
-- 3. SystemHealthMetrics Table
-- ============================================
CREATE TABLE "SystemHealthMetrics" (
    "Id" SERIAL PRIMARY KEY,
    "MetricDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CpuUsagePercent" DECIMAL(5,2),
    "MemoryUsageMb" BIGINT,
    "ActiveConnections" INTEGER,
    "RequestsPerMinute" INTEGER,
    "AverageResponseTimeMs" INTEGER,
    "ErrorRate" DECIMAL(5,2),
    "BackgroundJobsRunning" INTEGER,
    "BackgroundJobsFailed" INTEGER,
    "DatabaseConnectionPoolSize" INTEGER,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "IX_SystemHealthMetrics_MetricDate" ON "SystemHealthMetrics"("MetricDate");

-- ============================================
-- 4. EmailTemplate Table
-- ============================================
CREATE TABLE "EmailTemplate" (
    "Id" SERIAL PRIMARY KEY,
    "TemplateKey" VARCHAR(100) NOT NULL UNIQUE,
    "Name" VARCHAR(255) NOT NULL,
    "Subject" VARCHAR(500) NOT NULL,
    "HtmlBody" TEXT NOT NULL,
    "TextBody" TEXT,
    "Variables" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "IX_EmailTemplate_TemplateKey" ON "EmailTemplate"("TemplateKey");
CREATE INDEX "IX_EmailTemplate_IsActive" ON "EmailTemplate"("IsActive");

-- ============================================
-- 5. Update SystemSetting Table (if exists)
-- ============================================
-- Check if columns exist before adding
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='SystemSetting' AND column_name='DataType') THEN
        ALTER TABLE "SystemSetting" ADD COLUMN "DataType" VARCHAR(20) DEFAULT 'String';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='SystemSetting' AND column_name='ValidationRules') THEN
        ALTER TABLE "SystemSetting" ADD COLUMN "ValidationRules" TEXT;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='SystemSetting' AND column_name='DisplayOrder') THEN
        ALTER TABLE "SystemSetting" ADD COLUMN "DisplayOrder" INTEGER DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='SystemSetting' AND column_name='GroupName') THEN
        ALTER TABLE "SystemSetting" ADD COLUMN "GroupName" VARCHAR(100);
    END IF;
END $$;

-- ============================================
-- Migration Complete
-- ============================================
-- Tables Created: 4 new tables
-- Tables Updated: 1 (SystemSetting)
-- Indexes Created: 10 new indexes
-- Foreign Keys: 1
