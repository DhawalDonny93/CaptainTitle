-- =====================================================
-- Mixpanel Analytics Database Schema
-- PostgreSQL with Pascal Case Naming Convention
-- =====================================================

-- Analytics Events Table
CREATE TABLE IF NOT EXISTS "AnalyticsEvents" (
    "Id" BIGSERIAL PRIMARY KEY,
    "EventId" VARCHAR(255) UNIQUE NOT NULL,
    "UserId" VARCHAR(255) NOT NULL,
    "EventName" VARCHAR(255) NOT NULL,
    "Properties" JSONB,
    "Timestamp" TIMESTAMP NOT NULL,
    "SessionId" VARCHAR(255),
    "Platform" VARCHAR(50),
    "AppVersion" VARCHAR(50),
    "MixpanelTracked" BOOLEAN DEFAULT false,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX "IX_AnalyticsEvents_EventId" ON "AnalyticsEvents"("EventId");
CREATE INDEX "IX_AnalyticsEvents_UserId" ON "AnalyticsEvents"("UserId");
CREATE INDEX "IX_AnalyticsEvents_EventName" ON "AnalyticsEvents"("EventName");
CREATE INDEX "IX_AnalyticsEvents_Timestamp" ON "AnalyticsEvents"("Timestamp");
CREATE INDEX "IX_AnalyticsEvents_SessionId" ON "AnalyticsEvents"("SessionId");
CREATE INDEX "IX_AnalyticsEvents_CreatedAt" ON "AnalyticsEvents"("CreatedAt");
CREATE INDEX "IX_AnalyticsEvents_MixpanelTracked" ON "AnalyticsEvents"("MixpanelTracked");

-- User Properties Table
CREATE TABLE IF NOT EXISTS "UserProperties" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(255) UNIQUE NOT NULL,
    "Properties" JSONB NOT NULL DEFAULT '{}',
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX "IX_UserProperties_UserId" ON "UserProperties"("UserId");

-- Revenue Events Table
CREATE TABLE IF NOT EXISTS "RevenueEvents" (
    "Id" BIGSERIAL PRIMARY KEY,
    "RevenueId" VARCHAR(255) UNIQUE NOT NULL,
    "UserId" VARCHAR(255) NOT NULL,
    "Amount" DECIMAL(10, 2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL DEFAULT 'USD',
    "ProductId" VARCHAR(255) NOT NULL,
    "TransactionId" VARCHAR(255) UNIQUE NOT NULL,
    "Properties" JSONB,
    "Timestamp" TIMESTAMP NOT NULL,
    "MixpanelTracked" BOOLEAN DEFAULT false,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX "IX_RevenueEvents_RevenueId" ON "RevenueEvents"("RevenueId");
CREATE UNIQUE INDEX "IX_RevenueEvents_TransactionId" ON "RevenueEvents"("TransactionId");
CREATE INDEX "IX_RevenueEvents_UserId" ON "RevenueEvents"("UserId");
CREATE INDEX "IX_RevenueEvents_Timestamp" ON "RevenueEvents"("Timestamp");
CREATE INDEX "IX_RevenueEvents_CreatedAt" ON "RevenueEvents"("CreatedAt");

-- Analytics Funnels Table
CREATE TABLE IF NOT EXISTS "AnalyticsFunnels" (
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

-- Analytics Cohorts Table
CREATE TABLE IF NOT EXISTS "AnalyticsCohorts" (
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

-- Analytics Exports Table
CREATE TABLE IF NOT EXISTS "AnalyticsExports" (
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
-- Verification Queries
-- =====================================================

-- Verify all tables were created
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN (
    'AnalyticsEvents',
    'UserProperties',
    'RevenueEvents',
    'AnalyticsFunnels',
    'AnalyticsCohorts',
    'AnalyticsExports'
)
ORDER BY table_name;

-- Check indexes
SELECT tablename, indexname 
FROM pg_indexes 
WHERE schemaname = 'public' 
AND tablename IN (
    'AnalyticsEvents',
    'UserProperties',
    'RevenueEvents',
    'AnalyticsFunnels',
    'AnalyticsCohorts',
    'AnalyticsExports'
)
ORDER BY tablename, indexname;

-- =====================================================
-- Optional: Data Cleanup Queries
-- =====================================================

-- Clean up old analytics events (older than 1 year)
-- DELETE FROM "AnalyticsEvents" 
-- WHERE "CreatedAt" < NOW() - INTERVAL '1 year';

-- Clean up expired exports
-- DELETE FROM "AnalyticsExports" 
-- WHERE "ExpiresAt" < NOW() AND "Status" = 'completed';

-- =====================================================
-- Sample Queries for Testing
-- =====================================================

-- Count events by event name
-- SELECT "EventName", COUNT(*) as "Count"
-- FROM "AnalyticsEvents"
-- GROUP BY "EventName"
-- ORDER BY "Count" DESC;

-- Get user properties
-- SELECT "UserId", "Properties"
-- FROM "UserProperties"
-- WHERE "UserId" = 'user_123';

-- Get revenue by user
-- SELECT "UserId", SUM("Amount") as "TotalRevenue", COUNT(*) as "Transactions"
-- FROM "RevenueEvents"
-- GROUP BY "UserId"
-- ORDER BY "TotalRevenue" DESC;
