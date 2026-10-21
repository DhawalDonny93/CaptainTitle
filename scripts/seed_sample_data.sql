-- ============================================================================
-- AISEO Sample Data Seeding Script
-- Description: Seeds realistic sample data for testing and demonstration
-- Usage: psql -d your_database -f scripts/seed_sample_data.sql
-- ============================================================================

-- ============================================================================
-- PART 1: LLM Provider Configurations (if not already seeded)
-- ============================================================================

INSERT INTO "LlmProviderConfigs" ("Provider", "Model", "IsEnabled", "Priority", "MaxTokens", "Temperature", "CreatedAt", "UpdatedAt")
VALUES 
    ('OpenAI', 'gpt-4-turbo-preview', true, 1, 4000, 0.7, NOW(), NOW()),
    ('Anthropic', 'claude-3-opus-20240229', true, 2, 4000, 0.7, NOW(), NOW()),
    ('Google', 'gemini-pro', true, 3, 4000, 0.7, NOW(), NOW()),
    ('Perplexity', 'pplx-70b-online', false, 4, 4000, 0.7, NOW(), NOW())
ON CONFLICT ("Provider", "Model") DO NOTHING;

-- ============================================================================
-- PART 2: Sample Prompt Research Data
-- ============================================================================

-- Assuming Project ID 1 exists, seed prompt research suggestions
INSERT INTO "PromptResearchDatas" ("ProjectId", "PromptText", "Topic", "Category", "RelevanceScore", "EstimatedVolume", "Difficulty", "Status", "DiscoveredAt", "CreatedAt", "UpdatedAt")
VALUES
    -- AI SEO Topic
    (1, 'How can AI improve SEO rankings?', 'AI SEO', 'Question', 85, 'High', 'Medium', 'New', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
    (1, 'What are the best AI tools for SEO optimization?', 'AI SEO', 'Question', 90, 'High', 'Easy', 'New', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
    (1, 'AI-powered content optimization strategies', 'AI SEO', 'How-To', 88, 'Medium', 'Medium', 'Reviewed', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', NOW() - INTERVAL '4 days'),
    (1, 'Compare traditional SEO vs AI-driven SEO', 'AI SEO', 'Comparison', 82, 'Medium', 'Hard', 'New', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
    (1, 'Best practices for AI SEO in 2024', 'AI SEO', 'Best Practices', 87, 'High', 'Easy', 'Approved', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', NOW() - INTERVAL '3 days'),
    
    -- Content Marketing Topic
    (1, 'How to create engaging content with AI', 'Content Marketing', 'How-To', 83, 'High', 'Medium', 'New', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),
    (1, 'AI content generation vs human writers', 'Content Marketing', 'Comparison', 79, 'Medium', 'Medium', 'New', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),
    (1, 'What is the ROI of AI-powered content marketing?', 'Content Marketing', 'Question', 81, 'Medium', 'Hard', 'Reviewed', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days', NOW() - INTERVAL '3 days'),
    (1, 'Best AI tools for content marketers', 'Content Marketing', 'List', 86, 'High', 'Easy', 'New', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),
    (1, 'How to scale content production with AI', 'Content Marketing', 'How-To', 84, 'High', 'Medium', 'Approved', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days', NOW() - INTERVAL '2 days'),
    
    -- Technical SEO Topic
    (1, 'What is schema markup and why is it important?', 'Technical SEO', 'Question', 88, 'High', 'Easy', 'New', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
    (1, 'How to optimize Core Web Vitals', 'Technical SEO', 'How-To', 91, 'High', 'Hard', 'Reviewed', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', NOW() - INTERVAL '2 days'),
    (1, 'Technical SEO checklist for 2024', 'Technical SEO', 'Checklist', 89, 'High', 'Medium', 'Approved', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', NOW() - INTERVAL '1 day'),
    (1, 'How to fix crawl errors in Google Search Console', 'Technical SEO', 'How-To', 85, 'Medium', 'Medium', 'New', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
    (1, 'What are the most common technical SEO mistakes?', 'Technical SEO', 'Question', 83, 'Medium', 'Easy', 'New', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
    
    -- Link Building Topic
    (1, 'How to build high-quality backlinks in 2024', 'Link Building', 'How-To', 87, 'High', 'Hard', 'New', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
    (1, 'What makes a good backlink?', 'Link Building', 'Question', 84, 'Medium', 'Easy', 'Reviewed', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
    (1, 'Link building strategies that still work', 'Link Building', 'Best Practices', 86, 'High', 'Medium', 'New', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
    (1, 'How to recover from a Google penalty', 'Link Building', 'How-To', 82, 'Low', 'Hard', 'New', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
    (1, 'Best tools for link building and outreach', 'Link Building', 'List', 85, 'Medium', 'Easy', 'Approved', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
    
    -- Local SEO Topic
    (1, 'How to optimize Google Business Profile', 'Local SEO', 'How-To', 90, 'High', 'Easy', 'New', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    (1, 'What is local SEO and why does it matter?', 'Local SEO', 'Question', 88, 'High', 'Easy', 'Reviewed', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW() - INTERVAL '12 hours'),
    (1, 'Local SEO ranking factors in 2024', 'Local SEO', 'Best Practices', 89, 'Medium', 'Medium', 'New', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    (1, 'How to get more Google reviews', 'Local SEO', 'How-To', 86, 'High', 'Easy', 'Approved', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW() - INTERVAL '6 hours'),
    (1, 'Best local citation sources for SEO', 'Local SEO', 'List', 84, 'Medium', 'Easy', 'New', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day');

-- ============================================================================
-- PART 3: Sample Active Prompts (for visibility tracking)
-- ============================================================================

-- Insert sample prompts
INSERT INTO "PromptTrackings" ("ProjectId", "UserId", "PromptText", "Topic", "Status", "AverageVisibilityScore", "LastCheckedAt", "CreatedAt", "UpdatedAt")
VALUES
    (1, 1, 'How can AI improve SEO rankings?', 'AI SEO', 'Active', 75.5, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '10 days', NOW() - INTERVAL '1 hour'),
    (1, 1, 'What are the best AI tools for SEO optimization?', 'AI SEO', 'Active', 82.3, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '10 days', NOW() - INTERVAL '1 hour'),
    (1, 1, 'Best practices for AI SEO in 2024', 'AI SEO', 'Active', 68.7, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '9 days', NOW() - INTERVAL '1 hour'),
    (1, 1, 'How to optimize Core Web Vitals', 'Technical SEO', 'Active', 79.2, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '8 days', NOW() - INTERVAL '1 hour'),
    (1, 1, 'Technical SEO checklist for 2024', 'Technical SEO', 'Active', 85.1, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '8 days', NOW() - INTERVAL '1 hour'),
    (1, 1, 'How to optimize Google Business Profile', 'Local SEO', 'Active', 91.4, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 hour'),
    (1, 1, 'What is local SEO and why does it matter?', 'Local SEO', 'Active', 73.8, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 hour'),
    (1, 1, 'How to build high-quality backlinks in 2024', 'Link Building', 'Active', 66.5, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '6 days', NOW() - INTERVAL '1 hour'),
    (1, 1, 'Link building strategies that still work', 'Link Building', 'Active', 71.9, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '6 days', NOW() - INTERVAL '1 hour'),
    (1, 1, 'How to create engaging content with AI', 'Content Marketing', 'Active', 77.6, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 hour');

-- Get the IDs of the inserted prompts for platform tracking
DO $$
DECLARE
    prompt_id INTEGER;
BEGIN
    FOR prompt_id IN SELECT "Id" FROM "PromptTrackings" WHERE "ProjectId" = 1 AND "Status" = 'Active' LIMIT 10
    LOOP
        -- Add platform tracking for each prompt (ChatGPT, Claude, Gemini, Perplexity)
        INSERT INTO "PromptPlatformTrackings" ("PromptTrackingId", "Platform", "IsEnabled", "CurrentVisibilityScore", "BrandMentioned", "LastCheckDate", "CreatedAt", "UpdatedAt")
        VALUES
            (prompt_id, 'ChatGPT', true, 70 + (RANDOM() * 30)::INTEGER, (RANDOM() > 0.3), NOW() - INTERVAL '1 hour', NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 hour'),
            (prompt_id, 'Claude', true, 70 + (RANDOM() * 30)::INTEGER, (RANDOM() > 0.4), NOW() - INTERVAL '1 hour', NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 hour'),
            (prompt_id, 'Gemini', true, 70 + (RANDOM() * 30)::INTEGER, (RANDOM() > 0.5), NOW() - INTERVAL '1 hour', NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 hour'),
            (prompt_id, 'Perplexity', true, 70 + (RANDOM() * 30)::INTEGER, (RANDOM() > 0.6), NOW() - INTERVAL '1 hour', NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 hour');
    END LOOP;
END $$;

-- ============================================================================
-- PART 4: Sample Visibility Metrics (last 7 days)
-- ============================================================================

-- Generate sample visibility metrics for the last 7 days
DO $$
DECLARE
    platform_id INTEGER;
    day_offset INTEGER;
BEGIN
    FOR platform_id IN SELECT "Id" FROM "PromptPlatformTrackings" LIMIT 40
    LOOP
        FOR day_offset IN 0..6
        LOOP
            INSERT INTO "PromptVisibilityMetrics" (
                "PromptPlatformId", "CheckDate", "VisibilityScore", "Position", 
                "BrandMentioned", "Sentiment", "SentimentScore", "ResponseLength",
                "ResponseText", "CreatedAt"
            )
            VALUES (
                platform_id,
                NOW() - (day_offset || ' days')::INTERVAL,
                60 + (RANDOM() * 40)::INTEGER,
                CASE WHEN RANDOM() > 0.3 THEN (1 + (RANDOM() * 9)::INTEGER) ELSE NULL END,
                (RANDOM() > 0.4),
                CASE 
                    WHEN RANDOM() < 0.6 THEN 'Positive'
                    WHEN RANDOM() < 0.9 THEN 'Neutral'
                    ELSE 'Negative'
                END,
                50 + (RANDOM() * 50)::INTEGER,
                500 + (RANDOM() * 1500)::INTEGER,
                'Sample AI response text for testing purposes...',
                NOW() - (day_offset || ' days')::INTERVAL
            );
        END LOOP;
    END LOOP;
END $$;

-- ============================================================================
-- PART 5: Sample Citations
-- ============================================================================

INSERT INTO "Citations" (
    "ProjectId", "SourceUrl", "SourceDomain", "SourceTitle", "Platform",
    "PromptText", "Position", "CitationType", "Context", 
    "DiscoveredAt", "CreatedAt"
)
VALUES
    (1, 'https://example.com/ai-seo-guide', 'example.com', 'Complete AI SEO Guide 2024', 'ChatGPT', 'How can AI improve SEO rankings?', 1, 'DirectLink', 'AI tools can significantly improve SEO rankings through automated optimization...', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
    (1, 'https://blog.example.com/seo-tips', 'example.com', 'Top SEO Tips for 2024', 'Claude', 'What are the best AI tools for SEO optimization?', 2, 'Reference', 'According to industry experts, best practices include...', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
    (1, 'https://tools.example.com', 'example.com', 'AI SEO Tools Directory', 'Gemini', 'What are the best AI tools for SEO optimization?', 1, 'DirectLink', 'Check out these AI SEO tools for comprehensive optimization...', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    (1, 'https://competitor.com/guide', 'competitor.com', 'Technical SEO Implementation Guide', 'Perplexity', 'How to optimize Core Web Vitals', 3, 'Inline', 'As mentioned in technical implementation guides...', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    (1, 'https://example.com/core-web-vitals', 'example.com', 'Core Web Vitals Optimization', 'ChatGPT', 'How to optimize Core Web Vitals', 1, 'DirectLink', 'Performance matters for both user experience and SEO rankings...', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours');

-- ============================================================================
-- PART 6: Sample Opportunities
-- ============================================================================

-- Content Gap Opportunities (using COMPLETE_AISEO_SCHEMA.sql schema)
INSERT INTO "ContentGapOpportunities" (
    "ProjectId", "Topic", "Description", "RecommendedContentType",
    "EstimatedImpact", "CompetitorCoverage", "SearchVolume", "Difficulty",
    "Status", "CreatedAt", "UpdatedAt"
)
VALUES
    (1, 'Voice Search SEO', 'Growing trend in voice-activated search queries. Competitors have comprehensive coverage with 5+ articles on this topic.', 'Blog Post', 'High', 5, 8500, 65, 'New', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
    (1, 'AI Content Detection', 'Controversial but high-interest topic about making AI-generated content undetectable by AI detectors.', 'Guide', 'Medium', 8, 12000, 75, 'New', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    (1, 'SEO for E-commerce', 'Large market opportunity with limited competitor coverage. E-commerce SEO best practices guide.', 'Comprehensive Guide', 'High', 3, 15000, 60, 'New', NOW() - INTERVAL '6 hours', NOW() - INTERVAL '6 hours');

-- SEO Opportunities
INSERT INTO "SeoOpportunities" (
    "ProjectId", "PageUrl", "OpportunityType", "Priority",
    "CurrentState", "RecommendedAction", "EstimatedImpact",
    "Status", "DiscoveredAt", "CreatedAt", "UpdatedAt"
)
VALUES
    (1, 'https://example.com/blog/old-post', 'ThinContent', 'High', 'Only 300 words', 'Expand to 1500+ words with comprehensive information', 75, 'Open', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    (1, 'https://example.com/services', 'MissingMeta', 'Medium', 'No meta description', 'Add compelling meta description (150-160 chars)', 60, 'Open', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    (1, 'https://example.com/about', 'PoorHeadings', 'Low', 'No H1 tag', 'Add proper H1 heading with target keyword', 45, 'Open', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day');

-- Visibility Opportunities (using actual migration schema)
INSERT INTO "VisibilityOpportunities" (
    "ProjectId", "PromptText", "Platform", "OpportunityType",
    "CurrentPosition", "Reasoning", "RecommendedContent", "OptimizationStrategy",
    "Priority", "Status", "DiscoveredAt", "CreatedAt", "UpdatedAt"
)
VALUES
    (1, 'How can AI improve SEO rankings?', 'ChatGPT', 'MentionedLate', 8, 'Brand mentioned but appears late in response (position 8)', 'Create comprehensive AI SEO guide with practical examples', 'Improve content depth, add case studies, optimize for featured snippets', 1, 'New', NOW() - INTERVAL '12 hours', NOW() - INTERVAL '12 hours', NOW() - INTERVAL '12 hours'),
    (1, 'What are the best AI tools for SEO optimization?', 'Claude', 'CompetitorDominance', 5, 'Competitors mentioned before our brand', 'Develop detailed comparison guide highlighting unique features', 'Create comparison content, add unique insights, improve technical implementation', 2, 'New', NOW() - INTERVAL '6 hours', NOW() - INTERVAL '6 hours', NOW() - INTERVAL '6 hours');

-- ============================================================================
-- PART 7: Sample Job Execution Logs
-- ============================================================================

INSERT INTO "JobExecutionLogs" (
    "JobName", "JobType", "Status", "StartedAt", "CompletedAt", "DurationSeconds",
    "ItemsProcessed", "ItemsSucceeded", "ItemsFailed", "CreatedAt"
)
VALUES
    ('VisibilityCheckJob', 'VisibilityCheck', 'Completed', NOW() - INTERVAL '1 hour', NOW() - INTERVAL '55 minutes', 300, 10, 10, 0, NOW() - INTERVAL '55 minutes'),
    ('OpportunityDetectionJob', 'OpportunityDetection', 'Completed', NOW() - INTERVAL '4 hours', NOW() - INTERVAL '3 hours 55 minutes', 300, 1, 1, 0, NOW() - INTERVAL '3 hours 55 minutes'),
    ('DashboardMetricsUpdateJob', 'DashboardUpdate', 'Completed', NOW() - INTERVAL '6 hours', NOW() - INTERVAL '5 hours 59 minutes', 60, 1, 1, 0, NOW() - INTERVAL '5 hours 59 minutes'),
    ('VisibilityTrendAggregationJob', 'TrendAggregation', 'Completed', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day' + INTERVAL '2 minutes', 120, 1, 1, 0, NOW() - INTERVAL '1 day' + INTERVAL '2 minutes'),
    ('CitationAggregationJob', 'CitationAggregation', 'Completed', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day' + INTERVAL '1 minute', 60, 1, 1, 0, NOW() - INTERVAL '1 day' + INTERVAL '1 minute');

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- Check seeded data counts
SELECT 
    'Prompt Research' as "Data Type",
    COUNT(*) as "Count"
FROM "PromptResearchDatas"
WHERE "ProjectId" = 1

UNION ALL

SELECT 
    'Active Prompts',
    COUNT(*)
FROM "PromptTrackings"
WHERE "ProjectId" = 1 AND "Status" = 'Active'

UNION ALL

SELECT 
    'Platform Trackings',
    COUNT(*)
FROM "PromptPlatformTrackings"
WHERE "PromptTrackingId" IN (SELECT "Id" FROM "PromptTrackings" WHERE "ProjectId" = 1)

UNION ALL

SELECT 
    'Visibility Metrics',
    COUNT(*)
FROM "PromptVisibilityMetrics"
WHERE "PromptPlatformId" IN (
    SELECT "Id" FROM "PromptPlatformTrackings" 
    WHERE "PromptTrackingId" IN (SELECT "Id" FROM "PromptTrackings" WHERE "ProjectId" = 1)
)

UNION ALL

SELECT 
    'Citations',
    COUNT(*)
FROM "Citations"
WHERE "ProjectId" = 1

UNION ALL

SELECT 
    'Content Gap Opportunities',
    COUNT(*)
FROM "ContentGapOpportunities"
WHERE "ProjectId" = 1

UNION ALL

SELECT 
    'SEO Opportunities',
    COUNT(*)
FROM "SeoOpportunities"
WHERE "ProjectId" = 1

UNION ALL

SELECT 
    'Visibility Opportunities',
    COUNT(*)
FROM "VisibilityOpportunities"
WHERE "ProjectId" = 1

UNION ALL

SELECT 
    'Job Execution Logs',
    COUNT(*)
FROM "JobExecutionLogs";

-- ============================================================================
-- SUCCESS MESSAGE
-- ============================================================================

DO $$
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Sample Data Seeding Complete!';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Seeded data for Project ID: 1';
    RAISE NOTICE '- 25 Prompt Research entries';
    RAISE NOTICE '- 10 Active Prompts';
    RAISE NOTICE '- 40 Platform Trackings';
    RAISE NOTICE '- 280+ Visibility Metrics';
    RAISE NOTICE '- 5 Citations';
    RAISE NOTICE '- 8 Opportunities';
    RAISE NOTICE '- 5 Job Execution Logs';
    RAISE NOTICE '========================================';
END $$;
