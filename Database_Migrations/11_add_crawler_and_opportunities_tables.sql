-- ============================================================================
-- Migration 11: Add Site Crawler and Opportunities Tables
-- Description: Creates tables for site crawling, opportunity detection, and enhanced visibility tracking
-- Date: 2024
-- ============================================================================

-- ============================================================================
-- PART 1: EXTEND SITE AUDIT TABLES FOR MULTI-PAGE CRAWLING
-- ============================================================================

-- Extend SiteAuditRuns to support multi-page crawling
DO $$ 
BEGIN
    -- Add crawling-specific columns to existing SiteAuditRuns
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditRuns' AND column_name='IsCrawl') THEN
        ALTER TABLE "SiteAuditRuns" ADD COLUMN "IsCrawl" BOOLEAN NOT NULL DEFAULT FALSE;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditRuns' AND column_name='TotalPages') THEN
        ALTER TABLE "SiteAuditRuns" ADD COLUMN "TotalPages" INTEGER NOT NULL DEFAULT 1;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditRuns' AND column_name='SuccessfulPages') THEN
        ALTER TABLE "SiteAuditRuns" ADD COLUMN "SuccessfulPages" INTEGER NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditRuns' AND column_name='FailedPages') THEN
        ALTER TABLE "SiteAuditRuns" ADD COLUMN "FailedPages" INTEGER NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditRuns' AND column_name='BrokenLinks') THEN
        ALTER TABLE "SiteAuditRuns" ADD COLUMN "BrokenLinks" INTEGER NOT NULL DEFAULT 0;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditRuns' AND column_name='MaxDepth') THEN
        ALTER TABLE "SiteAuditRuns" ADD COLUMN "MaxDepth" INTEGER;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditRuns' AND column_name='MaxPages') THEN
        ALTER TABLE "SiteAuditRuns" ADD COLUMN "MaxPages" INTEGER;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditRuns' AND column_name='DurationSeconds') THEN
        ALTER TABLE "SiteAuditRuns" ADD COLUMN "DurationSeconds" INTEGER;
    END IF;
END $$;

-- Extend SiteAuditDetails to store crawled page data
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='PageUrl') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "PageUrl" VARCHAR(1000);
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='Depth') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "Depth" INTEGER;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='StatusCode') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "StatusCode" INTEGER;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='Title') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "Title" VARCHAR(500);
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='MetaDescription') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "MetaDescription" TEXT;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='WordCount') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "WordCount" INTEGER;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='HeadingsJson') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "HeadingsJson" TEXT;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='OutboundLinksJson') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "OutboundLinksJson" TEXT;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='ImagesJson') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "ImagesJson" TEXT;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='HasSchema') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "HasSchema" BOOLEAN;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='HasLlmsTxt') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "HasLlmsTxt" BOOLEAN;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='ContentHash') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "ContentHash" VARCHAR(64);
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SiteAuditDetails' AND column_name='MainContent') THEN
        ALTER TABLE "SiteAuditDetails" ADD COLUMN "MainContent" TEXT;
    END IF;
END $$;

-- Add indexes for new columns
CREATE INDEX IF NOT EXISTS "idx_site_audit_details_page_url" ON "SiteAuditDetails"("PageUrl");
CREATE INDEX IF NOT EXISTS "idx_site_audit_details_content_hash" ON "SiteAuditDetails"("ContentHash");

-- ============================================================================
-- PART 2: OPPORTUNITY DETECTION TABLES
-- ============================================================================

-- Content Gap Opportunities
CREATE TABLE IF NOT EXISTS "ContentGapOpportunities" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL REFERENCES "Projects"("Id") ON DELETE CASCADE,
    "Topic" VARCHAR(500) NOT NULL,
    "SuggestedPrompt" TEXT NOT NULL,
    "CompetitorCoverage" TEXT NOT NULL,
    "EstimatedImpact" INTEGER NOT NULL DEFAULT 50, -- 0-100
    "Priority" VARCHAR(20) NOT NULL DEFAULT 'Medium', -- High, Medium, Low
    "Reasoning" TEXT,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Open', -- Open, InProgress, Implemented, Dismissed
    "ImplementedAt" TIMESTAMP WITH TIME ZONE,
    "DiscoveredAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_content_gaps_project" ON "ContentGapOpportunities"("ProjectId");
CREATE INDEX IF NOT EXISTS "idx_content_gaps_status" ON "ContentGapOpportunities"("Status");
CREATE INDEX IF NOT EXISTS "idx_content_gaps_priority" ON "ContentGapOpportunities"("Priority");

-- SEO Opportunities
CREATE TABLE IF NOT EXISTS "SeoOpportunities" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL REFERENCES "Projects"("Id") ON DELETE CASCADE,
    "SiteAuditDetailId" INTEGER REFERENCES "SiteAuditDetails"("Id") ON DELETE SET NULL,
    "PageUrl" VARCHAR(1000) NOT NULL,
    "OpportunityType" VARCHAR(100) NOT NULL, -- ThinContent, MissingMeta, PoorHeadings, etc.
    "Priority" VARCHAR(20) NOT NULL, -- Critical, High, Medium, Low
    "CurrentState" TEXT,
    "RecommendedAction" TEXT NOT NULL,
    "EstimatedImpact" INTEGER NOT NULL, -- 0-100
    "DetailsJson" TEXT,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Open', -- Open, InProgress, Fixed, Dismissed
    "FixedAt" TIMESTAMP WITH TIME ZONE,
    "DiscoveredAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_seo_opportunities_project" ON "SeoOpportunities"("ProjectId");
CREATE INDEX IF NOT EXISTS "idx_seo_opportunities_status" ON "SeoOpportunities"("Status");
CREATE INDEX IF NOT EXISTS "idx_seo_opportunities_priority" ON "SeoOpportunities"("Priority");

-- AI Visibility Opportunities
CREATE TABLE IF NOT EXISTS "VisibilityOpportunities" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL REFERENCES "Projects"("Id") ON DELETE CASCADE,
    "PromptText" TEXT NOT NULL,
    "Platform" VARCHAR(50) NOT NULL, -- ChatGPT, Claude, Gemini, Perplexity
    "OpportunityType" VARCHAR(100) NOT NULL, -- NotMentioned, MentionedLate, NegativeMention, CompetitorDominance
    "CurrentPosition" INTEGER,
    "CompetitorsMentionedJson" TEXT, -- JSON array
    "Reasoning" TEXT,
    "RecommendedContent" TEXT,
    "OptimizationStrategy" TEXT,
    "Priority" INTEGER NOT NULL DEFAULT 3, -- 1-5
    "Status" VARCHAR(50) NOT NULL DEFAULT 'New', -- New, InProgress, Actioned, Dismissed
    "ActionedAt" TIMESTAMP WITH TIME ZONE,
    "DiscoveredAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_visibility_opportunities_project" ON "VisibilityOpportunities"("ProjectId");
CREATE INDEX IF NOT EXISTS "idx_visibility_opportunities_status" ON "VisibilityOpportunities"("Status");
CREATE INDEX IF NOT EXISTS "idx_visibility_opportunities_priority" ON "VisibilityOpportunities"("Priority");

-- ============================================================================
-- PART 3: ENHANCED VISIBILITY TRACKING TABLES
-- ============================================================================

-- Visibility Trends (Daily Aggregates)
CREATE TABLE IF NOT EXISTS "VisibilityTrends" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL REFERENCES "Projects"("Id") ON DELETE CASCADE,
    "TrendDate" DATE NOT NULL,
    "Platform" VARCHAR(50),
    "Topic" VARCHAR(200),
    "TotalPromptsChecked" INTEGER NOT NULL DEFAULT 0,
    "PromptsWithMention" INTEGER NOT NULL DEFAULT 0,
    "AverageVisibilityScore" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "AveragePosition" DECIMAL(5,2),
    "PositiveMentions" INTEGER NOT NULL DEFAULT 0,
    "NeutralMentions" INTEGER NOT NULL DEFAULT 0,
    "NegativeMentions" INTEGER NOT NULL DEFAULT 0,
    "CompetitorMentions" INTEGER NOT NULL DEFAULT 0,
    "ShareOfVoice" DECIMAL(5,2),
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE("ProjectId", "TrendDate", "Platform", "Topic")
);

CREATE INDEX IF NOT EXISTS "idx_visibility_trends_project_date" ON "VisibilityTrends"("ProjectId", "TrendDate");
CREATE INDEX IF NOT EXISTS "idx_visibility_trends_platform" ON "VisibilityTrends"("Platform");

-- Visibility Alerts
CREATE TABLE IF NOT EXISTS "VisibilityAlerts" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL REFERENCES "Projects"("Id") ON DELETE CASCADE,
    "AlertType" VARCHAR(100) NOT NULL, -- VisibilityDrop, NewCompetitor, NegativeMention, NotMentioned
    "Severity" VARCHAR(20) NOT NULL, -- Critical, Warning, Info
    "Message" TEXT NOT NULL,
    "PromptText" TEXT,
    "Platform" VARCHAR(50),
    "DetailsJson" TEXT,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReadAt" TIMESTAMP WITH TIME ZONE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_visibility_alerts_project" ON "VisibilityAlerts"("ProjectId");
CREATE INDEX IF NOT EXISTS "idx_visibility_alerts_is_read" ON "VisibilityAlerts"("IsRead");
CREATE INDEX IF NOT EXISTS "idx_visibility_alerts_severity" ON "VisibilityAlerts"("Severity");

-- ============================================================================
-- PART 4: LLM PROVIDER TRACKING TABLES
-- ============================================================================

-- LLM Provider Configurations
CREATE TABLE IF NOT EXISTS "LlmProviderConfigs" (
    "Id" SERIAL PRIMARY KEY,
    "Provider" VARCHAR(50) NOT NULL, -- OpenAI, Anthropic, Google, Perplexity
    "Model" VARCHAR(100) NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "MaxTokens" INTEGER,
    "Temperature" DECIMAL(3,2),
    "ConfigJson" TEXT, -- Additional provider-specific config
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE("Provider", "Model")
);

-- LLM Usage Logs
CREATE TABLE IF NOT EXISTS "LlmUsageLogs" (
    "Id" SERIAL PRIMARY KEY,
    "Provider" VARCHAR(50) NOT NULL,
    "Model" VARCHAR(100) NOT NULL,
    "RequestType" VARCHAR(50), -- PromptTest, CitationAnalysis, SentimentAnalysis, etc.
    "PromptTokens" INTEGER NOT NULL,
    "CompletionTokens" INTEGER NOT NULL,
    "TotalTokens" INTEGER NOT NULL,
    "Cost" DECIMAL(10,6),
    "LatencyMs" INTEGER,
    "ProjectId" INTEGER REFERENCES "Projects"("Id") ON DELETE SET NULL,
    "UserId" INTEGER REFERENCES "Users"("Id") ON DELETE SET NULL,
    "Success" BOOLEAN NOT NULL DEFAULT TRUE,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_llm_usage_logs_project" ON "LlmUsageLogs"("ProjectId");
CREATE INDEX IF NOT EXISTS "idx_llm_usage_logs_provider" ON "LlmUsageLogs"("Provider");
CREATE INDEX IF NOT EXISTS "idx_llm_usage_logs_created_at" ON "LlmUsageLogs"("CreatedAt");

-- Prompt Templates
CREATE TABLE IF NOT EXISTS "PromptTemplates" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL UNIQUE,
    "Category" VARCHAR(50) NOT NULL, -- Research, Visibility, Citation, Sentiment, etc.
    "Template" TEXT NOT NULL,
    "VariablesJson" TEXT, -- JSON array of variable names
    "Description" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "UsageCount" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_prompt_templates_category" ON "PromptTemplates"("Category");
CREATE INDEX IF NOT EXISTS "idx_prompt_templates_is_active" ON "PromptTemplates"("IsActive");

-- ============================================================================
-- PART 5: BACKGROUND JOB TRACKING
-- ============================================================================

-- Job Execution Logs
CREATE TABLE IF NOT EXISTS "JobExecutionLogs" (
    "Id" SERIAL PRIMARY KEY,
    "JobName" VARCHAR(100) NOT NULL,
    "JobType" VARCHAR(50) NOT NULL, -- VisibilityCheck, SiteCrawl, OpportunityDetection, etc.
    "Status" VARCHAR(50) NOT NULL, -- Running, Completed, Failed
    "ProjectId" INTEGER REFERENCES "Projects"("Id") ON DELETE SET NULL,
    "StartedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CompletedAt" TIMESTAMP WITH TIME ZONE,
    "DurationSeconds" INTEGER,
    "ItemsProcessed" INTEGER NOT NULL DEFAULT 0,
    "ItemsSucceeded" INTEGER NOT NULL DEFAULT 0,
    "ItemsFailed" INTEGER NOT NULL DEFAULT 0,
    "ErrorMessage" TEXT,
    "DetailsJson" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_job_execution_logs_job_name" ON "JobExecutionLogs"("JobName");
CREATE INDEX IF NOT EXISTS "idx_job_execution_logs_status" ON "JobExecutionLogs"("Status");
CREATE INDEX IF NOT EXISTS "idx_job_execution_logs_started_at" ON "JobExecutionLogs"("StartedAt");

-- ============================================================================
-- PART 6: PROJECT CONFIGURATION UPDATES
-- ============================================================================

-- Add new columns to Projects table for crawler and opportunity settings
DO $$ 
BEGIN
    -- Crawler settings
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Projects' AND column_name='EnableAutoCrawl') THEN
        ALTER TABLE "Projects" ADD COLUMN "EnableAutoCrawl" BOOLEAN NOT NULL DEFAULT FALSE;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Projects' AND column_name='CrawlMaxPages') THEN
        ALTER TABLE "Projects" ADD COLUMN "CrawlMaxPages" INTEGER DEFAULT 100;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Projects' AND column_name='CrawlMaxDepth') THEN
        ALTER TABLE "Projects" ADD COLUMN "CrawlMaxDepth" INTEGER DEFAULT 3;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Projects' AND column_name='CrawlSchedule') THEN
        ALTER TABLE "Projects" ADD COLUMN "CrawlSchedule" VARCHAR(50) DEFAULT 'Weekly';
    END IF;
    
    -- Opportunity detection settings
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Projects' AND column_name='EnableOpportunityDetection') THEN
        ALTER TABLE "Projects" ADD COLUMN "EnableOpportunityDetection" BOOLEAN NOT NULL DEFAULT TRUE;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Projects' AND column_name='CompetitorDomainsJson') THEN
        ALTER TABLE "Projects" ADD COLUMN "CompetitorDomainsJson" TEXT;
    END IF;
    
    -- Visibility tracking settings
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Projects' AND column_name='EnableVisibilityAlerts') THEN
        ALTER TABLE "Projects" ADD COLUMN "EnableVisibilityAlerts" BOOLEAN NOT NULL DEFAULT TRUE;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Projects' AND column_name='VisibilityCheckFrequency') THEN
        ALTER TABLE "Projects" ADD COLUMN "VisibilityCheckFrequency" VARCHAR(50) DEFAULT 'Hourly';
    END IF;
END $$;

-- ============================================================================
-- PART 7: SEED DEFAULT DATA
-- ============================================================================

-- Insert default LLM provider configurations
INSERT INTO "LlmProviderConfigs" ("Provider", "Model", "IsEnabled", "Priority", "MaxTokens", "Temperature")
VALUES 
    ('OpenAI', 'gpt-4-turbo-preview', TRUE, 1, 4096, 0.7),
    ('OpenAI', 'gpt-3.5-turbo', TRUE, 2, 4096, 0.7),
    ('Anthropic', 'claude-3-opus-20240229', TRUE, 3, 4096, 0.7),
    ('Anthropic', 'claude-3-sonnet-20240229', TRUE, 4, 4096, 0.7),
    ('Google', 'gemini-pro', TRUE, 5, 2048, 0.7)
ON CONFLICT ("Provider", "Model") DO NOTHING;

-- Insert prompt templates
INSERT INTO "PromptTemplates" ("Name", "Category", "Template", "VariablesJson", "Description", "IsActive")
VALUES 
    ('PromptResearchGeneration', 'Research', 'See PromptTemplates.cs', '["TOPIC", "INDUSTRY", "AUDIENCE", "BUSINESS_TYPE", "BRAND_NAME"]', 'Generate prompt research suggestions', TRUE),
    ('BrandMentionAnalysis', 'Visibility', 'See PromptTemplates.cs', '["RESPONSE_TEXT", "BRAND_NAME", "BRAND_DOMAIN", "PRODUCTS"]', 'Analyze brand mentions in AI responses', TRUE),
    ('CitationExtraction', 'Citation', 'See PromptTemplates.cs', '["RESPONSE_TEXT"]', 'Extract citations from AI responses', TRUE),
    ('SentimentAnalysis', 'Sentiment', 'See PromptTemplates.cs', '["TEXT"]', 'Analyze sentiment of text', TRUE),
    ('ContentGapAnalysis', 'Opportunity', 'See PromptTemplates.cs', '["YOUR_PAGES_SUMMARY", "COMPETITOR_PAGES_SUMMARY", "INDUSTRY", "KEYWORDS"]', 'Identify content gaps', TRUE),
    ('CommunityOpportunityAnalysis', 'Opportunity', 'See PromptTemplates.cs', '["PLATFORM", "TOPIC", "TITLE", "CONTENT", "UPVOTES", "COMMENTS", "BRAND_NAME", "INDUSTRY", "PRODUCTS", "EXPERTISE_AREAS"]', 'Analyze community engagement opportunities', TRUE)
ON CONFLICT ("Name") DO NOTHING;

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- Verify tables were created
DO $$
DECLARE
    table_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO table_count
    FROM information_schema.tables
    WHERE table_schema = 'public'
    AND table_name IN (
        'ContentGapOpportunities', 'SeoOpportunities', 'VisibilityOpportunities',
        'VisibilityTrends', 'VisibilityAlerts',
        'LlmProviderConfigs', 'LlmUsageLogs', 'PromptTemplates',
        'JobExecutionLogs'
    );
    
    RAISE NOTICE 'Migration 11 completed. Created/verified % tables.', table_count;
END $$;
