-- ============================================================================
-- COMPLETE AI SEO PLATFORM DATABASE SCHEMA
-- Date: 2026-02-09
-- Description: Complete schema for all AI SEO platform tables
-- Instructions: Drop all existing tables and run this script to recreate clean schema
-- ============================================================================

-- Drop all tables in correct order (respecting foreign keys)
DROP TABLE IF EXISTS "SiteAuditDetails" CASCADE;
DROP TABLE IF EXISTS "SiteAuditRuns" CASCADE;
DROP TABLE IF EXISTS "SiteAuditHistories" CASCADE;
DROP TABLE IF EXISTS "SiteAudits" CASCADE;
DROP TABLE IF EXISTS "PromptVisibilityMetrics" CASCADE;
DROP TABLE IF EXISTS "PromptPlatformTrackings" CASCADE;
DROP TABLE IF EXISTS "PromptResearchDatas" CASCADE;
DROP TABLE IF EXISTS "PromptTrackings" CASCADE;
DROP TABLE IF EXISTS "CitationAggregations" CASCADE;
DROP TABLE IF EXISTS "CitationMetrics" CASCADE;
DROP TABLE IF EXISTS "BrandMentions" CASCADE;
DROP TABLE IF EXISTS "Citations" CASCADE;
DROP TABLE IF EXISTS "OpportunityImpacts" CASCADE;
DROP TABLE IF EXISTS "CompetitorAnalyses" CASCADE;
DROP TABLE IF EXISTS "ContentGapOpportunities" CASCADE;
DROP TABLE IF EXISTS "OpportunityScans" CASCADE;
DROP TABLE IF EXISTS "CompetitiveIntelligence" CASCADE;
DROP TABLE IF EXISTS "VisibilityAlerts" CASCADE;
DROP TABLE IF EXISTS "VisibilityGoals" CASCADE;
DROP TABLE IF EXISTS "VisibilityDashboardMetrics" CASCADE;
DROP TABLE IF EXISTS "CommunityOpportunities" CASCADE;
DROP TABLE IF EXISTS "UserTasks" CASCADE;
DROP TABLE IF EXISTS "Notifications" CASCADE;
DROP TABLE IF EXISTS "AiTrackingResults" CASCADE;
DROP TABLE IF EXISTS "AiTrackingPrompts" CASCADE;
DROP TABLE IF EXISTS "ContentArticles" CASCADE;
DROP TABLE IF EXISTS "Keywords" CASCADE;
DROP TABLE IF EXISTS "Projects" CASCADE;

BEGIN;

-- ============================================================================
-- CORE TABLES
-- ============================================================================

-- Projects Table
CREATE TABLE "Projects" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Domain" VARCHAR(255),
    "BrandVoice" TEXT,
    "TargetAudience" TEXT,
    "CmsCredentials" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_Projects_Users_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Projects_UserId" ON "Projects"("UserId");
CREATE INDEX "IX_Projects_IsActive" ON "Projects"("IsActive");
CREATE INDEX "IX_Projects_CreatedAt" ON "Projects"("CreatedAt");

-- Keywords Table
CREATE TABLE "Keywords" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Text" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Keywords_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Keywords_ProjectId" ON "Keywords"("ProjectId");
CREATE INDEX "IX_Keywords_Text" ON "Keywords"("Text");

-- ============================================================================
-- CONTENT MANAGEMENT
-- ============================================================================

-- ContentArticles Table
CREATE TABLE "ContentArticles" (
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
    "ScheduledPublishAt" TIMESTAMP WITH TIME ZONE,
    "PublishedAt" TIMESTAMP WITH TIME ZONE,
    "WordCount" INTEGER NOT NULL DEFAULT 0,
    "AiModelUsed" VARCHAR(100),
    "GenerationPrompt" TEXT,
    "SeoScore" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_ContentArticles_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ContentArticles_ProjectId" ON "ContentArticles"("ProjectId");
CREATE INDEX "IX_ContentArticles_Status" ON "ContentArticles"("Status");
CREATE INDEX "IX_ContentArticles_PublishedAt" ON "ContentArticles"("PublishedAt");
CREATE INDEX "IX_ContentArticles_CreatedAt" ON "ContentArticles"("CreatedAt");

-- ============================================================================
-- AI TRACKING
-- ============================================================================

-- AiTrackingPrompts Table
CREATE TABLE "AiTrackingPrompts" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "PromptText" TEXT NOT NULL,
    "KeywordId" INTEGER,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_AiTrackingPrompts_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AiTrackingPrompts_Keywords_KeywordId" FOREIGN KEY ("KeywordId") 
        REFERENCES "Keywords"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_AiTrackingPrompts_ProjectId" ON "AiTrackingPrompts"("ProjectId");
CREATE INDEX "IX_AiTrackingPrompts_KeywordId" ON "AiTrackingPrompts"("KeywordId");
CREATE INDEX "IX_AiTrackingPrompts_IsActive" ON "AiTrackingPrompts"("IsActive");

-- AiTrackingResults Table
CREATE TABLE "AiTrackingResults" (
    "Id" SERIAL PRIMARY KEY,
    "PromptId" INTEGER NOT NULL,
    "AiEngine" VARCHAR(50) NOT NULL,
    "CountryCode" VARCHAR(10),
    "ResponseText" TEXT,
    "BrandMentioned" BOOLEAN NOT NULL DEFAULT FALSE,
    "MentionPosition" INTEGER,
    "VisibilityScore" INTEGER NOT NULL DEFAULT 0,
    "Sentiment" VARCHAR(20),
    "SentimentScore" DOUBLE PRECISION,
    "ExecutedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_AiTrackingResults_AiTrackingPrompts_PromptId" FOREIGN KEY ("PromptId") 
        REFERENCES "AiTrackingPrompts"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AiTrackingResults_PromptId" ON "AiTrackingResults"("PromptId");
CREATE INDEX "IX_AiTrackingResults_AiEngine" ON "AiTrackingResults"("AiEngine");
CREATE INDEX "IX_AiTrackingResults_ExecutedAt" ON "AiTrackingResults"("ExecutedAt");
CREATE INDEX "IX_AiTrackingResults_BrandMentioned" ON "AiTrackingResults"("BrandMentioned");

-- ============================================================================
-- PROMPT MANAGEMENT
-- ============================================================================

-- PromptTrackings Table
CREATE TABLE "PromptTrackings" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "PromptText" TEXT NOT NULL,
    "Topic" VARCHAR(255),
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Active',
    "CreatedByUserId" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "LastCheckedAt" TIMESTAMP WITH TIME ZONE,
    "AverageVisibilityScore" DOUBLE PRECISION,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PromptTrackings_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PromptTrackings_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PromptTrackings_ProjectId" ON "PromptTrackings"("ProjectId");
CREATE INDEX "IX_PromptTrackings_Status" ON "PromptTrackings"("Status");
CREATE INDEX "IX_PromptTrackings_IsActive" ON "PromptTrackings"("IsActive");
CREATE INDEX "IX_PromptTrackings_CreatedByUserId" ON "PromptTrackings"("CreatedByUserId");

-- PromptPlatformTrackings Table
CREATE TABLE "PromptPlatformTrackings" (
    "Id" SERIAL PRIMARY KEY,
    "PromptId" INTEGER NOT NULL,
    "Platform" VARCHAR(100) NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "LastCheckDate" TIMESTAMP WITH TIME ZONE,
    "CurrentVisibilityScore" DOUBLE PRECISION,
    "BrandMentioned" BOOLEAN,
    "MentionPosition" INTEGER,
    "ResponseSummary" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PromptPlatformTrackings_PromptTrackings_PromptId" FOREIGN KEY ("PromptId") 
        REFERENCES "PromptTrackings"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PromptPlatformTrackings_PromptId" ON "PromptPlatformTrackings"("PromptId");
CREATE INDEX "IX_PromptPlatformTrackings_Platform" ON "PromptPlatformTrackings"("Platform");
CREATE INDEX "IX_PromptPlatformTrackings_IsEnabled" ON "PromptPlatformTrackings"("IsEnabled");

-- PromptVisibilityMetrics Table
CREATE TABLE "PromptVisibilityMetrics" (
    "Id" SERIAL PRIMARY KEY,
    "PromptPlatformId" INTEGER NOT NULL,
    "CheckDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "VisibilityScore" DOUBLE PRECISION NOT NULL DEFAULT 0,
    "Position" INTEGER,
    "BrandMentioned" BOOLEAN NOT NULL DEFAULT FALSE,
    "Sentiment" VARCHAR(20) NOT NULL DEFAULT 'Neutral',
    "SentimentScore" DOUBLE PRECISION,
    "ResponseText" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PromptVisibilityMetrics_PromptPlatformTrackings_PromptPlatformId" FOREIGN KEY ("PromptPlatformId") 
        REFERENCES "PromptPlatformTrackings"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PromptVisibilityMetrics_PromptPlatformId" ON "PromptVisibilityMetrics"("PromptPlatformId");
CREATE INDEX "IX_PromptVisibilityMetrics_CheckDate" ON "PromptVisibilityMetrics"("CheckDate");
CREATE INDEX "IX_PromptVisibilityMetrics_PromptPlatformId_CheckDate" ON "PromptVisibilityMetrics"("PromptPlatformId", "CheckDate");

-- PromptResearchDatas Table (Note: EF Core expects plural name)
CREATE TABLE "PromptResearchDatas" (
    "Id" SERIAL PRIMARY KEY,
    "PromptText" TEXT NOT NULL,
    "Topic" VARCHAR(200),
    "EstimatedVolume" INTEGER,
    "Difficulty" VARCHAR(20) NOT NULL DEFAULT 'Medium',
    "RelevanceScore" DOUBLE PRECISION,
    "Source" VARCHAR(50) NOT NULL DEFAULT 'Manual',
    "DiscoveredAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "IX_PromptResearchDatas_Topic" ON "PromptResearchDatas"("Topic");
CREATE INDEX "IX_PromptResearchDatas_Source" ON "PromptResearchDatas"("Source");
CREATE INDEX "IX_PromptResearchDatas_DiscoveredAt" ON "PromptResearchDatas"("DiscoveredAt");

-- ============================================================================
-- CITATIONS & MENTIONS
-- ============================================================================

-- Citations Table
CREATE TABLE "Citations" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "SourceUrl" TEXT NOT NULL,
    "SourceDomain" VARCHAR(255) NOT NULL,
    "SourceTitle" VARCHAR(500) NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "PromptText" TEXT,
    "Position" INTEGER,
    "CitationType" VARCHAR(50) NOT NULL DEFAULT 'DirectLink',
    "Context" TEXT,
    "DiscoveredAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Citations_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Citations_ProjectId" ON "Citations"("ProjectId");
CREATE INDEX "IX_Citations_SourceDomain" ON "Citations"("SourceDomain");
CREATE INDEX "IX_Citations_Platform" ON "Citations"("Platform");
CREATE INDEX "IX_Citations_DiscoveredAt" ON "Citations"("DiscoveredAt");
CREATE INDEX "IX_Citations_ProjectId_SourceUrl_Platform" ON "Citations"("ProjectId", "SourceUrl", "Platform");

-- CitationMetrics Table (Per-Citation Tracking)
CREATE TABLE "CitationMetrics" (
    "Id" SERIAL PRIMARY KEY,
    "CitationId" INTEGER NOT NULL,
    "CheckDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Position" INTEGER NOT NULL,
    "IsVisible" BOOLEAN NOT NULL DEFAULT TRUE,
    "ResponseContext" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CitationMetrics_Citations_CitationId" FOREIGN KEY ("CitationId") 
        REFERENCES "Citations"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_CitationMetrics_CitationId" ON "CitationMetrics"("CitationId");
CREATE INDEX "IX_CitationMetrics_CheckDate" ON "CitationMetrics"("CheckDate");
CREATE INDEX "IX_CitationMetrics_CitationId_CheckDate" ON "CitationMetrics"("CitationId", "CheckDate");

-- BrandMentions Table (Tracking-Focused)
CREATE TABLE "BrandMentions" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "PromptText" TEXT NOT NULL,
    "IsMentioned" BOOLEAN NOT NULL,
    "Position" INTEGER,
    "MentionType" VARCHAR(50) NOT NULL DEFAULT 'Direct',
    "MentionContext" TEXT,
    "Sentiment" VARCHAR(20) NOT NULL DEFAULT 'Neutral',
    "SentimentScore" DOUBLE PRECISION,
    "ResponseText" TEXT,
    "CheckDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_BrandMentions_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_BrandMentions_ProjectId" ON "BrandMentions"("ProjectId");
CREATE INDEX "IX_BrandMentions_Platform" ON "BrandMentions"("Platform");
CREATE INDEX "IX_BrandMentions_CheckDate" ON "BrandMentions"("CheckDate");
CREATE INDEX "IX_BrandMentions_IsMentioned" ON "BrandMentions"("IsMentioned");
CREATE INDEX "IX_BrandMentions_ProjectId_Platform_CheckDate" ON "BrandMentions"("ProjectId", "Platform", "CheckDate");

-- CitationAggregations Table (Domain/Platform Based)
CREATE TABLE "CitationAggregations" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "SourceDomain" VARCHAR(255) NOT NULL,
    "Platform" VARCHAR(50) NOT NULL,
    "AggregationDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "TotalCitations" INTEGER NOT NULL,
    "UniqueCitations" INTEGER NOT NULL,
    "AveragePosition" DOUBLE PRECISION NOT NULL,
    "TopPosition" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CitationAggregations_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_CitationAggregations_ProjectId" ON "CitationAggregations"("ProjectId");
CREATE INDEX "IX_CitationAggregations_SourceDomain" ON "CitationAggregations"("SourceDomain");
CREATE INDEX "IX_CitationAggregations_Platform" ON "CitationAggregations"("Platform");
CREATE INDEX "IX_CitationAggregations_AggregationDate" ON "CitationAggregations"("AggregationDate");
CREATE UNIQUE INDEX "IX_CitationAggregations_ProjectId_SourceDomain_Platform_AggregationDate" ON "CitationAggregations"("ProjectId", "SourceDomain", "Platform", "AggregationDate");

-- ============================================================================
-- COMMUNITY OPPORTUNITIES
-- ============================================================================

-- CommunityOpportunities Table
CREATE TABLE "CommunityOpportunities" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "KeywordId" INTEGER,
    "Platform" VARCHAR(50) NOT NULL,
    "Title" VARCHAR(300) NOT NULL,
    "Url" TEXT NOT NULL,
    "Summary" TEXT,
    "Sentiment" VARCHAR(50),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'New',
    "DraftReply" TEXT,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "EngagementScore" INTEGER NOT NULL DEFAULT 0,
    "Upvotes" INTEGER NOT NULL DEFAULT 0,
    "Comments" INTEGER NOT NULL DEFAULT 0,
    "Views" INTEGER NOT NULL DEFAULT 0,
    "PostedAt" TIMESTAMP WITH TIME ZONE,
    "LastCheckedAt" TIMESTAMP WITH TIME ZONE,
    "IsArchived" BOOLEAN NOT NULL DEFAULT FALSE,
    "AssignedToUserId" INTEGER,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CommunityOpportunities_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommunityOpportunities_Keywords_KeywordId" FOREIGN KEY ("KeywordId") 
        REFERENCES "Keywords"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_CommunityOpportunities_ProjectId" ON "CommunityOpportunities"("ProjectId");
CREATE INDEX "IX_CommunityOpportunities_Platform" ON "CommunityOpportunities"("Platform");
CREATE INDEX "IX_CommunityOpportunities_Status" ON "CommunityOpportunities"("Status");
CREATE INDEX "IX_CommunityOpportunities_Priority" ON "CommunityOpportunities"("Priority");
CREATE INDEX "IX_CommunityOpportunities_EngagementScore" ON "CommunityOpportunities"("EngagementScore");
CREATE INDEX "IX_CommunityOpportunities_IsArchived" ON "CommunityOpportunities"("IsArchived");
CREATE INDEX "IX_CommunityOpportunities_CreatedAt" ON "CommunityOpportunities"("CreatedAt");

-- ============================================================================
-- SITE AUDITOR
-- ============================================================================

-- SiteAudits Table
CREATE TABLE "SiteAudits" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "AuditDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "OverallScore" INTEGER NOT NULL DEFAULT 0,
    "IssuesFound" INTEGER NOT NULL DEFAULT 0,
    "ReportUrl" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAudits_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_SiteAudits_ProjectId" ON "SiteAudits"("ProjectId");
CREATE INDEX "IX_SiteAudits_AuditDate" ON "SiteAudits"("AuditDate");

-- SiteAuditRuns Table
CREATE TABLE "SiteAuditRuns" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Url" TEXT NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "TotalChecks" INTEGER NOT NULL DEFAULT 0,
    "PassedChecks" INTEGER NOT NULL DEFAULT 0,
    "FailedChecks" INTEGER NOT NULL DEFAULT 0,
    "WarningChecks" INTEGER NOT NULL DEFAULT 0,
    "OverallScore" INTEGER NOT NULL DEFAULT 0,
    "StartedAt" TIMESTAMP WITH TIME ZONE,
    "CompletedAt" TIMESTAMP WITH TIME ZONE,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditRuns_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_SiteAuditRuns_ProjectId" ON "SiteAuditRuns"("ProjectId");
CREATE INDEX "IX_SiteAuditRuns_Status" ON "SiteAuditRuns"("Status");
CREATE INDEX "IX_SiteAuditRuns_CreatedAt" ON "SiteAuditRuns"("CreatedAt");

-- SiteAuditDetails Table
CREATE TABLE "SiteAuditDetails" (
    "Id" SERIAL PRIMARY KEY,
    "SiteAuditRunId" INTEGER NOT NULL,
    "CheckName" VARCHAR(255) NOT NULL,
    "CheckType" VARCHAR(100) NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Info',
    "Score" INTEGER NOT NULL DEFAULT 0,
    "Message" TEXT,
    "DetailsJson" TEXT,
    "Recommendation" TEXT,
    "AutoFixAvailable" BOOLEAN NOT NULL DEFAULT FALSE,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditDetails_SiteAuditRuns_SiteAuditRunId" FOREIGN KEY ("SiteAuditRunId") 
        REFERENCES "SiteAuditRuns"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_SiteAuditDetails_SiteAuditRunId" ON "SiteAuditDetails"("SiteAuditRunId");
CREATE INDEX "IX_SiteAuditDetails_CheckType" ON "SiteAuditDetails"("CheckType");
CREATE INDEX "IX_SiteAuditDetails_Status" ON "SiteAuditDetails"("Status");

-- SiteAuditHistories Table (Detailed Score Breakdown)
CREATE TABLE "SiteAuditHistories" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "CheckDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "OverallScore" INTEGER,
    "LlmsTxtScore" INTEGER,
    "SchemaScore" INTEGER,
    "DomClarityScore" INTEGER,
    "ContentScore" INTEGER,
    "TechnicalScore" INTEGER,
    "TotalIssues" INTEGER NOT NULL,
    "CriticalIssues" INTEGER NOT NULL,
    "WarningIssues" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_SiteAuditHistories_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_SiteAuditHistories_ProjectId" ON "SiteAuditHistories"("ProjectId");
CREATE INDEX "IX_SiteAuditHistories_CheckDate" ON "SiteAuditHistories"("CheckDate");

-- ============================================================================
-- OPPORTUNITIES AUDITOR
-- ============================================================================

-- OpportunityScans Table
CREATE TABLE "OpportunityScans" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "ScanType" VARCHAR(50) NOT NULL,
    "TargetUrl" TEXT,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "TotalOpportunities" INTEGER NOT NULL DEFAULT 0,
    "HighPriority" INTEGER NOT NULL DEFAULT 0,
    "MediumPriority" INTEGER NOT NULL DEFAULT 0,
    "LowPriority" INTEGER NOT NULL DEFAULT 0,
    "StartedAt" TIMESTAMP WITH TIME ZONE,
    "CompletedAt" TIMESTAMP WITH TIME ZONE,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_OpportunityScans_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_OpportunityScans_ProjectId" ON "OpportunityScans"("ProjectId");
CREATE INDEX "IX_OpportunityScans_Status" ON "OpportunityScans"("Status");
CREATE INDEX "IX_OpportunityScans_ScanType" ON "OpportunityScans"("ScanType");
CREATE INDEX "IX_OpportunityScans_CreatedAt" ON "OpportunityScans"("CreatedAt");

-- ContentGapOpportunities Table
CREATE TABLE "ContentGapOpportunities" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "OpportunityScanId" INTEGER,
    "Topic" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "RecommendedContentType" VARCHAR(50),
    "EstimatedImpact" VARCHAR(20),
    "CompetitorCoverage" INTEGER NOT NULL DEFAULT 0,
    "SearchVolume" INTEGER NOT NULL DEFAULT 0,
    "Difficulty" INTEGER NOT NULL DEFAULT 0,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'New',
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_ContentGapOpportunities_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ContentGapOpportunities_OpportunityScans_OpportunityScanId" FOREIGN KEY ("OpportunityScanId") 
        REFERENCES "OpportunityScans"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_ContentGapOpportunities_ProjectId" ON "ContentGapOpportunities"("ProjectId");
CREATE INDEX "IX_ContentGapOpportunities_Status" ON "ContentGapOpportunities"("Status");
CREATE INDEX "IX_ContentGapOpportunities_EstimatedImpact" ON "ContentGapOpportunities"("EstimatedImpact");

-- CompetitorAnalyses Table
CREATE TABLE "CompetitorAnalyses" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "CompetitorDomain" VARCHAR(255) NOT NULL,
    "CompetitorName" VARCHAR(255),
    "VisibilityScore" INTEGER NOT NULL DEFAULT 0,
    "CitationShare" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "ContentCount" INTEGER NOT NULL DEFAULT 0,
    "TopTopics" TEXT,
    "Strengths" TEXT,
    "Weaknesses" TEXT,
    "LastAnalyzedAt" TIMESTAMP WITH TIME ZONE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CompetitorAnalyses_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_CompetitorAnalyses_ProjectId" ON "CompetitorAnalyses"("ProjectId");
CREATE INDEX "IX_CompetitorAnalyses_CompetitorDomain" ON "CompetitorAnalyses"("CompetitorDomain");
CREATE UNIQUE INDEX "IX_CompetitorAnalyses_ProjectId_CompetitorDomain" ON "CompetitorAnalyses"("ProjectId", "CompetitorDomain");

-- OpportunityImpacts Table
CREATE TABLE "OpportunityImpacts" (
    "Id" SERIAL PRIMARY KEY,
    "OpportunityType" VARCHAR(50) NOT NULL,
    "OpportunityId" INTEGER NOT NULL,
    "ImpactType" VARCHAR(50) NOT NULL,
    "EstimatedValue" DECIMAL(10,2),
    "ActualValue" DECIMAL(10,2),
    "MeasuredAt" TIMESTAMP WITH TIME ZONE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "IX_OpportunityImpacts_OpportunityType" ON "OpportunityImpacts"("OpportunityType");
CREATE INDEX "IX_OpportunityImpacts_OpportunityId" ON "OpportunityImpacts"("OpportunityId");

-- ============================================================================
-- VISIBILITY DASHBOARD
-- ============================================================================

-- VisibilityDashboardMetrics Table
CREATE TABLE "VisibilityDashboardMetrics" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "MetricDate" DATE NOT NULL,
    "Period" VARCHAR(20) NOT NULL DEFAULT 'daily',
    
    -- Overall Visibility
    "OverallVisibilityScore" INTEGER NOT NULL DEFAULT 0,
    "VisibilityRank" INTEGER,
    "VisibilityChange" DECIMAL(5,2),
    
    -- Prompt Performance
    "TotalPrompts" INTEGER NOT NULL DEFAULT 0,
    "ActivePrompts" INTEGER NOT NULL DEFAULT 0,
    "AveragePromptScore" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "PromptsWithMentions" INTEGER NOT NULL DEFAULT 0,
    "MentionRate" DECIMAL(5,2) NOT NULL DEFAULT 0,
    
    -- Citation Metrics
    "TotalCitations" INTEGER NOT NULL DEFAULT 0,
    "CitationShare" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "CitationRank" INTEGER,
    "UniqueDomainsCiting" INTEGER NOT NULL DEFAULT 0,
    "OwnedDomainCitations" INTEGER NOT NULL DEFAULT 0,
    
    -- Site Health
    "SiteAuditScore" INTEGER NOT NULL DEFAULT 0,
    "CriticalIssues" INTEGER NOT NULL DEFAULT 0,
    "WarningIssues" INTEGER NOT NULL DEFAULT 0,
    "PassedChecks" INTEGER NOT NULL DEFAULT 0,
    
    -- Opportunities
    "TotalOpportunities" INTEGER NOT NULL DEFAULT 0,
    "HighPriorityOpportunities" INTEGER NOT NULL DEFAULT 0,
    "OpportunitiesActioned" INTEGER NOT NULL DEFAULT 0,
    
    -- Platform Breakdown
    "PlatformMetrics" TEXT,
    
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_VisibilityDashboardMetrics_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_VisibilityDashboardMetrics_ProjectId" ON "VisibilityDashboardMetrics"("ProjectId");
CREATE INDEX "IX_VisibilityDashboardMetrics_MetricDate" ON "VisibilityDashboardMetrics"("MetricDate");
CREATE INDEX "IX_VisibilityDashboardMetrics_Period" ON "VisibilityDashboardMetrics"("Period");
CREATE UNIQUE INDEX "IX_VisibilityDashboardMetrics_ProjectId_MetricDate_Period" ON "VisibilityDashboardMetrics"("ProjectId", "MetricDate", "Period");

-- VisibilityGoals Table
CREATE TABLE "VisibilityGoals" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "GoalType" VARCHAR(50) NOT NULL,
    "TargetValue" DECIMAL(10,2) NOT NULL,
    "CurrentValue" DECIMAL(10,2) NOT NULL DEFAULT 0,
    "Progress" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "Status" VARCHAR(20),
    "TargetDate" DATE,
    "Description" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_VisibilityGoals_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_VisibilityGoals_ProjectId" ON "VisibilityGoals"("ProjectId");
CREATE INDEX "IX_VisibilityGoals_Status" ON "VisibilityGoals"("Status");
CREATE INDEX "IX_VisibilityGoals_TargetDate" ON "VisibilityGoals"("TargetDate");

-- VisibilityAlerts Table
CREATE TABLE "VisibilityAlerts" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "AlertType" VARCHAR(50) NOT NULL,
    "Severity" VARCHAR(20) NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Message" TEXT NOT NULL,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReadAt" TIMESTAMP WITH TIME ZONE,
    "ActionUrl" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_VisibilityAlerts_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_VisibilityAlerts_ProjectId" ON "VisibilityAlerts"("ProjectId");
CREATE INDEX "IX_VisibilityAlerts_IsRead" ON "VisibilityAlerts"("IsRead");
CREATE INDEX "IX_VisibilityAlerts_Severity" ON "VisibilityAlerts"("Severity");
CREATE INDEX "IX_VisibilityAlerts_CreatedAt" ON "VisibilityAlerts"("CreatedAt");

-- CompetitiveIntelligence Table (Specific Metrics)
CREATE TABLE "CompetitiveIntelligence" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "CompetitorDomain" VARCHAR(255) NOT NULL,
    "MetricDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "VisibilityScore" INTEGER NOT NULL,
    "CitationShare" DECIMAL(5,2) NOT NULL,
    "Rank" INTEGER,
    "PromptsTracked" INTEGER NOT NULL,
    "AverageMentionPosition" DECIMAL(4,2),
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_CompetitiveIntelligence_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_CompetitiveIntelligence_ProjectId" ON "CompetitiveIntelligence"("ProjectId");
CREATE INDEX "IX_CompetitiveIntelligence_MetricDate" ON "CompetitiveIntelligence"("MetricDate");
CREATE INDEX "IX_CompetitiveIntelligence_CompetitorDomain" ON "CompetitiveIntelligence"("CompetitorDomain");

-- ============================================================================
-- TASKS & NOTIFICATIONS
-- ============================================================================

-- UserTasks Table
CREATE TABLE "UserTasks" (
    "Id" SERIAL PRIMARY KEY,
    "ProjectId" INTEGER NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "Priority" VARCHAR(20) NOT NULL DEFAULT 'Medium',
    "Status" VARCHAR(20) NOT NULL DEFAULT 'TO-DO',
    "IsCompleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DueDate" TIMESTAMP WITH TIME ZONE,
    "Source" VARCHAR(50) DEFAULT 'Manual',
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserTasks_Projects_ProjectId" FOREIGN KEY ("ProjectId") 
        REFERENCES "Projects"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserTasks_ProjectId" ON "UserTasks"("ProjectId");
CREATE INDEX "IX_UserTasks_Status" ON "UserTasks"("Status");
CREATE INDEX "IX_UserTasks_Priority" ON "UserTasks"("Priority");
CREATE INDEX "IX_UserTasks_IsCompleted" ON "UserTasks"("IsCompleted");
CREATE INDEX "IX_UserTasks_DueDate" ON "UserTasks"("DueDate");
CREATE INDEX "IX_UserTasks_CreatedAt" ON "UserTasks"("CreatedAt");

-- Notifications Table
CREATE TABLE "Notifications" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Body" VARCHAR(1000) NOT NULL,
    "Type" VARCHAR(20) NOT NULL DEFAULT 'info',
    "Icon" VARCHAR(50),
    "IsGlobal" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsDismissible" BOOLEAN NOT NULL DEFAULT TRUE,
    "DurationMs" INTEGER DEFAULT 5000,
    "ActionUrl" VARCHAR(500),
    "ActionText" VARCHAR(100),
    "Data" TEXT,
    "ImageUrl" TEXT,
    "IsSent" BOOLEAN NOT NULL DEFAULT FALSE,
    "SentAt" TIMESTAMP WITH TIME ZONE,
    "ScheduledFor" TIMESTAMP WITH TIME ZONE,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReadAt" TIMESTAMP WITH TIME ZONE,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "User"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Notifications_UserId" ON "Notifications"("UserId");
CREATE INDEX "IX_Notifications_Type" ON "Notifications"("Type");
CREATE INDEX "IX_Notifications_IsRead" ON "Notifications"("IsRead");
CREATE INDEX "IX_Notifications_IsSent" ON "Notifications"("IsSent");
CREATE INDEX "IX_Notifications_CreatedAt" ON "Notifications"("CreatedAt");

COMMIT;

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- List all tables
SELECT 
    'Tables Created:' as info,
    table_name
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_type = 'BASE TABLE'
    AND table_name IN (
        'Projects', 'Keywords', 'ContentArticles', 'AiTrackingPrompts', 'AiTrackingResults',
        'PromptTrackings', 'PromptPlatformTrackings', 'PromptVisibilityMetrics', 'PromptResearchDatas',
        'Citations', 'CitationMetrics', 'BrandMentions', 'CitationAggregations',
        'CommunityOpportunities', 'SiteAudits', 'SiteAuditRuns', 'SiteAuditDetails', 'SiteAuditHistories',
        'OpportunityScans', 'ContentGapOpportunities', 'CompetitorAnalyses', 'OpportunityImpacts',
        'VisibilityDashboardMetrics', 'VisibilityGoals', 'VisibilityAlerts', 'CompetitiveIntelligence',
        'UserTasks', 'Notifications'
    )
ORDER BY table_name;

-- Count indexes
SELECT 
    'Total Indexes Created:' as info,
    COUNT(*) as index_count
FROM pg_indexes 
WHERE schemaname = 'public'
    AND tablename IN (
        'Projects', 'Keywords', 'ContentArticles', 'AiTrackingPrompts', 'AiTrackingResults',
        'PromptTrackings', 'PromptPlatformTrackings', 'PromptVisibilityMetrics', 'PromptResearchDatas',
        'Citations', 'CitationMetrics', 'BrandMentions', 'CitationAggregations',
        'CommunityOpportunities', 'SiteAudits', 'SiteAuditRuns', 'SiteAuditDetails', 'SiteAuditHistories',
        'OpportunityScans', 'ContentGapOpportunities', 'CompetitorAnalyses', 'OpportunityImpacts',
        'VisibilityDashboardMetrics', 'VisibilityGoals', 'VisibilityAlerts', 'CompetitiveIntelligence',
        'UserTasks', 'Notifications'
    );

-- ============================================================================
-- SCRIPT COMPLETE
-- ============================================================================
