-- ============================================================================
-- Migration 24: REMOVE THE OLD AI-SEO TABLES   *** DESTRUCTIVE - DELETES DATA ***
-- ----------------------------------------------------------------------------
-- The AI-SEO product module (projects, prompt tracking, citations, site audits, opportunities, visibility dashboards,
-- competitors, content articles, tasks ...) was removed from the code base. This script drops its 35 tables.
--
--   BEFORE RUNNING
--     1. Take a backup (Supabase: Database > Backups, or pg_dump).
--     2. Run 23_schema_drift_check.sql and look at the "LEFTOVER AI-SEO TABLE" row counts.
--     3. Deploy the new application build first (the old build still reads these tables).
--
--   WHAT IT DOES
--     * Drops the tables with CASCADE (this also removes their foreign keys, including the old foreign keys from
--       "LlmRequest", "LlmCostTracking" and "QuotaUsageLogs" to "Projects"). Those three tables keep their
--       "ProjectId" integer column (now a plain nullable column) so history is preserved.
--     * Removes leftover Hangfire schedules/jobs of the deleted background jobs (only if the hangfire schema exists).
--   Safe to run on a database that never had these tables (everything is IF EXISTS). Runs in one transaction.
--   It does NOT touch generic template tables, Captain Title tables, or the SubscriptionPlans quota columns
--   (MaxProjects, MaxPromptsPerProject, MaxVisibilityChecksPerMonth, MaxSiteAuditsPerMonth, MaxContentArticles),
--   which are kept so existing plans keep working.
-- ============================================================================

BEGIN;

DROP TABLE IF EXISTS "AiTrackingResults" CASCADE;
DROP TABLE IF EXISTS "BrandMentions" CASCADE;
DROP TABLE IF EXISTS "BrandProfiles" CASCADE;
DROP TABLE IF EXISTS "CitationAggregations" CASCADE;
DROP TABLE IF EXISTS "CitationMetrics" CASCADE;
DROP TABLE IF EXISTS "Citations" CASCADE;
DROP TABLE IF EXISTS "CommunityOpportunities" CASCADE;
DROP TABLE IF EXISTS "CompetitiveIntelligence" CASCADE;
DROP TABLE IF EXISTS "CompetitorAnalyses" CASCADE;
DROP TABLE IF EXISTS "Competitors" CASCADE;
DROP TABLE IF EXISTS "ContentArticles" CASCADE;
DROP TABLE IF EXISTS "ContentGapOpportunities" CASCADE;
DROP TABLE IF EXISTS "JobExecutionLogs" CASCADE;
DROP TABLE IF EXISTS "Keywords" CASCADE;
DROP TABLE IF EXISTS "OpportunityImpacts" CASCADE;
DROP TABLE IF EXISTS "OpportunityScans" CASCADE;
DROP TABLE IF EXISTS "Projects" CASCADE;
DROP TABLE IF EXISTS "PromptEditHistory" CASCADE;
DROP TABLE IF EXISTS "PromptPlatformTrackings" CASCADE;
DROP TABLE IF EXISTS "PromptResearchDatas" CASCADE;
DROP TABLE IF EXISTS "PromptTemplates" CASCADE;
DROP TABLE IF EXISTS "PromptTrackings" CASCADE;
DROP TABLE IF EXISTS "PromptVisibilityMetrics" CASCADE;
DROP TABLE IF EXISTS "SeoOpportunities" CASCADE;
DROP TABLE IF EXISTS "SiteAuditDetails" CASCADE;
DROP TABLE IF EXISTS "SiteAuditHistories" CASCADE;
DROP TABLE IF EXISTS "SiteAuditIssues" CASCADE;
DROP TABLE IF EXISTS "SiteAuditRuns" CASCADE;
DROP TABLE IF EXISTS "SiteAudits" CASCADE;
DROP TABLE IF EXISTS "UserTasks" CASCADE;
DROP TABLE IF EXISTS "VisibilityAlerts" CASCADE;
DROP TABLE IF EXISTS "VisibilityDashboardMetrics" CASCADE;
DROP TABLE IF EXISTS "VisibilityGoals" CASCADE;
DROP TABLE IF EXISTS "VisibilityOpportunities" CASCADE;
DROP TABLE IF EXISTS "VisibilityTrends" CASCADE;

-- ---- Hangfire clean-up (recurring schedules + queued/scheduled jobs of the removed AI-SEO background jobs) ----
DO $$
DECLARE
    old_ids text[] := ARRAY['ai-visibility-citation-check','update-dashboard-metrics','aggregate-visibility-trends','aggregate-citations',
                            'detect-opportunities','generate-prompt-research','crawl-sites','auto-blog-publishing',
                            'ai-visibility-tracking','social-monitoring-scan','site-audit-scan'];
    job_ids bigint[];
BEGIN
    IF to_regclass('hangfire.set') IS NOT NULL THEN
        DELETE FROM hangfire.set  WHERE key = 'recurring-jobs' AND value = ANY (old_ids);
    END IF;
    IF to_regclass('hangfire.hash') IS NOT NULL THEN
        DELETE FROM hangfire.hash WHERE key IN (SELECT 'recurring-job:' || x FROM unnest(old_ids) AS x);
    END IF;
    IF to_regclass('hangfire.job') IS NOT NULL THEN
        SELECT array_agg(id) INTO job_ids FROM hangfire.job
        WHERE invocationdata::text ~ 'AppBackendBasic\.Infrastructure\.BackgroundJobs|IAutoBlogService|IAiTrackingService|ISocialMonitoringService|ISiteAuditService|IPromptTrackingService|IPromptResearchService|ICitationTrackingService|IVisibilityDashboardService';
        IF job_ids IS NOT NULL THEN
            IF to_regclass('hangfire.jobqueue') IS NOT NULL THEN DELETE FROM hangfire.jobqueue WHERE jobid = ANY (job_ids); END IF;
            DELETE FROM hangfire.job WHERE id = ANY (job_ids);
        END IF;
    END IF;
END $$;

COMMIT;
