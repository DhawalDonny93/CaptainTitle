-- ============================================================================
-- Enable AISEO Features for Projects
-- Description: Enables auto-crawl, opportunity detection, and visibility alerts
-- Usage: Run this script to enable features for specific projects
-- ============================================================================

-- ============================================================================
-- OPTION 1: Enable All Features for a Specific Project
-- ============================================================================

-- Replace PROJECT_ID with your actual project ID
UPDATE "Projects" SET
    -- Site Crawling
    "EnableAutoCrawl" = true,
    "CrawlMaxPages" = 100,
    "CrawlMaxDepth" = 3,
    
    -- Opportunity Detection
    "EnableOpportunityDetection" = true,
    "CompetitorDomainsJson" = '["competitor1.com", "competitor2.com"]', -- Update with actual competitors
    
    -- Visibility Alerts
    "EnableVisibilityAlerts" = true,
    "VisibilityCheckFrequency" = 'Hourly'
    
WHERE "Id" = 1; -- Change this to your project ID

-- ============================================================================
-- OPTION 2: Enable All Features for ALL Projects
-- ============================================================================

-- WARNING: This enables features for ALL projects in the database
-- Uncomment the lines below to use

-- UPDATE "Projects" SET
--     "EnableAutoCrawl" = true,
--     "CrawlMaxPages" = 100,
--     "CrawlMaxDepth" = 3,
--     "EnableOpportunityDetection" = true,
--     "EnableVisibilityAlerts" = true,
--     "VisibilityCheckFrequency" = 'Hourly';

-- ============================================================================
-- OPTION 3: Enable Features Selectively
-- ============================================================================

-- Enable only site crawling for project 1
UPDATE "Projects" SET
    "EnableAutoCrawl" = true,
    "CrawlMaxPages" = 50,  -- Smaller crawl
    "CrawlMaxDepth" = 2
WHERE "Id" = 1;

-- Enable only opportunity detection for project 2
UPDATE "Projects" SET
    "EnableOpportunityDetection" = true,
    "CompetitorDomainsJson" = '["example.com", "test.com"]'
WHERE "Id" = 2;

-- Enable only visibility alerts for project 3
UPDATE "Projects" SET
    "EnableVisibilityAlerts" = true,
    "VisibilityCheckFrequency" = 'Daily'
WHERE "Id" = 3;

-- ============================================================================
-- OPTION 4: Disable Features
-- ============================================================================

-- Disable all features for a project
UPDATE "Projects" SET
    "EnableAutoCrawl" = false,
    "EnableOpportunityDetection" = false,
    "EnableVisibilityAlerts" = false
WHERE "Id" = 1;

-- ============================================================================
-- VERIFY CONFIGURATION
-- ============================================================================

-- Check current configuration for all projects
SELECT 
    "Id",
    "Name",
    "WebsiteUrl",
    "EnableAutoCrawl",
    "CrawlMaxPages",
    "CrawlMaxDepth",
    "EnableOpportunityDetection",
    "CompetitorDomainsJson",
    "EnableVisibilityAlerts",
    "VisibilityCheckFrequency"
FROM "Projects"
ORDER BY "Id";

-- ============================================================================
-- NOTES
-- ============================================================================

/*
Feature Descriptions:

1. EnableAutoCrawl (true/false)
   - Enables weekly automatic site crawling
   - Crawls run every Sunday at 3 AM UTC
   - Discovers all pages up to MaxPages and MaxDepth

2. CrawlMaxPages (integer, default: 100)
   - Maximum number of pages to crawl per run
   - Recommended: 50-200 depending on site size
   - Higher values = longer crawl time

3. CrawlMaxDepth (integer, default: 3)
   - Maximum link depth from homepage
   - Depth 0 = homepage only
   - Depth 3 = homepage + 3 levels of links
   - Recommended: 2-4

4. EnableOpportunityDetection (true/false)
   - Enables daily opportunity detection
   - Runs every day at 4 AM UTC
   - Detects content gaps, SEO issues, visibility opportunities

5. CompetitorDomainsJson (JSON array)
   - List of competitor domains to analyze
   - Used for content gap analysis
   - Format: '["domain1.com", "domain2.com"]'
   - Leave as '[]' if no competitors

6. EnableVisibilityAlerts (true/false)
   - Enables real-time visibility alerts
   - Sends alerts when visibility drops >20%
   - Aggregates daily at 2 AM UTC

7. VisibilityCheckFrequency (string)
   - How often to check prompt visibility
   - Options: 'Hourly', 'Daily', 'Weekly'
   - Recommended: 'Hourly' for active monitoring

Cost Impact:
- EnableAutoCrawl: No LLM cost (just HTTP requests)
- EnableOpportunityDetection: ~$3/month per project
- EnableVisibilityAlerts: Included in hourly visibility checks

Background Jobs:
- Visibility Check: Runs hourly (checks all active prompts)
- Site Crawl: Runs weekly (only if EnableAutoCrawl = true)
- Opportunity Detection: Runs daily (only if EnableOpportunityDetection = true)
- Visibility Trends: Runs daily (aggregates metrics)
*/
